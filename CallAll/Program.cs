using CallAll;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // 1. Твой первый бот (CallAll) — остается без изменений
        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient("8583175698:AAFfUeUe5uwZqqKME2YAA_jUmJxntrkOrk0"));
        services.AddTransient<RunBot>();

        // 2. Наш новый бот (DNB Kronekort)
        // Передаем токен напрямую в конструктор RunDnbBot, чтобы не ломать DI для CallAll
        services.AddTransient<RunDnbBot>(provider => new RunDnbBot("8234600723:AAEOuQfGSTmU_Gw1Wgz33PwVWDGdB3MGyig"));

        var provider = services.BuildServiceProvider();

        // Разворачиваем оба сервиса
        var runBot = provider.GetRequiredService<RunBot>();
        var runDnbBot = provider.GetRequiredService<RunDnbBot>();

        Console.WriteLine("Запуск обоих ботов в одном процессе...");

        // 🔥 Запускаем обоих ботов параллельно в фоне
        await Task.WhenAll(
            runBot.RunAsync(),
            runDnbBot.RunAsync()
        );
    }
}