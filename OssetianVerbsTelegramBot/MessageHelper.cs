using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace OssetianVerbsTelegramBot
{
    public class MessageHelper
    {
        static TelegramBotClient _bot;
        public static long[] admins = { 946534275, 2033844706, 6242358847, 286858097 };
        public static long[] moderators = { 946534275, 2033844706, 6242358847 };
        public static HashSet<long> needFeedback = new();
        public static Dictionary<long, int[]> helpMessages = new();
        public static void Initialize(TelegramBotClient bot)
        {
            _bot = bot;
        }
        public static async Task SendAdminMenu(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]{
                new[] { new KeyboardButton("📝 Backup Базы данных") },
                new[] { new KeyboardButton("🔙 В главное меню") }
            })
            {
                ResizeKeyboard = true
            };
            await _bot.SendMessage(chatId: chatId,
               text: "Добро пожаловать🛠️", replyMarkup: keyboard, parseMode: ParseMode.Html);
        }

        public static async Task SendVerbMenu(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[]
                {
                    new KeyboardButton("📋 Тип глагола"),
                    new KeyboardButton("🖋️ Перевести"),
                    new KeyboardButton("🛠️ Спряжение")
                },
                new[]
                {
                    new KeyboardButton("⚙️ Статистика"),
                    new KeyboardButton("💡 Справка")
                },
                new[]
                {
                    new KeyboardButton("🔙 В главное меню")
                }
            })
            {
                ResizeKeyboard = true
            };

            await _bot.SendMessage(chatId: chatId,
                text: "<b>Выберите задание в меню:</b>", replyMarkup: keyboard, parseMode: ParseMode.Html);
        }

        public static async Task SendMainMenu(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]{
                new[] { new KeyboardButton("📝 Глаголы") },
                new[] { new KeyboardButton("🤖 Чат-бот (Beta)") },
                new[] { new KeyboardButton("🆘 Обратная связь") },
            })
            {
                ResizeKeyboard = true
            };
            if (admins.Contains(chatId))
            {
                keyboard = new ReplyKeyboardMarkup(new[]{
                new[] { new KeyboardButton("📝 Глаголы") },
                new[] { new KeyboardButton("🤖 Чат-бот (Beta)") },
                new[] { new KeyboardButton("🆘 Обратная связь") },
                new[] {new KeyboardButton("👨‍💻 Панель администратора")},
            })
                {
                    ResizeKeyboard = true
                };
            }


            await _bot.SendMessage(chatId: chatId,
                text: "<b> Навигация осуществляется с помощью меню</b> 👇", replyMarkup: keyboard, parseMode: ParseMode.Html);
        }

        public static  async Task SendStatistics(long id)
        {
            var list = await DbUser.GetUserStatById(id.ToString());
            string textStatistics = "Статистика правильных ответов: \n";
            foreach (var stat in list)
            {
                textStatistics += stat.ToString() + "\n";
            }
            await _bot.SendMessage(id, textStatistics);
        }

        public static async Task SendKeyboardLink(Message message)
        {
            string keyboardInformationString = """
                Чтобы пользоваться всеми функциями бота, вам понадобится «Яндекс Клавиатура»
                """;

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(
                new InlineKeyboardButton[] {
                    new InlineKeyboardButton("Андроид", "https://play.google.com/store/apps/details?id=ru.yandex.androidkeyboard&hl=ru"),
                    new InlineKeyboardButton("IOS", "https://apps.apple.com/ru/app/яндекс-клавиатура/id1053139327")
                });

            await _bot.SendMessage(message.Chat.Id, keyboardInformationString, replyMarkup: markup);
        }

        public static async Task<int[]> SendHelp(long id)
        {
            var firstTypeVerbs = DbVerbImport.GetAllFirstTypeVerbs();
            var secondTypeVerbs = DbVerbImport.GetAllSecondTypeVerbs();
            var imageFile = File.Open(Path.Combine("Images", "declinationRule.jpg"), FileMode.Open);

            var photoMessage = await _bot.SendPhoto(id, imageFile, caption: "Правило спряжения глаголов в прошедшем времени.");

            var textVerbs = "<b>Переходные глаголы:</b>\n<i>Инфинитив - Морфема в прошедшем времени - Перевод</i>\n";
            foreach (var verb in firstTypeVerbs)
                textVerbs += $"{verb.Inf} - {verb.Past} - {verb.Trans}\n";

            var firstTypeMessage = await _bot.SendMessage(id, textVerbs, parseMode: ParseMode.Html);

            textVerbs = "<b>Непереходные глаголы:</b>\n<i>Инфинитив - Морфема в прошедшем времени - Перевод</i>\n";
            foreach (var verb in secondTypeVerbs)
                textVerbs += $"{verb.Inf} - {verb.Past} - {verb.Trans}\n";

            var secondTypeMessage = await _bot.SendMessage(id, textVerbs, parseMode: ParseMode.Html);

            return new[] { photoMessage.MessageId, firstTypeMessage.MessageId, secondTypeMessage.MessageId, photoMessage.MessageId - 1 };
        }

        public static async Task SendReportToAllModerators(long reporterId,Message message)
        {
            foreach (var moder in moderators)
            {
                await _bot.SendMessage(moder, $"🆘 Вам поступило новое обращение\n" +
                    $"Отправитель: {reporterId}  @{message?.From?.Username ??
                    message?.From?.FirstName ?? "скрыл юзернейм"} :\n" + message.Text);
            }
            await _bot.SendMessage(reporterId, "Ваше обращение было успешно доставлено, ожидайте ответа!");

            await MessageHelper.SendMainMenu(reporterId);
        }

        public static async Task SendReportHelp(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new KeyboardButton("🔙 Отмена"))
            {
                ResizeKeyboard = true
            };
            await _bot.SendMessage(chatId, "Если есть вопросы или заметили ошибки в работе бота, напишите сюда и ваше сообщение будет передано модераторам:", replyMarkup: keyboard);
        }

    }
}
