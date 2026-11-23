using Cike.AspNetCore.MinimalAPIs.Extensions;
using Cike.Core.Extensions;
using Cike.OS.Service.Open;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.File("Logs/logs.log", outputTemplate: "{Timestamp:HH:mm} || {Level} || {SourceContext:l} || {Message} || {Exception} ||end {NewLine}", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Logging.AddSerilog();

await builder.Services.AddApplicationAsync<CikeOSServiceOpenModule>();
var app = builder.Build();

await app.InitializeApplicationAsync();
app.MapGet("/", () => "Hello Project!");

app.Run();
