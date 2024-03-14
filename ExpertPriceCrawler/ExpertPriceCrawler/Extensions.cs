using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertPriceCrawler
{
    public static class Extensions
    {
        public static Uri MakeExpertCrawlUri(this Uri original)
        {
            return new Uri($"{Configuration.Instance.ExpertBaseUrl}{original.AbsolutePath}");
        }
        public static Uri MakeExpertPriceAPICrawlUri(this Uri original)
        {
            return new Uri($"{Configuration.Instance.ExpertBaseUrl}{original.AbsolutePath}");
        }

        public static Uri NormalizeUri(this Uri original)
        {
            return new Uri($"https://www.expert.de{original.AbsolutePath}");
        }

        public static string ToGermanDateTimezoneString(this DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTime(dateTime, Configuration.Instance.GermanTimeZoneInfo).ToString(Configuration.Instance.GermanDateFormatInfo);
        }
    }
}
