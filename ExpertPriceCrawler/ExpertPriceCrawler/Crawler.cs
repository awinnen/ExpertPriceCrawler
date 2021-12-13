using Microsoft.Extensions.Caching.Memory;
using PuppeteerSharp;
using System.Collections.Concurrent;

namespace ExpertPriceCrawler
{
    public static class Crawler
    {
        private static Configuration Configuration => Configuration.Instance;
        private static IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        public static ConcurrentDictionary<string, string> StatusDictionary = new ConcurrentDictionary<string, string>();

        public static async Task<List<Result>> CollectPrices(Uri uri)
        {
            uri = uri.MakeExpertUri();

            await EnsureBrowserAvailable();

            return await memoryCache.GetOrCreateAsync(uri.ToString(), async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Configuration.MemoryCacheMinutes);
                return await GetResultForUri(uri, CancellationToken.None);
            });
        }

        private static async Task<List<Result>> GetResultForUri(Uri uri, CancellationToken cancellationToken)
        {
            try
            {
                using var browser = await Puppeteer.LaunchAsync(Configuration.PuppeteerLaunchOptions);
                ConcurrentStack<BrowserContext> browserContextPool = await CreateBrowserContextPool(browser);

                var results = new ConcurrentDictionary<string, Result>();
                var branchesTotal = Configuration.Branches.Count;
                var branchesDone = 0;

                await Parallel.ForEachAsync(Configuration.Branches, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Configuration.MaxParallelRequests,
                    CancellationToken = cancellationToken
                }, async (branch, cancellationToken) =>
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        if (!browserContextPool.TryPop(out var browserContext))
                        {
                            Console.Error.WriteLine("Could not get BrowserContext from Stack");
                            return;
                        }

                        var branchUrl = $"{uri}?branch_id={branch.Key}";
                        var price = await RequestProductPageInBrowser(browserContext, branchUrl);
                        browserContextPool.Push(browserContext);
                        results.TryAdd(branch.Key, new Result()
                        {
                            Price = price is not null ? price.ToString() + "€" : "N/A",
                            PriceDecimal = price ?? decimal.MaxValue,
                            BranchId = branch.Key,
                            BranchName = branch.Value,
                            Url = branchUrl
                        });
                    }
                    finally
                    {
                        Interlocked.Increment(ref branchesDone);
                        var statusMessage = $"Progress: ({branchesDone}/{branchesTotal})";
                        Console.WriteLine(statusMessage);
                        StatusDictionary.AddOrUpdate(uri.ToString(), _ => statusMessage, (_, _) => statusMessage);
                    }
                });

                return results.Values.OrderBy(x => x.PriceDecimal).ToList();
            }
            finally
            {
                StatusDictionary.Remove(uri.ToString(), out _);
            }
        }

        private static async Task<ConcurrentStack<BrowserContext>> CreateBrowserContextPool(Browser browser)
        {
            var browserContextPool = new ConcurrentStack<BrowserContext>();
            for (var i = 0; i < Configuration.MaxParallelRequests; i++)
            {
                browserContextPool.Push(await browser.CreateIncognitoBrowserContextAsync());
            }

            return browserContextPool;
        }

        static async Task<decimal?> RequestProductPageInBrowser(BrowserContext browserContext, string productUrl)
        {
            await using var page = await browserContext.NewPageAsync();
            await page.SetUserAgentAsync(Configuration.UserAgent);
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


            var response = await page.GoToAsync(productUrl);
            return await FindPrice(page);
        }

        static async Task<decimal?> FindPrice(Page page)
        {
            try
            {
                var handle = await page.WaitForSelectorAsync("div[itemProp=\"price\"]", new WaitForSelectorOptions() { Timeout = 1000 });
                var property = await handle.GetPropertyAsync("innerText");
                var innerText = await property.JsonValueAsync();
                return innerText is string priceString && !string.IsNullOrWhiteSpace(priceString) ? decimal.Parse(Configuration.PriceRegex.Match(priceString).Value, Configuration.Culture) : null;
            }
            catch
            {
                return null;
            }
        }

        private static async Task EnsureBrowserAvailable()
        {
            if (Configuration.PuppeteerLaunchOptions.ExecutablePath is null)
            {
                Console.WriteLine("Initializing. This may take some minutes...");
                using var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
            }
        }
    }
}