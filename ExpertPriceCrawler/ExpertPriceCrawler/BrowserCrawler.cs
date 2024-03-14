using Microsoft.Extensions.Caching.Memory;
using PuppeteerSharp;
using Serilog;
using System.Collections.Concurrent;

namespace ExpertPriceCrawler
{
    public class BrowserCrawler: IProductCrawler
    {
        private static ILogger logger => Configuration.Logger;
        private static ConfigurationValues configuration => Configuration.Instance;
        private static IMemoryCache memoryCache = Configuration.MemoryCache;

        public async Task<List<Result>> CollectPrices(Uri uri)
        {
            uri = uri.NormalizeUri();

            await EnsureBrowserAvailable();

            logger.Information($"Requested Prices for {uri}");
            return await memoryCache.GetOrCreateAsync(uri.ToString(), async e =>
            {
                logger.Verbose("Result not in cache. Crawling with {maxParallel} parallel requests", configuration.MaxParallelRequests);
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(configuration.MemoryCacheMinutes);
                return await GetResultForUri(uri.MakeExpertCrawlUri(), uri, CancellationToken.None);
            });
        }

        private static async Task<List<Result>> GetResultForUri(Uri crawlUrl, Uri originalUrl, CancellationToken cancellationToken)
        {
            var errors = 0;
            try
            {
                using var browser = await Puppeteer.LaunchAsync(configuration.PuppeteerLaunchOptions);
                ConcurrentStack<IBrowserContext> browserContextPool = await CreateBrowserContextPool(browser);

                var results = new ConcurrentDictionary<string, Result>();
                var branchesTotal = configuration.Branches.Count;
                var branchesDone = 0;

                await Parallel.ForEachAsync(configuration.Branches, new ParallelOptions
                {
                    MaxDegreeOfParallelism = configuration.MaxParallelRequests,
                    CancellationToken = cancellationToken
                }, async (branch, cToken) =>
                {
                    try
                    {
                        if (cToken.IsCancellationRequested)
                        {
                            logger.Debug("Cancellation requested. Preventing further requests");
                            return;
                        }
                        if (errors > configuration.MaxErrorsAllowed)
                        {
                            return;
                        }
                        if (!browserContextPool.TryPop(out var browserContext))
                        {
                            logger.Error("Could not get BrowserContext from Stack");
                            return;
                        }

                        var crawlUrlWithBranchQueryParameter = $"{crawlUrl}?branch_id={branch.Key}";
                        var originalUrlWithBranchQueryParameter = $"{originalUrl}?branch_id={branch.Key}";
                        (bool error, decimal? price, bool isExhibition, string productName, string productImageUrl) = await RequestProductPageInBrowser(browserContext, crawlUrlWithBranchQueryParameter);
                        browserContextPool.Push(browserContext);
                        results.TryAdd(branch.Key, new Result()
                        {
                            Price = error ? "ERROR" : price is not null ? price.ToString() + "€" : "N/A",
                            PriceDecimal = error ? decimal.MaxValue : price ?? decimal.MaxValue -1,
                            IsExhibition = isExhibition,
                            BranchId = branch.Key,
                            BranchName = branch.Value,
                            Url = originalUrlWithBranchQueryParameter,
                            ProductImage = productImageUrl,
                            ProductName = productName,
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
                    }
                });

                logger.Information("Finished Crawling with {errorCount} Errors. {successBranches}/{branchesTotal} branches had prices", errors, results.Values.Count(x => x.PriceDecimal < decimal.MaxValue-1), branchesTotal);
                return results.Values.OrderBy(x => x.PriceDecimal).ToList();
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Something bad happened, Sorry ;(");
                throw;
            }
            finally
            {
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

        private static async Task<ConcurrentStack<IBrowserContext>> CreateBrowserContextPool(IBrowser browser)
        {
            var browserContextPool = new ConcurrentStack<IBrowserContext>();
            for (var i = 0; i < configuration.MaxParallelRequests; i++)
            {
                browserContextPool.Push(await browser.CreateIncognitoBrowserContextAsync());
            }

            return browserContextPool;
        }

        static async Task<(bool Error, decimal? Price, bool isExhibition, string productName, string imageUrl)> RequestProductPageInBrowser(IBrowserContext browserContext, string productUrl)
        {
            try
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
                var retryDelayInMinutes = 1;

                IResponse response = await page.GoToAsync(productUrl);
                while (response.Status != System.Net.HttpStatusCode.OK && retries-- > 0)
                {
                    if (response.Status is System.Net.HttpStatusCode.TooManyRequests)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(retryDelayInMinutes + (configuration.Retries - retries)));
                    }
                    response = await page.ReloadAsync();
                }

                if (response.Status != System.Net.HttpStatusCode.OK)
                {
                    logger.Warning("Failed to retrieve {url} after {retries} retries: Status {status}", productUrl, configuration.Retries, (int)response.Status);
                    logger.Debug("{body}", await page.GetContentAsync());
                    return (true, null, false, null, null);
                }
                var (name, image) = await FindProductNameAndImage(page);
                return (false, await FindPrice(page), await FindExhibitionStatus(page), name, image);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Something unexpected happened while retrieving {}", productUrl);
                return (true, null, false, null, null);
            }
        }

        static async Task<(string productName, string productImageUrl)> FindProductNameAndImage(IPage page)
        {
            var handleImage = await page.WaitForSelectorAsync(".image-item", new WaitForSelectorOptions() { Timeout = 1000 });
            var handleTitle = await page.WaitForSelectorAsync("head title", new WaitForSelectorOptions() { Timeout = 1000 });
            var productImageUrl = await handleImage.EvaluateFunctionAsync<string>("(el) => el.dataset.img", handleImage);
            var productName = await handleTitle.EvaluateFunctionAsync<string>("(el) => el.innerText", handleTitle);
            return (productName?.Replace("- bei expert kaufen", string.Empty)?.Trim(), productImageUrl);
        }

        static async Task<bool> FindExhibitionStatus(IPage page)
        {
            try
            {
                var handle = await page.WaitForSelectorAsync(".articleExhibit.mr-2", new WaitForSelectorOptions() { Timeout = 10 });
                return true;
            }
            catch(Exception e)
            {
                return false;
            }
        }

        static async Task<decimal?> FindPrice(IPage page)
        {
            try
            {
                await new BrowserFetcher().DownloadAsync();
                var launchOptions = new LaunchOptions
                {
                    Headless = false,
                };
                using (var browser = await Puppeteer.LaunchAsync(launchOptions))
                using (var pageTest = await browser.NewPageAsync())
                {
                    
                    await pageTest.GoToAsync(page.Url);
                    var testcontentt = await pageTest.GetContentAsync();
                    await page.WaitForTimeoutAsync(2000);
                    //var handlet = await pageTest.WaitForSelectorAsync(".exp_price", new WaitForSelectorOptions() { Timeout = 50000 }) ;
                    var handlet = await pageTest.WaitForSelectorAsync("div.flex-col:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > span", new WaitForSelectorOptions() { Timeout = 1000 });
                    //var marktHandle = await pageTest.WaitForSelectorAsync("#\\31  > div.lg\\:container.lg\\:mx-auto.lg\\:flex.lg\\:justify-between > div > div:nth-child(3) > div.local-store.flyout.flex.flex-col.border.border-solid.border-secondary.w-full.left-0.sm\\:left-auto.sm\\:right-0.absolute.sm\\:w-\\[350px\\].text-paragraph > div.storeName.items-center.p-4 > div > div:nth-child(1)", new WaitForSelectorOptions() { Timeout = 50000, Visible = true });
                    //var makrtProperty = await marktHandle.GetPropertyAsync("innerText");
                    //var marktInnerTexts = await makrtProperty.JsonValueAsync();

                    //var xpropertsy = await handlet.GetPropertyAsync("innerText");
                    //var xinnerTexts = await xpropertsy.JsonValueAsync();


                    var propertsy = await handlet.GetPropertyAsync("innerText");
                    var innerTexts = await propertsy.JsonValueAsync();

                    //Console.WriteLine("Markt:" + marktInnerTexts + "Price:" + innerTexts);
                    return innerTexts is string priceStrings && !string.IsNullOrWhiteSpace(priceStrings) ? decimal.Parse(configuration.PriceRegex.Match(priceStrings).Value, configuration.Culture) : null;

                }

                //var testcontent = await page.GetContentAsync();

                //var test = await page.WaitForXPathAsync("/html/body/div[1]/div/main/div/div[1]/div[4]/div[2]/div[2]/div[2]/div[1]/div[2]/div[2]/span", new WaitForSelectorOptions() { Timeout = 50000, Visible = true });
                //var handle = await page.WaitForSelectorAsync(".exp_price", new WaitForSelectorOptions() { Timeout = 300000 });
                //var property = await handle.GetPropertyAsync("innerText");
                //var innerText = await property.JsonValueAsync();
                //return innerText is string priceString && !string.IsNullOrWhiteSpace(priceString) ? decimal.Parse(configuration.PriceRegex.Match(priceString).Value, configuration.Culture) : null;
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