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
        public List<string> EmailAddress { get; set; } = new List<string>();
        public DateTime TimeCreated { get; set; } = DateTime.UtcNow;
        public DateTime TimeCompleted { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ResultTableHtml { get; set; }
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }

        public CrawlJob(CrawlJobPost input)
        {
            CrawlUrl = new Uri(input.Url).NormalizeUri();
            if(!string.IsNullOrWhiteSpace(input.EmailAddress)) {
                EmailAddress.Add(input.EmailAddress);
            }
        }

        public CrawlJob() { }
    }
}
