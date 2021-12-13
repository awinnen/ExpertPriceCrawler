using PuppeteerSharp;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExpertPriceCrawler
{
    public static class Constants
    {
        public const string AddItemUrl = "https://www.expert.de/_api/shoppingcart/addItem";
        public const string ModifyItemQuantityUrl = "https://www.expert.de/_api/shoppingcart/modifyItemQuantity";
        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.93 Safari/537.36";
        public const int ChunkSize = 20;
        public const int MemoryCacheMinutes = 10;

        public const string CartIdPattern = "data-cart-id=\"(.+?)\"";
        public const string ArticleIdPattern = "data-article-id=\"(.+?)\"";
        public const string CsrfTokenPattern = "content=\"(.+?)\".*(csrf-token)";

        public static Regex CartIdRegex = new Regex(CartIdPattern, RegexOptions.Compiled);
        public static Regex ArticleIdRegex = new Regex(ArticleIdPattern, RegexOptions.Compiled);
        public static Regex CsrfTokenRegex = new Regex(CsrfTokenPattern, RegexOptions.Compiled);
        public static Regex PriceRegex = new Regex(@"[\d\,\.]+", RegexOptions.Compiled);
        public static Regex PriceRegexItemProp = new Regex("itemprop=\"price\".*?([\\d+\\,\\.]+)", RegexOptions.Compiled);

        public static CultureInfo Culture = CultureInfo.InvariantCulture;
        public static LaunchOptions LaunchOptions = new LaunchOptions
        {
            Headless = true
        };

        public static Dictionary<string, string> Branches = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,  "branches.json")));
    }
}
