using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using PuppeteerSharp;
using System.Text;
using System.Text.Json;

namespace ExpertPriceCrawler
{
    public static class Crawler
    {
        private static IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        public static async Task<List<Result>> CollectPrices(Uri uri)
        {
            return await memoryCache.GetOrCreateAsync(uri.ToString(), async e => {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Constants.MemoryCacheMinutes);

                var (cookies, articleId, cartId, csrfToken, userAgent) = await RequestProductPage(uri.ToString());

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("csrf-token", csrfToken);
                httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, userAgent);

                async Task<Result?> AddToCart(KeyValuePair<string, string> branch)
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
                            Url = $"{uri}?branch_id={branch.Key}"
                        };
                    }
                    catch (Exception ex)
                    {
                        return new Result()
                        {
                            BranchId = branch.Key,
                            BranchName = branch.Value,
                            Price = $"-",
                            PriceDecimal = Decimal.MaxValue,
                            Url = string.Empty
                        };
                    }
                }

                var branchPrices = new List<Result>();

                var chunks = Constants.Branches.Chunk(Constants.ChunkSize);
                var current = 0;
                var total = chunks.Count();
                foreach (var chunk in chunks)
                {
                    Console.WriteLine($"Fetching chunk. Please wait... ({++current}/{total})");
                    var results = await Task.WhenAll(chunk.Select(c => AddToCart(c)));
                    branchPrices.AddRange(results.Where(r => r != null).Cast<Result>());
                }

                branchPrices = branchPrices.OrderBy(x => x.PriceDecimal).ToList();

                return branchPrices;
            });
        }

        static async Task<(Dictionary<string, string>, string cartId, string articleId, string csrfToken, string userAgent)> RequestProductPage(string productUrl)
        {
            Console.WriteLine("Please wait...");

            if(Constants.LaunchOptions.ExecutablePath is null)
            {
                using var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
            }
            await using var browser = await Puppeteer.LaunchAsync(Constants.LaunchOptions);
            await using var page = await browser.NewPageAsync();
            await page.SetUserAgentAsync(Constants.UserAgent);
            await page.SetJavaScriptEnabledAsync(true);


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

            var shoppingCartResponse = await client.PostAsync(Constants.ShoppingCartUrl, content);
            shoppingCartResponse.EnsureSuccessStatusCode();
            var body = await shoppingCartResponse.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AddItemResponse>(body, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });
            return result.ShoppingCart.LastAdded.Price.Gross;
        }
    }
}