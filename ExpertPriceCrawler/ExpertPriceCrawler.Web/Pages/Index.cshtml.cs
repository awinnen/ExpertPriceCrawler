using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace ExpertPriceCrawler.Web.Pages
{
    public class IndexModel : PageModel
    {
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
        public CrawlJob CrawlJob { get; set; }
        public async Task<IActionResult> OnPostAsync()
        {
            var ipaddress = HttpContext.Connection.RemoteIpAddress;
            if (memoryCache.TryGetValue(ipaddress, out var _)){
                Configuration.Logger.Information("Blocking Request from {ipaddress}", ipaddress);
                return Content("Leider hast du schon zu viele Anfragen gestellt. Probiere es später noch einmal.");
            }
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            Configuration.Logger.Information("Processing request from {ipaddress}", ipaddress);
            memoryCache.Set(ipaddress, String.Empty, TimeSpan.FromMinutes(30));
            await channelManager.AddJob(CrawlJob);
            return RedirectToPage("/Queue");
        }
    }
}