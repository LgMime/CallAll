using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace CallAll
{
    public static class Keyboard
    {
        public static ReplyKeyboardMarkup GetMainKeyboard()
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                // Одна строка с одной кнопкой
                new KeyboardButton[] { "Call All" }
            })
            {
                // Обязательная настройка для красивого вида
                ResizeKeyboard = true
            };

            return keyboard;
        }

    }
}
