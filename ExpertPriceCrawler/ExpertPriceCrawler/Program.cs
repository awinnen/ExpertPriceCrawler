using CefSharp;
using CefSharp.OffScreen;
using Microsoft.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ConsoleTables;
using ExpertChecker;
using ExpertPriceCrawler;
using System.Diagnostics;

static class Programm
{
    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var uri = ReadUrl();
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
                    Url = $"{uri}?branch_id={branch.Key}"
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        var BranchPrices = new List<Result>();

        var chunks = Constants.Branches.Chunk(Constants.ChunkSize);
        var current = 0;
        var total = chunks.Count();
        foreach (var chunk in chunks)
        {
            Console.WriteLine($"Fetching chunk. Please wait... ({++current}/{total})");
            var results = await Task.WhenAll(chunk.Select(c => AddToCart(c)));
            BranchPrices.AddRange(results.Where(r => r != null).Cast<Result>());
        }

        BranchPrices = BranchPrices.OrderBy(x => x.Price).ToList();

        int done = 0;
        int printRowCount = 10;
        do
        {
            ConsoleTable.From(BranchPrices.OrderBy(x => x.Price).Skip(done).Take(printRowCount)).Write(Format.Minimal);
            done += printRowCount;
            if (done < BranchPrices.Count)
            {
                Console.WriteLine("Press any Key to print next Results!");
            }
        } while (done < BranchPrices.Count && Console.ReadKey(true) != null);

        Console.WriteLine("Press any Key to Close this Window");
        Console.ReadKey(true);
        Environment.Exit(0);
    }

    static async Task<(Dictionary<string, string>, string cartId, string articleId, string csrfToken, string userAgent)> RequestProductPage(string productUrl)
    {
        Console.WriteLine("Please wait...");

        using (var browser = new ChromiumWebBrowser(productUrl))
        {
            var initialLoadResponse = await browser.WaitForInitialLoadAsync();

            if (!initialLoadResponse.Success)
            {
                throw new Exception(string.Format("Page load failed with ErrorCode:{0}, HttpStatusCode:{1}", initialLoadResponse.ErrorCode, initialLoadResponse.HttpStatusCode));
            }

            var cookies = await browser.GetCookieManager().VisitUrlCookiesAsync(productUrl, true);
            var cookieList = cookies.ToDictionary(c => c.Name, c => c.Value);


            //Give the browser a little time to render
            await Task.Delay(500);

            var userAgent = (await browser.EvaluateScriptAsync("window.navigator.userAgent")).Result as string;
            var productPage = await browser.GetSourceAsync();
            var cartId = Constants.CartIdRegex.Match(productPage);
            var articleId = Constants.ArticleIdRegex.Match(productPage);
            var csrfToken = Constants.CsrfTokenRegex.Match(productPage);

            if (!(cartId.Success && articleId.Success && csrfToken.Success))
            {
                throw new Exception("CartId, ArticleId or CsrfToken not found on Page");
            }

            return (cookieList, articleId.Groups[1].Value, cartId.Groups[1].Value, csrfToken.Groups[1].Value, userAgent);
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

        var shoppingCartResponse = await client.PostAsync(Constants.ShoppingCartUrl, content);
        shoppingCartResponse.EnsureSuccessStatusCode();
        var body = await shoppingCartResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AddItemResponse>(body, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        });
        return result.ShoppingCart.LastAdded.Price.Gross;
    }


    static Uri ReadUrl()
    {
        try
        {
            Console.WriteLine("Bitte Produkt-URL von Expert eingeben:");
            var input = Console.ReadLine();
            return new Uri(input);
        }
        catch (UriFormatException)
        {
            Console.WriteLine("Url ungültig");
            return ReadUrl();
        }
    }
}