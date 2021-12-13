using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using PuppeteerSharp;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ExpertPriceCrawler
{
    public static class Crawler
    {
        private static IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        private static KeyValuePair<string, string>[][] chunks = Constants.Branches.Chunk(Constants.ChunkSize).ToArray();
        private static int chunkCount = chunks.Count();
        public static ConcurrentDictionary<string, string> StatusDictionary = new ConcurrentDictionary<string, string>();


        public static async Task<List<Result>> CollectPrices(Uri uri)
        {
            if (Constants.LaunchOptions.ExecutablePath is null)
            {
                using var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
            }

            using var browser = await Puppeteer.LaunchAsync(Constants.LaunchOptions);
            //var requiredInformation = await GetRequiredInformation(uri.ToString());
            //using var httpClient = new HttpClient();
            //httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, requiredInformation.userAgent);

            var browserContexts = new BrowserContext[Constants.ChunkSize];
            for (var i = 0; i < Constants.ChunkSize; i++)
            {
                browserContexts[i] = await browser.CreateIncognitoBrowserContextAsync();
            }

            return await memoryCache.GetOrCreateAsync(uri.ToString(), async e =>
            {
                try
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Constants.MemoryCacheMinutes);

                    var branchPrices = new List<Result>();

                    var current = 0;
                    foreach (var chunk in chunks)
                    {
                        var statusMessage = $"Fetching chunk. Please wait... ({++current}/{chunkCount})";
                        StatusDictionary.AddOrUpdate(uri.ToString(), (_) => statusMessage, (_, _) => statusMessage);
                        Console.WriteLine(statusMessage);

                        var results = await Task.WhenAll(chunk.Select(async (branch, index) =>
                        {
                            var price = await RequestProductPageInBrowser(browserContexts[index], $"{uri}?branch_id={branch.Key}");
                            //var price = await RequestProductPageWithHttpClient(httpClient, $"{uri}?branch_id={branch.Key}", branch.Key, requiredInformation.cookies);

                            return new Result()
                            {
                                Price = price is not null ? price.ToString() + "€" : "N/A",
                                PriceDecimal = price ?? decimal.MaxValue,
                                BranchId = branch.Key,
                                BranchName = branch.Value,
                                Url = $"{uri}?branch_id={branch.Key}"
                            };
                        }));
                        branchPrices.AddRange(results.Cast<Result>());
                    }

                    branchPrices = branchPrices.OrderBy(x => x.PriceDecimal).ToList();

                    return branchPrices;
                }
                finally
                {
                    StatusDictionary.Remove(uri.ToString(), out _);
                }
            });
        }

        static async Task<(Dictionary<string, string> cookies, string cartId, string articleId, string csrfToken, string userAgent)> GetRequiredInformation(string productUrl)
        {
            await using var browser = await Puppeteer.LaunchAsync(Constants.LaunchOptions);
            await using var page = await browser.NewPageAsync();
            await page.SetUserAgentAsync(Constants.UserAgent);
            await page.SetJavaScriptEnabledAsync(false);

            var response = await page.GoToAsync(productUrl);

            var userAgent = await page.EvaluateExpressionAsync<string>("window.navigator.userAgent");

            var productPage = await response.TextAsync();
            var cookies = await page.GetCookiesAsync(productUrl);
            var cookieList = cookies.ToDictionary(c => c.Name, c => c.Value);

            var cartId = Constants.CartIdRegex.Match(productPage);
            var articleId = Constants.ArticleIdRegex.Match(productPage);
            var csrfToken = Constants.CsrfTokenRegex.Match(productPage);

            if (!(cartId.Success && articleId.Success && csrfToken.Success))
            {
                throw new Exception("CartId, ArticleId or CsrfToken not found on Page");
            }

            return (cookieList, articleId.Groups[1].Value, cartId.Groups[1].Value, csrfToken.Groups[1].Value, userAgent);
        }

        static async Task<decimal?> RequestProductPageWithHttpClient(HttpClient httpClient, string productUrl, string branchId, Dictionary<string, string> cookies)
        {
            var content = new StringContent(String.Empty);
            httpClient.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies.Where(x => x.Key != "fmarktcookie").Select(c => $"{c.Key}={c.Value}")));
            var response = await httpClient.SendAsync(new HttpRequestMessage() { Content = content, RequestUri = new Uri(productUrl) });
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return null;

            var match = Constants.PriceRegexItemProp.Match(text);

            return match.Success && match.Groups[1].Success ? decimal.Parse(match.Groups[1].Value, Constants.Culture) : null;
        }

        static async Task<decimal?> RequestProductPageInBrowser(BrowserContext browserContext, string productUrl)
        {
            await using var page = await browserContext.NewPageAsync();
            await page.SetUserAgentAsync(Constants.UserAgent);
            await page.SetJavaScriptEnabledAsync(true);
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
            var price = await FindPrice(page);

            return price;
        }

        static async Task<decimal?> FindPrice(Page page)
        {
            try
            {
                var handle = await page.WaitForSelectorAsync("div[itemProp=\"price\"]", new WaitForSelectorOptions() { Timeout = 5000 });
                var property = await handle.GetPropertyAsync("innerText");
                var innerText = await property.JsonValueAsync();
                return innerText is string priceString ? decimal.Parse(Constants.PriceRegex.Match(priceString).Value, Constants.Culture) : null;
            }
            catch
            {
                return null;
            }
        }

        static async Task<Result> AddToCart(HttpClient httpClient, string productUrl, string articleId, string cartId, Dictionary<string, string> cookies, KeyValuePair<string, string> branch)
        {
            try
            {
                var price = await RequestBranch(httpClient, articleId, cartId, branch.Key, cookies);
                return new Result()
                {
                    BranchId = branch.Key,
                    BranchName = branch.Value,
                    Price = $"{price}€",
                    PriceDecimal = price,
                    Url = $"{productUrl}?branch_id={branch.Key}"
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
                    Url = $"{productUrl}?branch_id={branch.Key}"
                };
            }
        }

        static async Task<decimal> RequestBranch(HttpClient client, string articleId, string cartId, string branchId, Dictionary<string, string> cookies)
        {
            var payload = JsonSerializer.Serialize(new
            {
                shoppingCartId = cartId,
                quantity = 1,
                article = articleId,
            });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            cookies["fmarktcookie"] = $"e_{branchId}";
            content.Headers.Add("Cookie", string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}")));

            var shoppingCartResponse = await client.PostAsync(Constants.AddItemUrl, content);
            var body = await shoppingCartResponse.Content.ReadAsStringAsync();
            if (!shoppingCartResponse.IsSuccessStatusCode)
            {
                Console.Error.WriteLine(body);
            }
            shoppingCartResponse.EnsureSuccessStatusCode();
            var result = JsonSerializer.Deserialize<AddItemResponse>(body, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });

            // Remove item from ShoppingCart
            await client.PostAsync(Constants.ModifyItemQuantityUrl, new StringContent(JsonSerializer.Serialize(new
            {
                itemId = result.ItemId,
                quantity = 0,
                shoppingCartId = cartId,
            }), Encoding.UTF8, "application/json"));

            return result.ShoppingCart.LastAdded.Price.Gross;
        }
    }
}