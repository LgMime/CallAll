using CallAll;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;



class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        string botToken = File.ReadAllText("C:\\Software\\CallAllApi.txt").Trim();
        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
        services.AddTransient<RunBot>();

        var provider = services.BuildServiceProvider(); 
        var runBot = provider.GetRequiredService<RunBot>();
        await runBot.RunAsync();



    }
}