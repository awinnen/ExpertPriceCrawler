using ExpertPriceCrawler.Web;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddSingleton<ChannelManager>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedForHeaderName = "X-Forwarded-For";
    options.ForwardLimit = null;
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.AllowedHosts.Clear();
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var configurationRoot = ExpertPriceCrawler.Configuration.Init(builder.Environment.EnvironmentName);
builder.Services.Configure<SmtpServerConfig>(c => configurationRoot.GetSection("SmtpServer").Bind(c));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseForwardedHeaders();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
