using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertPriceCrawler
{
    public interface IProductCrawler
    {
        public Task<List<Result>> CollectPrices(Uri uri);
    }
}
