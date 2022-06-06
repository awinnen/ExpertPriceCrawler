using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace ExpertPriceCrawler.Web.Pages
{
    public class IndexModel : PageModel
    {
        private const int rateLimitInMinutes = 30;
        private readonly ILogger<IndexModel> _logger;
        private readonly ChannelManager channelManager;
        private readonly IMemoryCache memoryCache;
        private readonly IWebHostEnvironment environment;

        public IndexModel(ILogger<IndexModel> logger, ChannelManager channelManager, IMemoryCache memoryCache, IWebHostEnvironment environment)
        {
            _logger = logger;
            this.channelManager = channelManager;
            this.memoryCache = memoryCache;
            this.environment = environment;
        }

        public void OnGet()
        {

        }


        [BindProperty]
        public CrawlJobPost CrawlJobPost { get; set; }
        public async Task<IActionResult> OnPostAsync()
        {
            var ipaddress = HttpContext.Connection.RemoteIpAddress.ToString();
            if (!this.environment.IsDevelopment())
            {

                if (memoryCache.TryGetValue(ipaddress, out var _))
                {
                    Configuration.Logger.Information("Blocking Request from {ipaddress}", ipaddress);
                    return Content($"Leider hast du schon zu viele Anfragen gestellt. Probiere es in {rateLimitInMinutes} Minuten noch einmal.");
                }
                else if (memoryCache.TryGetValue(CrawlJobPost.Url, out _))
                {
                    Configuration.Logger.Information("Blocking Request for {url} due to ratelimit from {ipaddress}", CrawlJobPost.Url, ipaddress);
                    return Content($"Dieses Produkt wurde erst kürzlich angefragt. Probiere es in {rateLimitInMinutes} Minuten noch einmal.");
                }
            }
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            Configuration.Logger.Information("Processing request from {ipaddress}", ipaddress);
            memoryCache.Set(ipaddress, string.Empty, TimeSpan.FromMinutes(rateLimitInMinutes));
            memoryCache.Set(CrawlJobPost.Url, string.Empty, TimeSpan.FromMinutes(rateLimitInMinutes));
            var job = new CrawlJob(CrawlJobPost);
            await channelManager.AddJob(job);
            return RedirectToPage("/Queue", new { job.Id });
        }
    }
}