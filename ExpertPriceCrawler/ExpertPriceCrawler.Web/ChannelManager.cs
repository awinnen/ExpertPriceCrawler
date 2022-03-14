using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Threading.Channels;

namespace ExpertPriceCrawler.Web
{
    public class ChannelManager
    {
        private readonly Channel<CrawlJob> jobs = Channel.CreateUnbounded<CrawlJob>();
        private readonly ILogger<ChannelManager> logger;
        private readonly IOptions<SmtpServerConfig> smtpServerConfig;
        private readonly SortedList<DateTime, CrawlJob> CompletedJobs = new SortedList<DateTime, CrawlJob>(Comparer<DateTime>.Create((x, y) => y.CompareTo(x)));
        private const int MAX_COMPLETED_JOBS = 10;

        public TimeSpan LastJobTimeTaken { get; private set; } = TimeSpan.FromMinutes(15);

        public int JobCount => jobs.Reader.Count;

        public List<CrawlJob> RecentlyCompletedJobs => CompletedJobs.Select(x => x.Value).ToList();

        public ChannelManager(ILogger<ChannelManager> logger, IOptions<SmtpServerConfig> smtpServerConfig)
        {
            this.logger = logger;
            this.smtpServerConfig = smtpServerConfig;
            StartWorker();
        }

        public async Task AddJob(CrawlJob job)
        {
            await jobs.Writer.WriteAsync(job);
            Configuration.Logger.Information("Job Queued for {url}", job.Url);
        }

        private void StartWorker()
        {
            Task.Run(async () => {
                while(true)
                {
                    var job = await jobs.Reader.ReadAsync();
                    try
                    {
                        await StartJob(job);
                    } catch(Exception ex)
                    {
                        Configuration.Logger.Error(ex, "Error executing job for {url}", job.Url);
                    }
                }
            });
        }

        private async Task StartJob(CrawlJob job)
        {
            var stopWatch = Stopwatch.StartNew();
            var uri = new Uri(job.Url);
            var resultTask = Configuration.Instance.CrawlerType == nameof(BrowserCrawler) ? BrowserCrawler.CollectPrices(uri) : ApiCrawler.CollectPrices(uri);
            Configuration.Logger.Information("Starting Job for {url}", job.Url);
            var result = await resultTask;
            stopWatch.Stop();
            LastJobTimeTaken = stopWatch.Elapsed;
            var emailBody = GetEmailBody(job, result);
            SetResult(job, GetResultTable(result));
            await SendResult(job, emailBody);
        }

        private void SetResult(CrawlJob job, string resultTableHtml)
        {
            job.TimeCompleted = DateTime.UtcNow;
            job.ResultTableHtml = resultTableHtml;
            if(CompletedJobs.Count > MAX_COMPLETED_JOBS)
            {
                CompletedJobs.Remove(CompletedJobs.Last().Key);
            }

            CompletedJobs.Add(job.TimeCompleted, job);
        }

        private async Task SendResult(CrawlJob job, string body)
        {
            using var smtpClient = new SmtpClient(smtpServerConfig.Value.Hostname, smtpServerConfig.Value.Port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpServerConfig.Value.Username, smtpServerConfig.Value.Password),
                EnableSsl = smtpServerConfig.Value.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };
            var message = new MailMessage()
            {
                Subject = "ExpertPriceCrawler: Ergebnis deiner Anfrage",
                Body = body,
                IsBodyHtml = true,
            };

            message.To.Add(job.EmailAddress);
            message.From = new MailAddress(smtpServerConfig.Value.From);

            Configuration.Logger.Debug("Attempting to send Email to {mailAddress}", job.EmailAddress);
            await smtpClient.SendMailAsync(message);
            Configuration.Logger.Information("Sent Email to {mailAddress}", job.EmailAddress);
        }


        private string GetEmailBody(CrawlJob job, List<Result> result)
        {
            return @$"
<h1>Ergebnis deiner Anfrage</h1>
<h2>für {job.Url}</h2>
<h3>Agefordert: {job.TimeCreated.ToString(Configuration.Instance.DateFormat)}, Fertiggestellt: {DateTime.UtcNow.ToString(Configuration.Instance.DateFormat)} (UTC)</h3>
{GetResultTable(result)}
";
        }

        private string GetResultTable(List<Result> result)
        {
            if(result.All(r => r.Price.Equals("error", StringComparison.OrdinalIgnoreCase)))
            {
                return "Leider sind zuviele Fehler aufgetreten. Dies kann daran liegen, dass Expert uns gesperrt hat. Bitte versuche es später noch einmal.";
            }
            return @"
            <table class=""table"">
                <thead>
                    <tr>
                        <th> Filial-ID </th>
                        <th> Filiale </th>
                        <th> Preis </th>
                        <th> Link </th>
                    </tr>
                </thead>
                <tbody>
            " +
                    string.Join('\n', result.Select(WriteLine))
                +
                @"</tbody>
            </table>";
        }

        private string WriteLine(Result entry)
        {
            return $@"
                <tr>
                        <td> {entry.BranchId} </td>
                        <td> {entry.BranchName} </td>
                        <td> {entry.Price} </td>
                        <td>
                            <a href=""{entry.Url}"" target=""_blank"" rel=""noreferrer""> Zum Shop </a>
                        </td>
                    </tr>
";
        }
    }
}
