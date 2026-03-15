using OssetianVerbsTelegramBot.Models;
using Sprache;
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
    public class MessageHelper(TelegramBotClient bot)
    {
        TelegramBotClient bot = bot;
        //Геор        Давид       Алан     МД
        public long[] admins = { 946534275, 2033844706, 6242358847, 286858097 };
        public long[] moderators = { 946534275, 2033844706, 6242358847 };

        public HashSet<long> needFeedback = new();
        public Dictionary<long, List<int>> messagesToDelete = new();
        private Dictionary<string, string> _cachedPhotos = new();

        public async Task SendAdminMenu(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]{
                new[] { new KeyboardButton("📝 Backup Базы данных") },
                new[] { new KeyboardButton("🔙 В главное меню") }
            })
            {
                ResizeKeyboard = true
            };
            await bot.SendMessage(chatId: chatId,
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

            await bot.SendMessage(chatId: chatId,
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


            await bot.SendMessage(chatId: chatId,
                text: "<b> Навигация осуществляется с помощью меню</b> 👇", replyMarkup: keyboard, parseMode: ParseMode.Html);
        }

        public async Task SendStatistics(long id)
        {
            var list = await DbUser.GetUserStatById(id.ToString());
            var textStatistics = FormatVerbStats(list);
            await bot.SendMessage(id, textStatistics, parseMode: ParseMode.Markdown);
        }


        #region Все что связано с РЕЙТИНГОМ
        private enum RatingType
        {
            Daily,
            Weekly,
            Monthly
        }

        public async Task SendRating(long id, int msgId)
        {
            await DbUser.UpdateUserRating();
            var ratingMsg = await bot.SendMessage(id, "Загрузка...");
            await SendRating(id, ratingMsg, RatingType.Daily);
            messagesToDelete[id] = new List<int> { ratingMsg.Id, msgId };
        }

        private InlineKeyboardMarkup CreateRatingKeyboard(RatingType currentType)
        {
            var buttons = new List<InlineKeyboardButton[]>();

            var otherTypes = new Dictionary<RatingType, (string text, string callback)>
            {
                [RatingType.Daily] = ("Ежедневный", "ratingId:1"),
                [RatingType.Weekly] = ("Еженедельный", "ratingId:2"),
                [RatingType.Monthly] = ("Ежемесячный", "ratingId:3")
            };

            foreach (var type in Enum.GetValues<RatingType>())
            {
                if (type != currentType)
                {
                    var (text, callback) = otherTypes[type];
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(text, callback) });
                }
            }

            return new InlineKeyboardMarkup(buttons);
        }



        private async Task SendRating(long id, Message msg, RatingType ratingType)
        {
            var (title, scoreSelector) = GetRatingInfo(ratingType);
            var textRating = $"{title}:\n";

            var keyboard = CreateRatingKeyboard(ratingType);

            var rating = GetSortedRating(ratingType);

            int pos = GetUserPosition(rating, id);

            textRating = BuildRatingText(textRating, rating, pos, scoreSelector);

            await bot.EditMessageText(id, msg.Id, textRating);
            await bot.EditMessageReplyMarkup(id, msg.Id, replyMarkup: keyboard);
        }

        private (string title, Func<RatingItem, int> scoreSelector) GetRatingInfo(RatingType type)
        {

            return type switch
            {
                RatingType.Daily => (
                    "Ежедневный рейтинг",
                    u => u.DailyScore
                ),
                RatingType.Weekly => (
                    "Еженедельный рейтинг",
                    u => u.WeeklyScore
                ),
                RatingType.Monthly => (
                    "Ежемесячный рейтинг",
                    u => u.MonthlyScore
                ),
                _ => throw new ArgumentException("Неизвестный тип рейтинга")
            };
        }

        private int GetUserPosition(IEnumerable<RatingItem> rating, long userId)
        {
            var ratingList = rating.ToList(); // Материализуем список для многократного использования
            int index = ratingList.FindIndex(item => item.UserId == userId);
            return index >= 0 ? index + 1 : ratingList.Count + 1;
        }

        private string BuildRatingText(string header, IEnumerable<RatingItem> rating, int userPos, Func<RatingItem, int> scoreSelector)
        {
            var ratingList = rating.ToList();
            var sb = new StringBuilder(header);

            // Топ-3
            for (int i = 0; i < Math.Min(3, ratingList.Count); i++)
            {
                var user = ratingList[i];
                sb.AppendLine($"{i + 1}. {user.Name} - {scoreSelector(user)} очков");
            }

            // Если пользователь не в топ-3
            if (userPos > 3)
            {
                sb.AppendLine(".\n.");

                // Показываем пользователя с соседями
                int start = Math.Max(3, userPos - 2);
                int end = Math.Min(ratingList.Count, userPos + 1);

                for (int i = start; i <= end; i++)
                {
                    if (i != userPos - 1 && i != userPos && i != userPos + 1) continue;

                    var user = ratingList[i - 1];
                    sb.AppendLine($"{i}. {user.Name} - {scoreSelector(user)} очков");
                }
            }

            return sb.ToString();
        }

        private IEnumerable<RatingItem> GetSortedRating(RatingType type)
        {
            return type switch
            {
                RatingType.Daily => DbUser.tempRating.OrderByDescending(item => item.DailyScore),
                RatingType.Weekly => DbUser.tempRating.OrderByDescending(item => item.WeeklyScore),
                RatingType.Monthly => DbUser.tempRating.OrderByDescending(item => item.MonthlyScore),
                _ => throw new ArgumentException("Неизвестный тип рейтинга")
            };
        }
        public async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var msg = callbackQuery.Message;

            switch (callbackQuery.Data?.Split(":")[1])
            {
                case "1": await SendRating(chatId, msg, RatingType.Daily); break;
                case "2": await SendRating(chatId, msg, RatingType.Weekly); break;
                case "3": await SendRating(chatId, msg, RatingType.Monthly); break;
            }
        }
        #endregion


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

            await bot.SendMessage(message.Chat.Id, keyboardInformationString, replyMarkup: markup);
        }


        public async Task<Message> SendPhotoCached(string imagePath, long id)
        {

            if (_cachedPhotos.TryGetValue(imagePath, out var fileId))
            {
                try
                {
                    return await bot.SendPhoto(id, fileId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SENDPHOTOCACHED:    " + ex.Message);
                    _cachedPhotos.Remove(imagePath);
                }
            }

            using (var stream = File.OpenRead(imagePath))
            {
                var photoMessage = await bot.SendPhoto(id, stream);

                fileId = photoMessage.Photo.Last().FileId;
                _cachedPhotos[imagePath] = fileId;

                return photoMessage;
            }

        }


        public async Task SendHelp(long id, int msgId)
        {
            try
            {
                var messageIds = new List<int> { msgId };
                var imagePath = Path.Combine("Images", "declinationRule.jpg");

                var firstTypeVerbs = DbVerbImport.GetAllFirstTypeVerbs();
                var secondTypeVerbs = DbVerbImport.GetAllSecondTypeVerbs();

                var firstTypeText = new StringBuilder();
                firstTypeText.AppendLine("<b>📚 Переходные глаголы:</b>\n");
                firstTypeText.AppendLine("<b>Инфинитив</b> → <i>Прошедшее время</i> → Перевод");
                firstTypeText.AppendLine("─────────────────────────────");
                foreach (var verb in firstTypeVerbs)
                {
                    firstTypeText.AppendLine($"<b>{verb.Infinitive}</b>  →  {verb.Past}  →  {verb.Translation}");
                }
                
                
                var secondTypeText = new StringBuilder();
                secondTypeText.AppendLine("<b>📗 Непереходные глаголы:</b>\n");
                secondTypeText.AppendLine("<b>Инфинитив</b> → <i>Прошедшее время</i> → Перевод");
                secondTypeText.AppendLine("─────────────────────────────");
                foreach (var verb in secondTypeVerbs)
                {
                    secondTypeText.AppendLine($"<b>{verb.Infinitive}</b>  →  {verb.Past}  →  {verb.Translation}");
                }


                var photomessage = await SendPhotoCached(imagePath, id);
                var firstTypeMessage = await bot.SendMessage(id, firstTypeText.ToString(), parseMode: ParseMode.Html);
                var secondTypeMessage = await bot.SendMessage(id, secondTypeText.ToString(), parseMode: ParseMode.Html);

                messageIds.Add(firstTypeMessage.MessageId);
                messageIds.Add(secondTypeMessage.MessageId);
                messageIds.Add(photomessage.Id);
                messagesToDelete[id] = messageIds;
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
                await bot.DeleteMessages(userId, messagesToDelete[userId]);
                messagesToDelete.Remove(userId);
            }
            catch
            {
                Console.WriteLine("Ошибка удаления справки");
            }
        }

        public string FormatVerbStats(List<StatItem> stats)
        {
            var sb = new StringBuilder();
            sb.AppendLine("📊 *СТАТИСТИКА ГЛАГОЛОВ*\n");

            // Группировка по успеваемости
            var excellent = stats.Where(s => s.Percent >= 90).OrderByDescending(s => s.Percent);
            var good = stats.Where(s => s.Percent >= 70 && s.Percent < 90).OrderByDescending(s => s.Percent);
            var average = stats.Where(s => s.Percent >= 50 && s.Percent < 70).OrderByDescending(s => s.Percent);
            var poor = stats.Where(s => s.Percent >= 30 && s.Percent < 50).OrderByDescending(s => s.Percent);
            var bad = stats.Where(s => s.Percent < 30).OrderByDescending(s => s.Percent);

            
            if (excellent.Any())
            {
                sb.AppendLine("🌟 *Отлично усвоены:*");
                foreach (var s in excellent)
                {
                    sb.AppendLine($"`{s.Verb,-12}` ({s.Percent}%)  {s.CorrectCount}/{s.TotalCount}");
                    sb.AppendLine($"{GetProgressBar(s.Percent)}");
                    sb.AppendLine();
                }
                sb.AppendLine();
            }

            if (good.Any())
            {
                sb.AppendLine("👍 *Хорошо:*");
                foreach (var s in good)
                {
                    sb.AppendLine($"`{s.Verb,-12}` ({s.Percent}%)  {s.CorrectCount}/{s.TotalCount}");
                    sb.AppendLine($"{GetProgressBar(s.Percent)}");
                    sb.AppendLine();
                }
                sb.AppendLine();
            }

            if (average.Any())
            {
                sb.AppendLine("👌 *Средне:*");
                foreach (var s in average)
                {
                    sb.AppendLine($"`{s.Verb,-12}` ({s.Percent}%)  {s.CorrectCount}/{s.TotalCount}");
                    sb.AppendLine($"{GetProgressBar(s.Percent)}");
                    sb.AppendLine();
                }
                sb.AppendLine();
            }

            if (poor.Any())
            {
                sb.AppendLine("⚠️ *Нужно повторить:*");
                foreach (var s in poor)
                {
                    sb.AppendLine($"`{s.Verb,-12}` ({s.Percent}%)  {s.CorrectCount}/{s.TotalCount}");
                    sb.AppendLine($"{GetProgressBar(s.Percent)}");
                    sb.AppendLine();
                }
                sb.AppendLine();
            }

            if (bad.Any())
            {
                sb.AppendLine("🔴 *Требуют внимания:*");
                foreach (var s in bad)
                {
                    sb.AppendLine($"`{s.Verb,-12}` ({s.Percent}%)  {s.CorrectCount}/{s.TotalCount}");
                    sb.AppendLine($"{GetProgressBar(s.Percent)}");
                    sb.AppendLine();
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string GetProgressBar(int percent, int length = 10)
        {
            int filled = (int)Math.Round(percent / 100.0 * length);

            //string emoji = percent switch
            //{
            //    >= 80 => "🟢",  // отлично - зеленый
            //    >= 50 => "🟡",  // хорошо - желтый
            //    >= 30 => "🟠",  // средне - оранжевый
            //    >= 10 => "🔴",  // плохо - красный
            //    _ => "⭕"       // очень плохо - пустой красный круг
            //};
            string emoji = percent switch
            {
                >= 80 => "🟩",  // отлично - зеленый
                >= 35 => "🟨",  // хорошо - желтый
                _ => "🟥",  // плохо - красный
            };

            string filledPart = string.Concat(Enumerable.Repeat(emoji, filled));
            string emptyPart = string.Concat(Enumerable.Repeat("⬜️", length - filled));

            return filledPart + emptyPart;
        }

        public async Task SendReportToAllModerators(long reporterId, Message message)
        {
            foreach (var moder in moderators)
            {
                await bot.SendMessage(moder, $"🆘 Вам поступило новое обращение\n" +
                    $"Отправитель: {reporterId}  @{message?.From?.Username ??
                    message?.From?.FirstName ?? "undefined"} :\n" + message?.Text);
            }
            await bot.SendMessage(reporterId, "Ваше обращение было успешно доставлено, ожидайте ответа!");

            await SendMainMenu(reporterId);
        }

        public async Task SendReportHelp(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new KeyboardButton("🔙 Отмена"))
            {
                ResizeKeyboard = true
            };
            await bot.SendMessage(
                chatId,
                "Если есть вопросы или заметили ошибки в работе бота, напишите сюда и ваше сообщение будет передано модераторам:",
                replyMarkup: keyboard);
        }

    }
}
