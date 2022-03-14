using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpertPriceCrawler.Web.Pages
{
    public class QueueModel : PageModel
    {
        private readonly ChannelManager channelManager;

        public int JobCount { get; private set; }
        public TimeSpan EstimatedWaitingTime { get; private set; }
        public List<CrawlJob> RecentlyCompletedJobs { get; private set; }

        public QueueModel(ChannelManager channelManager)
        {
            this.channelManager = channelManager;
        }

        public void OnGet()
        {
            JobCount = channelManager.JobCount;
            EstimatedWaitingTime = channelManager.LastJobTimeTaken * (JobCount+1);
            RecentlyCompletedJobs = channelManager.RecentlyCompletedJobs;
        }
    }
}
