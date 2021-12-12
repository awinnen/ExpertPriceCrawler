using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExpertPriceCrawler
{
    internal static class Constants
    {
        public const string ShoppingCartUrl = "https://www.expert.de/_api/shoppingcart/addItem";
        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.93 Safari/537.36";
        public const int ChunkSize = 30;

        public const string CartIdPattern = "data-cart-id=\"(.+?)\"";
        public const string ArticleIdPattern = "data-article-id=\"(.+?)\"";
        public const string CsrfTokenPattern = "content=\"(.+?)\".*(csrf-token)";

        public static Regex CartIdRegex = new Regex(CartIdPattern, RegexOptions.Compiled);
        public static Regex ArticleIdRegex = new Regex(ArticleIdPattern, RegexOptions.Compiled);
        public static Regex CsrfTokenRegex = new Regex(CsrfTokenPattern, RegexOptions.Compiled);

        public static Dictionary<string, string> Branches = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory +  @"\branches.json"));
    }
}
