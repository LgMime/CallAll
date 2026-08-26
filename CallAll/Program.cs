using CallAll;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // 1. Твой первый бот (CallAll) — остается без изменений
        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient("8583175698:AAHgUVaRGkg_WN-qZ9YqTSTmAWvSTKzKSEY"));
        services.AddTransient<RunBot>();

        var provider = services.BuildServiceProvider();

        // Разворачиваем оба сервиса
        var runBot = provider.GetRequiredService<RunBot>();


        Console.WriteLine("Запуск обоих ботов в одном процессе...");

        // 🔥 Запускаем обоих ботов параллельно в фоне
        await Task.WhenAll(
            runBot.RunAsync()
        );
    }
}