using Microsoft.Extensions.Caching.Memory;
using PuppeteerSharp;
using Serilog;
using System.Collections.Concurrent;

namespace ExpertPriceCrawler
{
    public static class BrowserCrawler
    {
        private static ILogger logger => Configuration.Logger;
        private static ConfigurationValues configuration => Configuration.Instance;
        private static IMemoryCache memoryCache = Configuration.MemoryCache;
        public static ConcurrentDictionary<string, string> statusDictionary = Configuration.StatusDictionary;

        public static async Task<List<Result>> CollectPrices(Uri uri)
        {
            uri = uri.MakeExpertUri();

            await EnsureBrowserAvailable();

            logger.Information($"Requested Prices for {uri}");
            return await memoryCache.GetOrCreateAsync(uri.ToString(), async e =>
            {
                logger.Verbose("Result not in cache. Crawling with {maxParallel} parallel requests", configuration.MaxParallelRequests);
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(configuration.MemoryCacheMinutes);
                return await GetResultForUri(uri, CancellationToken.None);
            });
        }

        private static async Task<List<Result>> GetResultForUri(Uri uri, CancellationToken cancellationToken)
        {
            var errors = 0;
            try
            {
                using var browser = await Puppeteer.LaunchAsync(configuration.PuppeteerLaunchOptions);
                ConcurrentStack<BrowserContext> browserContextPool = await CreateBrowserContextPool(browser);

                var results = new ConcurrentDictionary<string, Result>();
                var branchesTotal = configuration.Branches.Count;
                var branchesDone = 0;

                await Parallel.ForEachAsync(configuration.Branches, new ParallelOptions
                {
                    MaxDegreeOfParallelism = configuration.MaxParallelRequests,
                    CancellationToken = cancellationToken
                }, async (branch, cancellationToken) =>
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            logger.Debug("Cancellation requested. Preventing further requests");
                            return;
                        }
                        if (!browserContextPool.TryPop(out var browserContext))
                        {
                            logger.Error("Could not get BrowserContext from Stack");
                            return;
                        }

                        var branchUrl = $"{uri}?branch_id={branch.Key}";
                        (bool error, decimal? price) = await RequestProductPageInBrowser(browserContext, branchUrl);
                        browserContextPool.Push(browserContext);
                        results.TryAdd(branch.Key, new Result()
                        {
                            Price = price is not null ? price.ToString() + "€" : "N/A",
                            PriceDecimal = price ?? decimal.MaxValue,
                            BranchId = branch.Key,
                            BranchName = branch.Value,
                            Url = branchUrl
                        });
                        if (error)
                        {
                            Interlocked.Increment(ref errors);
                        }
                    }
                    finally
                    {
                        Interlocked.Increment(ref branchesDone);
                        var statusMessage = $"Progress: {branchesDone}/{branchesTotal} branches";
                        logger.Information(statusMessage);
                        statusDictionary.AddOrUpdate(uri.ToString(), _ => statusMessage, (_, _) => statusMessage);
                    }
                });

                logger.Information("Finished Crawling with {errorCount} Errors. {successBranches}/{branchesTotal} branches had prices", errors, results.Values.Count(x => x.PriceDecimal < decimal.MaxValue), branchesTotal);
                return results.Values.OrderBy(x => x.PriceDecimal).ToList();
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Something bad happened, Sorry ;(");
                throw;
            }
            finally
            {
                statusDictionary.Remove(uri.ToString(), out _);
                if (errors > configuration.MaxErrorsAllowed)
                {
                    memoryCache.GetOrCreate("disabledUntil", (e) =>
                    {
                        var expireDate = DateTimeOffset.UtcNow.AddMinutes(10);
                        e.SetAbsoluteExpiration(expireDate);
                        return expireDate;
                    });
                }
            }
        }

        private static async Task<ConcurrentStack<BrowserContext>> CreateBrowserContextPool(Browser browser)
        {
            var browserContextPool = new ConcurrentStack<BrowserContext>();
            for (var i = 0; i < configuration.MaxParallelRequests; i++)
            {
                browserContextPool.Push(await browser.CreateIncognitoBrowserContextAsync());
            }

            return browserContextPool;
        }

        static async Task<(bool Error, decimal? Price)> RequestProductPageInBrowser(BrowserContext browserContext, string productUrl)
        {
            logger.Debug("Requesting {url}", productUrl);
            await using var page = await browserContext.NewPageAsync();
            await page.SetUserAgentAsync(configuration.UserAgent);
            await page.SetJavaScriptEnabledAsync(false);
            await page.SetRequestInterceptionAsync(true);
            page.Request += async (_, args) =>
            {
                var req = args.Request;
                if (req.ResourceType != ResourceType.Document)
                {
                    await req.AbortAsync();
                }
                else
                {
                    await req.ContinueAsync();
                }
            };

            var retries = configuration.Retries;

            Response response = await page.GoToAsync(productUrl);
            while (response.Status != System.Net.HttpStatusCode.OK && retries-- > 0) {
                response = await page.ReloadAsync();
            }

            if (response.Status != System.Net.HttpStatusCode.OK)
            {
                logger.Warning("Failed to retrieve {url} after {retries} retries: Status {status}", productUrl, configuration.Retries, (int)response.Status);
                logger.Debug("{body}", await page.GetContentAsync());
                return (true, null);
            }
            return (false, await FindPrice(page));
        }

        static async Task<decimal?> FindPrice(Page page)
        {
            try
            {
                var handle = await page.WaitForSelectorAsync("div[itemProp=\"price\"]", new WaitForSelectorOptions() { Timeout = 5000 });
                var property = await handle.GetPropertyAsync("innerText");
                var innerText = await property.JsonValueAsync();
                return innerText is string priceString && !string.IsNullOrWhiteSpace(priceString) ? decimal.Parse(configuration.PriceRegex.Match(priceString).Value, configuration.Culture) : null;
            }
            catch (Exception e)
            {
                logger.Debug(e, "Error finding Price for {pageUrl}", page.Url);
                return null;
            }
        }

        private static async Task EnsureBrowserAvailable()
        {
            if (configuration.PuppeteerLaunchOptions.ExecutablePath is null)
            {
                logger.Information("Initializing. This may take some minutes...");
                using var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
            }
        }
    }
}