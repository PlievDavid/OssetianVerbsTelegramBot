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
        TelegramBotClient _bot;
        public long[] admins = { 946534275, 2033844706, 6242358847, 286858097 };
                                          //Геор        Давид       Алан     МД
        public long[] moderators = { 946534275 , 2033844706, 6242358847 };
        public HashSet<long> needFeedback = new();
        public Dictionary<long, List<int>> helpMessages = new();
        public MessageHelper(TelegramBotClient bot)
        {
            _bot = bot;
        }

        public async Task SendAdminMenu(long chatId)
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

        public async Task SendVerbMenu(long chatId)
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

        public async Task SendMainMenu(long chatId)
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

        public  async Task SendStatistics(long id)
        {
            var list = await DbUser.GetUserStatById(id.ToString());
            string textStatistics = "Статистика правильных ответов: \n";
            foreach (var stat in list)
            {
                textStatistics += stat.ToString() + "\n";
            }
            await _bot.SendMessage(id, textStatistics);
        }

        public async Task SendKeyboardLink(Message message)
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

        public async Task SendHelp(long id, int msgId)
        {
            var messageIds = new List<int>{ msgId };
            try
            {
                var imagePath = Path.Combine("Images", "declinationRule.jpg");

                using (var imageFile = File.Open(imagePath, FileMode.Open, FileAccess.Read))
                {
                    var photoMessage = await _bot.SendPhoto( id,imageFile, caption: "Правило спряжения глаголов в прошедшем времени.");
                    messageIds.Add(photoMessage.MessageId);
                }
                var firstTypeVerbs = DbVerbImport.GetAllFirstTypeVerbs();
                var secondTypeVerbs = DbVerbImport.GetAllSecondTypeVerbs();

                var firstTypeText = new StringBuilder();
                firstTypeText.AppendLine("<b>Переходные глаголы:</b>");
                firstTypeText.AppendLine("<i>Инфинитив - Морфема в прошедшем времени - Перевод</i>");
                foreach (var verb in firstTypeVerbs)
                {
                    firstTypeText.AppendLine($"{verb.Inf} - {verb.Past} - {verb.Trans}");
                }
                var firstTypeMessage = await _bot.SendMessage(id, firstTypeText.ToString(), parseMode: ParseMode.Html);
                messageIds.Add(firstTypeMessage.MessageId);

                var secondTypeText = new StringBuilder();
                secondTypeText.AppendLine("<b>Непереходные глаголы:</b>");
                secondTypeText.AppendLine("<i>Инфинитив - Морфема в прошедшем времени - Перевод</i>");
                foreach (var verb in secondTypeVerbs)
                {
                    secondTypeText.AppendLine($"{verb.Inf} - {verb.Past} - {verb.Trans}");
                }
                var secondTypeMessage = await _bot.SendMessage(id, secondTypeText.ToString(), parseMode: ParseMode.Html);
                messageIds.Add(secondTypeMessage.MessageId);

                helpMessages[id] = messageIds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending help to user {id}: {ex.Message}");
            }

            

        }
        public async Task SafeDeleteHelpMessages(long userId)
        {
            try
            {
                await _bot.DeleteMessages(userId,helpMessages[userId]);
                helpMessages.Remove(userId);
            }
            catch
            {
                Console.WriteLine("Ошибка удаления справки");
            }
        }

        public async Task SendReportToAllModerators(long reporterId,Message message)
        {
            foreach (var moder in moderators)
            {
                await _bot.SendMessage(moder, $"🆘 Вам поступило новое обращение\n" +
                    $"Отправитель: {reporterId}  @{message?.From?.Username ??
                    message?.From?.FirstName ?? "скрыл юзернейм"} :\n" + message.Text);
            }
            await _bot.SendMessage(reporterId, "Ваше обращение было успешно доставлено, ожидайте ответа!");

            await SendMainMenu(reporterId);
        }

        public async Task SendReportHelp(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new KeyboardButton("🔙 Отмена"))
            {
                ResizeKeyboard = true
            };
            await _bot.SendMessage(
                chatId,
                "Если есть вопросы или заметили ошибки в работе бота, напишите сюда и ваше сообщение будет передано модераторам:",
                replyMarkup: keyboard);
        }

    }
}
