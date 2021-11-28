using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExpertPriceCrawler
{
    internal class Constants
    {
        public const string ShoppingCartUrl = "https://www.expert.de/_api/shoppingcart/addItem";
        public const int ChunkSize = 30;

        public const string CartIdPattern = "data-cart-id=\"(.+?)\"";
        public const string ArticleIdPattern = "data-article-id=\"(.+?)\"";
        public const string CsrfTokenPattern = "content=\"(.+?)\".*(csrf-token)";

        public static Regex CartIdRegex = new Regex(CartIdPattern, RegexOptions.Compiled);
        public static Regex ArticleIdRegex = new Regex(ArticleIdPattern, RegexOptions.Compiled);
        public static Regex CsrfTokenRegex = new Regex(CsrfTokenPattern, RegexOptions.Compiled);

        public static Dictionary<string, string> Branches = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText("branches.json"));
    }
}
