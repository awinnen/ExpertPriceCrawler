using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using PuppeteerSharp;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ExpertPriceCrawler
{
    public class ApiCrawler: IProductCrawler
    {
        private static ILogger logger => Configuration.Logger;
        private static ConfigurationValues configuration => Configuration.Instance;
        private static IMemoryCache memoryCache = Configuration.MemoryCache;

        public async Task<List<Result>> CollectPrices(Uri uri)
        {
            uri = uri.MakeExpertCrawlUri();

            await EnsureBrowserAvailable();

            logger.Information($"Requested Prices for {uri}");
            var (cookies, webCode, userAgent, productName) = await GetRequiredInformation(uri.ToString());

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(Configuration.Instance.ExpertBaseUrl);
            httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, userAgent);
            //httpClient.DefaultRequestHeaders.Add("csrf-token", csrfToken);

            return await memoryCache.GetOrCreateAsync(uri.ToString(), async e =>
            {
                logger.Verbose("Result not in cache. Crawling with {maxParallel} parallel requests", configuration.MaxParallelRequests);
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(configuration.MemoryCacheMinutes);
                var result = await GetResultForUri(httpClient, uri, webCode, cookies, CancellationToken.None);
                result.ForEach(r => {
                    r.ProductName = productName;
                });
                return result;
            });
        }

        private static async Task<List<Result>> GetResultForUri(HttpClient httpClient, Uri uri, string webCode, Dictionary<string, string> cookies, CancellationToken cancellationToken)
        {

            var results = new ConcurrentDictionary<string, Result>();
            var branchesTotal = configuration.Branches.Count;
            var branchesDone = 0;
            var timer = new Stopwatch();

            timer.Start();
            await Parallel.ForEachAsync(configuration.Branches, new ParallelOptions
            {
                MaxDegreeOfParallelism = configuration.MaxParallelRequests,
                CancellationToken = cancellationToken
            }, async (branch, cancellationToken) =>
            {
                try
                {
                    results.TryAdd(branch.Key, await GetResultForBranch(httpClient, uri.ToString(), webCode, cookies, branch));
                }
                finally
                {
                    Interlocked.Increment(ref branchesDone);
                    var statusMessage = $"Progress: {branchesDone}/{branchesTotal} branches";
                    logger.Information(statusMessage);
                }
            });
            timer.Stop();
            logger.Information($"Time took: {timer.Elapsed.TotalSeconds} seconds");
            return results.Values.ToList();
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

        static async Task<(Dictionary<string, string> cookies, string webCode, string userAgent, string productName)> GetRequiredInformation(string productUrl)
        {
            await using var browser = await Puppeteer.LaunchAsync(configuration.PuppeteerLaunchOptions);
            await using var page = await browser.NewPageAsync();
            await page.SetUserAgentAsync(configuration.UserAgent);
            await page.SetJavaScriptEnabledAsync(false);

            var response = await page.GoToAsync(productUrl);

            var userAgent = await page.EvaluateExpressionAsync<string>("window.navigator.userAgent");

            var productPage = await response.TextAsync();
            var cookies = await page.GetCookiesAsync(productUrl);
            var cookieList = cookies.ToDictionary(c => c.Name, c => c.Value);

            var webCode = configuration.WebCodeRegex.Match(productPage);

            if (!webCode.Success)
            {
                throw new Exception("webCode not found on Page");
            }

            var productName = await FindProductNameAndImage(page);
            return (cookieList, webCode.Groups[1].Value, userAgent, productName);
        }

        static async Task<string> FindProductNameAndImage(IPage page)
        {
            var handleTitle = await page.WaitForSelectorAsync("head title", new WaitForSelectorOptions() { Timeout = 1000 });
            var productName = await handleTitle.EvaluateFunctionAsync<string>("(el) => el.innerText", handleTitle);
            return (productName?.Replace("- bei expert kaufen", string.Empty)?.Trim());
        }

        static async Task<Result> GetResultForBranch(HttpClient httpClient, string productUrl, string webCode, Dictionary<string, string> cookies, KeyValuePair<string, string> branch)
        {
            var branchUrl = $"{productUrl}?branch_id={branch.Key}";
            try
            {
                var price = await RequestAPIforWebCode(httpClient, webCode, branch.Key);
                return new Result()
                {
                    BranchId = branch.Key,
                    BranchName = branch.Value,
                    Price = price.price is not null ? $"{price.price}€" : "N/A",
                    PriceDecimal = price.price ?? decimal.MaxValue,
                    Url = branchUrl,
                    IsExhibition = price.isOnDisplay ?? false
                };
            }
            catch (Exception ex)
            {
                return new Result()
                {
                    BranchId = branch.Key,
                    BranchName = branch.Value,
                    Price = $"N/A",
                    PriceDecimal = Decimal.MaxValue,
                    Url = branchUrl
                };
            }
        }
        static async Task<(bool error, decimal? price, bool? isOnDisplay)> RequestAPIforWebCode(HttpClient clients, string webCode, string branchCode, int maxRetries = 3)
        {
            string apiUrl = $"https://shop.brntgs.expert.de/api/pricepds?webcode={webCode}&storeId={branchCode}";

            for (int retry = 1; retry <= maxRetries; retry++)
            {
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
                        HttpResponseMessage response = await client.GetAsync(apiUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();

                            var responseObj = JsonSerializer.Deserialize<ExpertApiResponseBody>(responseBody);

                            if (responseObj?.price?.bruttoPrice != 0)
                            {
                                logger.Debug($"Success retrieving price. price: {responseObj?.price?.bruttoPrice}");
                                return (false, responseObj.price.bruttoPrice, responseObj?.price?.itemOnDisplay ?? false);
                            }
                            else
                            {
                                logger.Debug($"Failed to retrieve price. price: {responseObj?.price?.bruttoPrice} -- Status code: {response.StatusCode}");
                                return (true, null, null);
                            }
                        }
                        else
                        {
                            logger.Debug($"Failed to retrieve data. url: {apiUrl}");
                            return (true, null, null);
                        }
                    }
                    catch (TaskCanceledException timeoutE)
                    {
                        if (retry < maxRetries)
                        {
                            logger.Debug($"Ran into a timeout, retrying: {retry}");
                            await Task.Delay(2000);
                            continue;
                        }
                        else
                        {
                            logger.Debug(timeoutE, $"Maxed out on request retries for URL{apiUrl}");
                            return (true, null, null);
                        }
                    }
                    catch (Exception e)
                    {

                        logger.Debug(e, $"Maxed out on request retries for URL{apiUrl}");
                        return (true, null, null);

                    }
                }
            }
            // Should not reach here
            return (true, null, null);
        }
    }
}
