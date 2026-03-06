using OssetianVerbsTelegramBot.Models;
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
        public static Dictionary<long, List<int>> messagesToDelete = new();
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
                    new KeyboardButton("🏆 Рейтинг"),
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
        public static async Task SendRating(long id, int msgId)
        {
            await DbUser.UpdateUserRating();
            var ratingMsg = await _bot.SendMessage(id, "Загрузка...");
            await SendDailyRating(id, ratingMsg);
            messagesToDelete[id] = new List<int> { ratingMsg.Id, msgId };
        }

        private static async Task SendDailyRating(long id, Message msg)
        {
            var textRating = "Ежедневный рейтинг:\n";
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Еженедельный", "ratingId:2") },
                new[] { InlineKeyboardButton.WithCallbackData("Ежемесячный", "ratingId:3") },
            });
            var rating = DbUser.tempRating.OrderByDescending(item => item.DailyScore).ThenBy(item => item.Name);
            int pos = rating.Count() - rating.SkipWhile(item => item.UserId != id).Count() + 1;
            textRating += $"1. {rating.ElementAt(0).Name} - {rating.ElementAt(0).DailyScore} очков\n" +
                $"2. {rating.ElementAt(1).Name} - {rating.ElementAt(1).DailyScore} очков\n" +
                $"3. {rating.ElementAt(2).Name}  -  {rating.ElementAt(2).DailyScore} очков\n";
            if (pos == 1 || pos== 2)
            {
                await _bot.EditMessageText(id, msg.Id, textRating);
                await _bot.EditMessageReplyMarkup(
                    id,
                    msg.Id,
                    replyMarkup: keyboard
                );
                return;
            }
            textRating += ".\n.\n";
            if (pos !=3 && pos!=4)
            {
                textRating += $"{pos - 1}. {rating.ElementAt(pos - 2).Name} - {rating.ElementAt(pos - 2).DailyScore} очков\n";
            }
            textRating += $"{pos}. {rating.ElementAt(pos - 1).Name} - {rating.ElementAt(pos - 1).DailyScore} очков\n";
            if (pos!=rating.Count())
                textRating += $"{pos+1}. {rating.ElementAt(pos).Name} - {rating.ElementAt(pos).DailyScore} очков\n";
            await _bot.EditMessageText(id, msg.Id, textRating);
            await _bot.EditMessageReplyMarkup(
                id,
                msg.Id,
                replyMarkup: keyboard
            );
        }
        private static async Task SendWeeklyRating(long id, Message msg)
        {
            var textRating = "Еженедельный рейтинг:\n";
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Ежедневный", "ratingId:1") },
                new[] { InlineKeyboardButton.WithCallbackData("Ежемесячный", "ratingId:3") },
            });
            var rating = DbUser.tempRating.OrderByDescending(item => item.WeeklyScore).ThenBy(item => item.Name);
            int pos = rating.Count() - rating.SkipWhile(item => item.UserId != id).Count() + 1;
            textRating += $"1. {rating.ElementAt(0).Name} - {rating.ElementAt(0).WeeklyScore} очков\n" +
                $"2. {rating.ElementAt(1).Name} - {rating.ElementAt(1).WeeklyScore} очков\n" +
                $"3. {rating.ElementAt(2).Name}  -  {rating.ElementAt(2).WeeklyScore} очков\n";
            if (pos == 1 || pos == 2)
            {
                await _bot.EditMessageText(id, msg.Id, textRating);
                await _bot.EditMessageReplyMarkup(
                    id,
                    msg.Id,
                    replyMarkup: keyboard
                );
                return;
            }
            textRating += ".\n.\n";
            if (pos != 3 && pos != 4)
            {
                textRating += $"{pos - 1}. {rating.ElementAt(pos - 2).Name} - {rating.ElementAt(pos - 2).WeeklyScore} очков\n";
            }
            textRating += $"{pos}. {rating.ElementAt(pos - 1).Name} - {rating.ElementAt(pos - 1).WeeklyScore} очков\n";
            if (pos != rating.Count())
                textRating += $"{pos + 1}. {rating.ElementAt(pos).Name} - {rating.ElementAt(pos).WeeklyScore} очков\n";
            await _bot.EditMessageText(id, msg.Id, textRating);
            await _bot.EditMessageReplyMarkup(
                id,
                msg.Id,
                replyMarkup: keyboard
            );
        }
        private static async Task SendMonthlyRating(long id, Message msg)
        {
            var textRating = "Ежемесячный рейтинг:\n";
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Ежедневный", "ratingId:1") },
                new[] { InlineKeyboardButton.WithCallbackData("Еженедельный", "ratingId:2") },
            });
            var rating = DbUser.tempRating.OrderByDescending(item => item.MonthlyScore).ThenBy(item => item.Name);
            int pos = rating.Count() - rating.SkipWhile(item => item.UserId != id).Count() + 1;
            textRating += $"1. {rating.ElementAt(0).Name} - {rating.ElementAt(0).MonthlyScore} очков\n" +
                $"2. {rating.ElementAt(1).Name} - {rating.ElementAt(1).MonthlyScore} очков\n" +
                $"3. {rating.ElementAt(2).Name}  -  {rating.ElementAt(2).MonthlyScore} очков\n";
            if (pos == 1 || pos == 2)
            {
                await _bot.EditMessageText(id, msg.Id, textRating);
                await _bot.EditMessageReplyMarkup(
                    id,
                    msg.Id,
                    replyMarkup: keyboard
                );
                return;
            }
            textRating += ".\n.\n";
            if (pos != 3 && pos != 4)
            {
                textRating += $"{pos - 1}. {rating.ElementAt(pos - 2).Name} - {rating.ElementAt(pos - 2).MonthlyScore} очков\n";
            }
            textRating += $"{pos}. {rating.ElementAt(pos - 1).Name} - {rating.ElementAt(pos - 1).MonthlyScore} очков\n";
            if (pos != rating.Count())
                textRating += $"{pos + 1}. {rating.ElementAt(pos).Name} - {rating.ElementAt(pos).MonthlyScore} очков\n";
            await _bot.EditMessageText(id, msg.Id, textRating);
            await _bot.EditMessageReplyMarkup(
                id,
                msg.Id,
                replyMarkup: keyboard
            );
        }
        public static async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            switch (callbackQuery.Data?.Split(":")[1])
            {
                case "1":await SendDailyRating(callbackQuery.Message!.Chat.Id, callbackQuery.Message);break;
                case "2": await SendWeeklyRating(callbackQuery.Message!.Chat.Id, callbackQuery.Message); break;
                case "3": await SendMonthlyRating(callbackQuery.Message!.Chat.Id, callbackQuery.Message); break;
            }
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

        public static async Task SendHelp(long id, int msgId)
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

                messagesToDelete[id] = messageIds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending help to user {id}: {ex.Message}");
            }

            

        }
        public static async Task SafeDeleteHelpMessages(long userId)
        {
            try
            {
                await _bot.DeleteMessages(userId,messagesToDelete[userId]);
                messagesToDelete.Remove(userId);
            }
            catch
            {
                Console.WriteLine("Ошибка удаления справки");
            }
        }

        public static async Task SendReportToAllModerators(long reporterId,Message message)
        {
            foreach (var moder in moderators)
            {
                await _bot.SendMessage(moder, $"🆘 Вам поступило новое обращение\n" +
                    $"Отправитель: {reporterId}  @{message?.From?.Username ??
                    message?.From?.FirstName ?? "скрыл юзернейм"} :\n" + message?.Text);
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
