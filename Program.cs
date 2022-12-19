using Microsoft.Extensions.Logging;
using Serilog;

Console.Title = "Cooking Bot";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("log.txt")
    .CreateLogger();

var loggerFactory = new LoggerFactory().AddSerilog();

var bot = new CookingBot.Bot();
await bot.MainAsync(loggerFactory, CookingBot.AppCredentials.Token);

Log.CloseAndFlush();