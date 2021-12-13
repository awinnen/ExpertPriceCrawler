using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertPriceCrawler
{
    public static class Extensions
    {
        public static Uri MakeExpertUri(this Uri original)
        {
            return new Uri($"https://www.expert.de{original.AbsolutePath}");
        }
    }
}
