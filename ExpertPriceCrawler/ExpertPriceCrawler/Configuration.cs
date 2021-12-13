using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExpertPriceCrawler
{
    public class Configuration
    {
        public static Configuration Instance = new Configuration();
        public string UserAgent { get; set; }
        public int MaxParallelRequests { get; set; }
        public int MemoryCacheMinutes { get; set; }
        public LaunchOptions PuppeteerLaunchOptions { get; set; }
        public Dictionary<string, string> Branches { get; set; }

        public readonly Regex PriceRegex = new Regex(@"[\d\,\.]+", RegexOptions.Compiled);
        public readonly Regex PriceRegexItemProp = new Regex("itemprop=\"price\".*?([\\d\\,\\.]+)", RegexOptions.Compiled);
        public readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        public static void Init(string environment)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("branches.json", optional: false)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true);

            var configuration = builder.Build();
            configuration.Bind("Options", Instance);
            configuration.Bind(Instance);

        }
    }
}
