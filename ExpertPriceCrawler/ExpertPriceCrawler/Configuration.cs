using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using Serilog;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ExpertPriceCrawler
{
    public static class Configuration
    {
        public static readonly ConfigurationValues Instance = new ConfigurationValues();
        public static ILogger Logger;

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

            Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        }
    }

    public class ConfigurationValues
    {
        /// <summary>
        /// Useragent to use for chromium when requesting the website
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// max amount of parallel requests. Reduce this value if the system is lagging :P
        /// </summary>
        public int MaxParallelRequests { get; set; }

        /// <summary>
        /// Cache Results for some Minutes. Useful for Website so that subsequent requests of the same product won't be crawles again
        /// </summary>
        public int MemoryCacheMinutes { get; set; }

        /// <summary>
        /// Options for Puppeteer launcher
        /// </summary>
        public LaunchOptions PuppeteerLaunchOptions { get; set; }

        /// <summary>
        /// Dictionary of expert branches in Form (BranchId, BranchName>)
        /// </summary>
        public Dictionary<string, string> Branches { get; set; }

        public readonly Regex PriceRegex = new Regex(@"[\d\,\.]+", RegexOptions.Compiled);
        public readonly Regex PriceRegexItemProp = new Regex("itemprop=\"price\".*?([\\d\\,\\.]+)", RegexOptions.Compiled);
        public readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        internal ConfigurationValues() { }
    }
}
