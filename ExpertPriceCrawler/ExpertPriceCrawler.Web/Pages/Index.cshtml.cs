using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpertPriceCrawler.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ChannelManager channelManager;

        public IndexModel(ILogger<IndexModel> logger, ChannelManager channelManager)
        {
            _logger = logger;
            this.channelManager = channelManager;
        }

        public void OnGet()
        {

        }


        [BindProperty]
        public CrawlJob CrawlJob { get; set; }
        public async Task<IActionResult> OnPostAsync()
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await channelManager.AddJob(CrawlJob);
            return RedirectToPage("/Queue");
        }
    }
}