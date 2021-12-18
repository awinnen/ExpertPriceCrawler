using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using PuppeteerSharp;
using Serilog;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace ExpertPriceCrawler
{
    public static class ApiCrawler
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
            var (cookies, cartId, articleId, csrfToken, userAgent) = await GetRequiredInformation(uri.ToString());

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, userAgent);
            httpClient.DefaultRequestHeaders.Add("csrf-token", csrfToken);

            return await memoryCache.GetOrCreateAsync(uri.ToString(), async e =>
            {
                logger.Verbose("Result not in cache. Crawling with {maxParallel} parallel requests", configuration.MaxParallelRequests);
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(configuration.MemoryCacheMinutes);
                return await GetResultForUri(httpClient, uri, articleId, cartId, cookies, CancellationToken.None);
            });
        }

        private static async Task<List<Result>> GetResultForUri(HttpClient httpClient, Uri uri, string articleId, string cartId, Dictionary<string, string> cookies, CancellationToken cancellationToken)
        {

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
                    results.TryAdd(branch.Key, await GetResultForBranch(httpClient, uri.ToString(), articleId, cartId, cookies, branch));
                }
                finally
                {
                    Interlocked.Increment(ref branchesDone);
                    var statusMessage = $"Progress: {branchesDone}/{branchesTotal} branches";
                    logger.Information(statusMessage);
                    statusDictionary.AddOrUpdate(uri.ToString(), _ => statusMessage, (_, _) => statusMessage);
                }
            });

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

        static async Task<(Dictionary<string, string> cookies, string cartId, string articleId, string csrfToken, string userAgent)> GetRequiredInformation(string productUrl)
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

            var cartId = configuration.CartIdRegex.Match(productPage);
            var articleId = configuration.ArticleIdRegex.Match(productPage);
            var csrfToken = configuration.CsrfTokenRegex.Match(productPage);

            if (!(cartId.Success && articleId.Success && csrfToken.Success))
            {
                throw new Exception("CartId, ArticleId or CsrfToken not found on Page");
            }

            return (cookieList, cartId.Groups[1].Value, articleId.Groups[1].Value, csrfToken.Groups[1].Value, userAgent);
        }

        static async Task<Result> GetResultForBranch(HttpClient httpClient, string productUrl, string articleId, string cartId, Dictionary<string, string> cookies, KeyValuePair<string, string> branch)
        {
            var branchUrl = $"{productUrl}?branch_id={branch.Key}";
            try
            {
                var price = await GetPrice(httpClient, articleId, cartId, branch.Key, cookies);
                return new Result()
                {
                    BranchId = branch.Key,
                    BranchName = branch.Value,
                    Price = price is not null ? $"{price}€" : "N/A",
                    PriceDecimal = price ?? decimal.MaxValue,
                    Url = branchUrl
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

        static async Task<decimal?> GetPrice(HttpClient client, string articleId, string cartId, string branchId, Dictionary<string, string> cookies)
        {
            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    shoppingCartId = cartId,
                    quantity = 1,
                    article = articleId,
                });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                cookies = cookies.ToDictionary(x => x.Key, x => x.Value);
                cookies["fmarktcookie"] = $"e_{branchId}";
                content.Headers.Add("Cookie", string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}")));

                var shoppingCartResponse = await client.PostAsync(configuration.AddItemUrl, content);
                var body = await shoppingCartResponse.Content.ReadAsStringAsync();
                if (!shoppingCartResponse.IsSuccessStatusCode)
                {
                    logger.Error("Error while adding to shoppingcart, {response}", body);
                    return null;
                }
                shoppingCartResponse.EnsureSuccessStatusCode();
                var result = JsonSerializer.Deserialize<AddItemResponse>(body, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });

                // Remove item from ShoppingCart
                var deleteContent = new StringContent(JsonSerializer.Serialize(new
                {
                    itemId = result.ItemId,
                    quantity = 0,
                    shoppingCartId = cartId,
                }), Encoding.UTF8, "application/json");
                deleteContent.Headers.Add("Cookie", string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}")));
                var deleteResponse = await client.PostAsync(configuration.ModifyItemQuantityUrl, deleteContent);
                return result.ShoppingCart.LastAdded.Price.Gross;
            }
            finally
            {
                await Task.Delay(1000);
            }
        }
    }
}
