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
        public bool JobCompleted { get; private set; }
        public bool JobIdPresent { get; private set; }
        public int QueuePosition { get; private set; }

        public QueueModel(ChannelManager channelManager)
        {
            this.channelManager = channelManager;
        }

        public void OnGet()
        {
            JobCount = channelManager.JobCount;
            EstimatedWaitingTime = channelManager.LastJobTimeTaken * (JobCount+1);
            RecentlyCompletedJobs = channelManager.RecentlyCompletedJobs;
            if(HttpContext.Request.Query.TryGetValue("Id", out var jobIdFromQuery))
            {
                JobIdPresent = true;
                JobCompleted = RecentlyCompletedJobs.Any(x => x.Id.ToString().Equals(jobIdFromQuery.ToString(), StringComparison.OrdinalIgnoreCase));
                var jobInQueue = channelManager.JobsInQueue.FirstOrDefault(j => j.Id.ToString().Equals(jobIdFromQuery, StringComparison.OrdinalIgnoreCase));
                QueuePosition = jobInQueue is null ? JobCount : Array.IndexOf(channelManager.JobsInQueue, jobInQueue) + 1;
                EstimatedWaitingTime = channelManager.LastJobTimeTaken * QueuePosition;
            }
        }
    }
}
