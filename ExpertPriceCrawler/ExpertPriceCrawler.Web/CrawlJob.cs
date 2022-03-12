using System.ComponentModel.DataAnnotations;

namespace ExpertPriceCrawler.Web
{
    public class CrawlJob
    {
        [Required]
        public string Url { get; set; } = null!;
        [Required]
        public string EmailAddress { get; set; } = null!;
        public DateTime TimeCreated { get; } = DateTime.UtcNow;
    }
}
