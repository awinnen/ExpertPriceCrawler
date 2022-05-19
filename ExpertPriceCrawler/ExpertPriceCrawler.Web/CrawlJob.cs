using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ExpertPriceCrawler.Web
{
    public class CrawlJobPost
    {
        [Required]
        public string Url { get; set; } = null!;

        [EmailAddress]
        public string? EmailAddress { get; set; } = null!;
    }

    public class CrawlJob
    {
        public bool Success { get; set; }
        public Uri CrawlUrl { get; set; }
        public string EmailAddress { get; set; }
        public DateTime TimeCreated { get; } = DateTime.UtcNow;
        public DateTime TimeCompleted { get; set; }
        public Guid Id { get; } = Guid.NewGuid();
        public string ResultTableHtml { get; set; }
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }

        public CrawlJob(CrawlJobPost input)
        {
            CrawlUrl = new Uri(input.Url).NormalizeUri();
            EmailAddress = input.EmailAddress;
        }

        public CrawlJob() { }
    }
}
