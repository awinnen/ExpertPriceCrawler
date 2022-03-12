using Microsoft.Extensions.Options;
using System.Diagnostics;
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

        public TimeSpan LastJobTimeTaken { get; private set; } = TimeSpan.FromMinutes(15);

        public int JobCount => jobs.Reader.Count;

        public ChannelManager(ILogger<ChannelManager> logger, IOptions<SmtpServerConfig> smtpServerConfig)
        {
            this.logger = logger;
            this.smtpServerConfig = smtpServerConfig;
            StartWorker();
        }

        public async Task AddJob(CrawlJob job)
        {
            await jobs.Writer.WriteAsync(job);
            logger.LogInformation("Job Queued for {url}", job.Url);
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
                        logger.LogError("Error executing job for {url}", job.Url);
                    }
                }
            });
        }

        private async Task StartJob(CrawlJob job)
        {
            var stopWatch = Stopwatch.StartNew();
            var uri = new Uri(job.Url);
            var resultTask = Configuration.Instance.CrawlerType == nameof(BrowserCrawler) ? BrowserCrawler.CollectPrices(uri) : ApiCrawler.CollectPrices(uri);
            var result = await resultTask;
            logger.LogInformation("Starting Job for {url}", job.Url);
            stopWatch.Stop();
            LastJobTimeTaken = stopWatch.Elapsed;
            await SendResult(job, result);
        }

        private async Task SendResult(CrawlJob job, List<Result> result)
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
                Body = @$"
<h1>Ergebnis deiner Anfrage</h1>
<h2>für {job.Url}</h2>
<h3>Agefordert um {job.TimeCreated}</h3>
{GetResultTable(result)}
",
                IsBodyHtml = true,
            };

            message.To.Add(job.EmailAddress);
            message.From = new MailAddress(smtpServerConfig.Value.From);

            logger.LogDebug("Attempting to send Email to {mailAddress}", job.EmailAddress);
            await smtpClient.SendMailAsync(message);
            logger.LogInformation("Sent Email to {mailAddress}", job.EmailAddress);
        }

        private string GetResultTable(List<Result> result)
        {
        return @"
        <table>
            <thead>
                <tr>
                    <th> Filial - ID </th>
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
