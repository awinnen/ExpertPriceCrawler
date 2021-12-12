using Microsoft.AspNetCore.Mvc;

namespace ExpertPriceCrawler.Web.Controllers
{
    public class StatusController: Controller
    {

        [Route("/api/status")]
        public async Task<IActionResult> GetStatus(string uri)
        {
            if(Crawler.StatusDictionary.TryGetValue(uri, out var statusMsg))
            {
                return Ok(statusMsg);
            }
            return NotFound($"No Status for {uri}");
        }
    }
}
