using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpertPriceCrawler.Web.Pages
{
    public class QueueModel : PageModel
    {
        private readonly ChannelManager channelManager;

        public int JobCount { get; set; }
        public TimeSpan EstimatedWaitingTime { get; set; }

        public QueueModel(ChannelManager channelManager)
        {
            this.channelManager = channelManager;
        }

        public void OnGet()
        {
            JobCount = channelManager.JobCount;
            EstimatedWaitingTime = channelManager.LastJobTimeTaken * (JobCount+1);
        }
    }
}
