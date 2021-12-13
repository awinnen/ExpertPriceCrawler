using Microsoft.AspNetCore.Mvc;

namespace ExpertPriceCrawler.Web.Controllers
{
    public class StatusController: Controller
    {

        [Route("/api/status")]
        public async Task<IActionResult> GetStatus(Uri uri)
        {
            uri = new Uri($"https://www.expert.de{uri.AbsolutePath}");
            if (Crawler.StatusDictionary.TryGetValue(uri.ToString(), out var statusMsg))
            {
                return Ok(statusMsg);
            }
            return NotFound($"No Status for {uri}");
        }
    }
}
