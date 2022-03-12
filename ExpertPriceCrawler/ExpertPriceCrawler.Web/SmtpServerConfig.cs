namespace ExpertPriceCrawler.Web
{
    public class SmtpServerConfig
    {
        public string Hostname {get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get; set; }
        public string From { get; set; }
    }
}
