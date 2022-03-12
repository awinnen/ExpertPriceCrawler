using ExpertPriceCrawler.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddSingleton<ChannelManager>();

var configurationRoot = ExpertPriceCrawler.Configuration.Init(builder.Environment.EnvironmentName);
builder.Services.Configure<SmtpServerConfig>(c => configurationRoot.GetSection("SmtpServer").Bind(c));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
