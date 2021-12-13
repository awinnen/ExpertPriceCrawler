using Microsoft.AspNetCore.Mvc;

namespace ExpertPriceCrawler.Web.Controllers
{
    public class StatusController: Controller
    {

        [Route("/api/status")]
        public IActionResult GetStatus(Uri uri)
        {
            if (Crawler.StatusDictionary.TryGetValue(uri.MakeExpertUri().ToString(), out var statusMsg))
            {
                return Ok(statusMsg);
            }
            return NotFound($"No Status for {uri}");
        }
    }
}
