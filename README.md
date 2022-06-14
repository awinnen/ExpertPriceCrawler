# ExpertPriceCrawler

## Console App
### Requirements
- Before you download the Console App, make sure you have [DotNet 6](https://dotnet.microsoft.com/en-us/download) installed.

### Download and usage
- Go to [release section](https://github.com/awinnen/ExpertPriceCrawler/releases) of this repository and download latest version as zip (e.g. ExpertPriceCrawler-0.5.zip)
- Extract the zip archive to any directory on your computer
- Open the directory to which you extracted the archive with explorer
- Find _ExpertPriceCrawler.Shell.exe_ and double-click it
  - When Windows warns you that the file _might_ hurt your computer, you must proceed the execution. If you don't trust the Application, you can't use it. Sorry :)
  - A Console Window will open. It asks you for expert product url
- On first usage, the App will download a chromium browser. Therefore the first crawling might take a feq minutes. Just be patient and wait until you see many messages in the Console window.

## WebApp
If you want to run the WebApp on your computer or server(preferred), there are three ways:
- Simply run the dotnet app using cli
- Run the webapp hosted inside IIS on Windows machines
- Let the WebApp run inside a Container using [Docker](https://docs.docker.com/get-docker/)

### Docker
1. Create a file named `expertpricecrawler.env` at any desired folder
   The content of that file might look like this
   ```
   ASPNETCORE_ENVIRONMENT=Production
   PUPPETEER_EXECUTABLE_PATH=/usr/bin/google-chrome-stable
   Serilog__MinimumLevel=Verbose
   SmtpServer__From=noreply@domain.tld
   SmtpServer__Hostname=mailserver.domain.tld
   SmtpServer__Password=anyawesomepassword
   SmtpServer__Port=587
   SmtpServer__Username=anyusernameavailableonyourmailserver
   SmtpServer__UseSsl=true
   Logging__Console__FormatterName=json
   Options__CrawlerType=BrowserCrawler
   Options__MaxParallelRequests=5
   Options__MemoryCacheMinutes=0
   ```
3. In that folder, open a command line (Powershell, Bash, ...)
4. RUN `docker run awinnen/expertpricecrawler:latest -p 80:80 --env-file expertpricecrawler.env`
5. Open any Browser and navigate to http://localhost:80 (http is important! https will not work. If you can't reach that site, make sure your browser is not redirecting to https due to htst!)
