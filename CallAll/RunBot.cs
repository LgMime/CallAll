using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.IO;

namespace CallAll
{
    public class RunBot
    {
        private readonly ITelegramBotClient botClient;

        public RunBot(ITelegramBotClient bot)
        {
            botClient = bot;
        }

        public async Task RunAsync()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new UpdateType[] { UpdateType.Message }
            };

            botClient.StartReceiving(
                updateHandlerasync,
                HandleErrorAsync,
                receiverOptions
            );

            await Task.Delay(-1);
        }

        private static async Task updateHandlerasync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // 1. ЗАЩИТА ОТ ОШИБОК: Проверяем, что это сообщение и в нем есть текст
            if (update.Message is not { } message) return;
            if (message.Text is not { } messageText) return;

            var subService = new SubscriptionService();
            long chatId = message.Chat.Id;

            // Логируем в консоль, чтобы видеть, что бот жив
            Console.WriteLine($"Received: '{messageText}' from {message.From?.FirstName}");

            // 2. ОБРАБОТКА КОМАНДЫ /start
            if (messageText == "/start")
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Привет! Нажми /sub, чтобы подписаться, или кнопку 'Call All' для вызова всех.",
                    // Вот здесь мы подключаем вашу клавиатуру
                    replyMarkup: Keyboard.GetMainKeyboard()
                );
                return;
            }

            if (messageText == "/sub")
            {
                var user = message.From;
                // Проверка на null для user
                if (user == null) return;

                bool isNew = subService.AddSubscriber(user.Id, user.FirstName, user.Username);

                if (isNew)
                    await botClient.SendMessage(chatId, "✅ Вы добавлены в список для тегов!");
                else
                    await botClient.SendMessage(chatId, "Вы уже в списке!");
            }
            else if (messageText == "Call All")
            {
                var allSubs = subService.GetAllSubscribers();

                if (!allSubs.Any())
                {
                    await botClient.SendMessage(chatId, "Сначала кто-нибудь должен нажать /sub!");
                    return;
                }

                StringBuilder messageBuilder = new StringBuilder();
                messageBuilder.AppendLine();

                foreach (var sub in allSubs)
                {
                    if (!string.IsNullOrEmpty(sub.Username))
                    {
                        messageBuilder.Append($"@{sub.Username} ");
                    }
                    else
                    {
                        messageBuilder.Append($"<a href=\"tg://user?id={sub.Id}\">{sub.Name}</a> ");
                    }
                }
                string path = @"C:\Software\Ebem.jpg";
                using (var stream = File.OpenRead(path))
                {
                    await botClient.SendPhoto(
                        chatId: chatId,
                        // 3. Используем FromStream вместо FromUri
                        photo: InputFile.FromStream(stream, "my_photo.jpg"),
                        caption: messageBuilder.ToString()
                    );
                }
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Вывод ошибок в консоль обязателен, иначе вы не поймете, почему бот упал
            Console.WriteLine(exception.ToString());
            return Task.CompletedTask;
        }
    }
}