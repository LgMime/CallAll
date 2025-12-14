using CallAll;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;



class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient("8583175698:AAFfUeUe5uwZqqKME2YAA_jUmJxntrkOrk0"));
        services.AddTransient<RunBot>();

        var provider = services.BuildServiceProvider(); 
        var runBot = provider.GetRequiredService<RunBot>();
        await runBot.RunAsync();



    }
}