using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text; // 🔥 Добавили для работы с Encoding.UTF8
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public class RunDnbBot
{
    private readonly TelegramBotClient _botClient;
    private readonly HttpClient _httpClient;
    private const string BankApiUrl = "https://api-open.ccp.dnb.no/v1/kronekort/balance";
    private const string StorageFilePath = "users_storage.json";
    private Dictionary<long, string> _userCardNumbers;

    public RunDnbBot(string token)
    {
        _botClient = new TelegramBotClient(token);
        _httpClient = new HttpClient();
        _userCardNumbers = LoadUserData();
    }

    public async Task RunAsync()
    {
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<Telegram.Bot.Types.Enums.UpdateType>()
        };

        try
        {
            await _botClient.DeleteWebhook(dropPendingUpdates: true, cancellationToken: cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DNB Bot] Предупреждение вебхука: {ex.Message}");
        }

        Console.WriteLine("[DNB Bot] Успешно запущен и слушает сервер...");

        await _botClient.ReceiveAsync(
            updateHandler: ProcessUpdate,
            errorHandler: ProcessError,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );
    }

    private async Task ProcessUpdate(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Type != Telegram.Bot.Types.Enums.UpdateType.Message || update.Message?.Text == null)
                return;

            var message = update.Message;
            var userId = message.From!.Id;
            var chatId = message.Chat.Id;
            var text = message.Text.Trim();

            if (!_userCardNumbers.ContainsKey(userId))
            {
                if (text.Length == 12 && text.All(char.IsDigit))
                {
                    var truncatedCardNumber = text.Substring(0, 11);
                    _userCardNumbers[userId] = truncatedCardNumber;
                    SaveUserData(_userCardNumbers);

                    var replyMarkup = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new KeyboardButton("Balance") } })
                    {
                        ResizeKeyboard = true
                    };

                    // 🔥 Изменено на SendMessageAsync
                    await client.SendMessage(chatId, $"✅ Card number saved!\nStored: {truncatedCardNumber}", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
                }
                else
                {
                    // 🔥 Изменено на SendMessageAsync
                    await client.SendMessage(chatId, "❌ Please send exactly 12 digits.", cancellationToken: cancellationToken);
                }
            }
            else
            {
                if (text == "Balance")
                {
                    await SendBalanceRequest(client, chatId, _userCardNumbers[userId], cancellationToken);
                }
                else if (text.Length == 12 && text.All(char.IsDigit))
                {
                    var truncatedCardNumber = text.Substring(0, 11);
                    _userCardNumbers[userId] = truncatedCardNumber;
                    SaveUserData(_userCardNumbers);

                    // 🔥 Изменено на SendMessageAsync
                    await client.SendMessage(chatId, $"✅ Card number updated!\nStored: {truncatedCardNumber}", cancellationToken: cancellationToken);
                }
                else
                {
                    // 🔥 Изменено на SendMessageAsync
                    await client.SendMessage(chatId, "ℹ️ Press Balance or send a new 12-digit number.", cancellationToken: cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DNB Bot] Error processing update: {ex.Message}");
        }
    }

    private async Task SendBalanceRequest(ITelegramBotClient client, long chatId, string cardNumber, CancellationToken cancellationToken)
    {
        try
        {
            // 🔥 Изменено на SendMessageAsync
            await client.SendMessage(chatId, "⏳ Checking balance...", cancellationToken: cancellationToken);

            var request = new HttpRequestMessage(HttpMethod.Post, BankApiUrl);
            request.Headers.Add("Origin", "https://www.dnb.no");
            request.Headers.Add("Referer", "https://www.dnb.no/");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36 OPR/131.0.0.0");
            request.Headers.Add("X-DNBAPI-Trace-Id", Guid.NewGuid().ToString());
            request.Headers.Add("X-DNBAPI-Channel", "BMPULS");

            var payload = new { accountNumber = cardNumber };
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;
                string balanceMessage = "💳 Balance Information:\n\n";

                if (root.TryGetProperty("balance", out var balanceElement) && balanceElement.ValueKind == JsonValueKind.Number)
                {
                    balanceMessage += $"Balance: {balanceElement.GetDecimal():N2} NOK";
                }
                else
                {
                    balanceMessage += $"Balance raw data: {responseContent}";
                }

                // 🔥 Изменено на SendMessageAsync
                await client.SendMessage(chatId, balanceMessage, cancellationToken: cancellationToken);
            }
            else
            {
                // 🔥 Изменено на SendMessageAsync
                await client.SendMessage(chatId, $"❌ Error. Status: {response.StatusCode}\nDetails: {responseContent}", cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // 🔥 Изменено на SendMessageAsync
            await client.SendMessage(chatId, $"❌ An error occurred: {ex.Message}", cancellationToken: cancellationToken);
        }
    }

    private Task ProcessError(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DNB Bot] Polling error: {exception.Message}");
        return Task.CompletedTask;
    }

    private Dictionary<long, string> LoadUserData()
    {
        if (!System.IO.File.Exists(StorageFilePath)) return new Dictionary<long, string>();
        try
        {
            string json = System.IO.File.ReadAllText(StorageFilePath);
            return JsonSerializer.Deserialize<Dictionary<long, string>>(json) ?? new Dictionary<long, string>();
        }
        catch { return new Dictionary<long, string>(); }
    }

    private void SaveUserData(Dictionary<long, string> data)
    {
        try
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(StorageFilePath, json);
        }
        catch (Exception ex) { Console.WriteLine($"Ошибка сохранения БД: {ex.Message}"); }
    }
}