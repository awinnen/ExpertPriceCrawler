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

        public IndexModel(ILogger<IndexModel> logger, ChannelManager channelManager, IMemoryCache memoryCache)
        {
            _logger = logger;
            this.channelManager = channelManager;
            this.memoryCache = memoryCache;
        }

        public void OnGet()
        {

        }


        [BindProperty]
        public CrawlJobPost CrawlJobPost { get; set; }
        public async Task<IActionResult> OnPostAsync()
        {
            var ipaddress = HttpContext.Connection.RemoteIpAddress;
            if (memoryCache.TryGetValue(ipaddress, out var _)){
                Configuration.Logger.Information("Blocking Request from {ipaddress}", ipaddress);
                return Content($"Leider hast du schon zu viele Anfragen gestellt. Probiere es in {rateLimitInMinutes} Minuten noch einmal.");
            }
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            Configuration.Logger.Information("Processing request from {ipaddress}", ipaddress);
            memoryCache.Set(ipaddress, string.Empty, TimeSpan.FromMinutes(rateLimitInMinutes));
            var job = new CrawlJob(CrawlJobPost);
            await channelManager.AddJob(job);
            return RedirectToPage("/Queue", new { job.Id });
        }
    }
}