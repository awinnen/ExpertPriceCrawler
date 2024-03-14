using ConsoleTables;
using ExpertPriceCrawler;
using System.Diagnostics;
using System.Text;

public static class Program
{

    static async Task Main()
    {
        Configuration.Init("Production");
        Console.OutputEncoding = Encoding.UTF8;

        var uri = ReadUrl();
        IProductCrawler crawler = Configuration.Instance.CrawlerType == nameof(BrowserCrawler) ? new BrowserCrawler() : new ApiCrawler();
        var branchPrices = await crawler.CollectPrices(uri);

        int done = 0;
        int printRowCount = 25;
        
        do
        {
            ConsoleTable.From<ResultBase>(
                branchPrices.OrderBy(x => x.PriceDecimal)
                    .Skip(done)
                    .Take(printRowCount)
                ).Write(Format.Minimal);
            done += printRowCount;
            if (done < branchPrices.Count)
            {
                Console.WriteLine("Press any Key to print next Results!");
            }
        } while (done < branchPrices.Count && Console.ReadKey(true) != null);

        Console.WriteLine("Press any Key to Close this Window");
        Console.ReadKey(true);
        Environment.Exit(0);
    }

    static Uri ReadUrl()
    {
        try
        {
            Console.WriteLine("Bitte Produkt-URL von Expert eingeben:");
            var input = Console.ReadLine();
            return new Uri(input);
        }
        catch (UriFormatException)
        {
            Console.WriteLine("Url ungültig");
            return ReadUrl();
        }
    }
}
