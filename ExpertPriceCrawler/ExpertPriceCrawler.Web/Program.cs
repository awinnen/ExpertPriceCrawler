var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    ExpertPriceCrawler.Constants.LaunchOptions = new PuppeteerSharp.LaunchOptions()
    {
        Headless = true,
        ExecutablePath = "/usr/bin/google-chrome-stable",
        Args = new[]
        {
            "--no-sandbox"
        }
    };
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
