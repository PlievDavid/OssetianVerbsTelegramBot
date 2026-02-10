using OssetianVerbsTelegramBot.Models;
using OssetianVerbsTelegramBot.Tasks.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OssetianVerbsTelegramBot.Tasks
{
    internal class TaskDeclination: BaseTask, IMessageTask
    {
        public TaskDeclination(TelegramBotClient bot, Dictionary<long, TestSession> sessions):base(bot, sessions) { }
        public override async Task SendNextQuestion(long chatId, TestSession session)
        {
            if (session.CurrentIndex > session.Verbs.Count - 1)
            {
                await _bot.SendMessage(chatId, $"Вы закончили тест, количество правильных ответов: {session.Score}/10");
                _sessions.Remove(chatId);
                return;
            }

            var sentence = DbSentencesImport.GetRandomSentenceByVerbInf(session.Verbs[session.CurrentIndex].Inf);
            _sessions[chatId].Sentences.Add(sentence);
            await _bot.SendMessage(chatId, $"№{session.CurrentIndex + 1}/10\n\nПереведите предложение: <b>{sentence.Russian}</b>", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        public async Task HandleMessageAnswer(Message message)
        {
            var chatId = message.Chat.Id;

            var session = _sessions[chatId];
            var rightAns = session.Sentences[session.CurrentIndex].Ossetian.ToLower();

            string msgText = message.Text.ToLower();
            if (rightAns.Contains(","))
            {
                var temp = rightAns.Split(", ");
                for (int i = 0; i < temp.Length; i++)
                {
                    if (msgText == temp[i])
                    {
                        session.Score++;
                        await DbUser.UpdateUserStat(chatId.ToString(), session.Sentences[session.CurrentIndex].VerbInf, false);
                        await _bot.SendMessage(chatId, ComplimentGenerator.GetRandomCompliment());
                        break;
                    }
                    if (i==temp.Count()-1)
                    {
                        await DbUser.UpdateUserStat(chatId.ToString(), session.Sentences[session.CurrentIndex].VerbInf, true);
                        await _bot.SendMessage(chatId, "Неверно! Правильно: " + rightAns);
                    }
                }
            }
            else
            {
                if (msgText == rightAns)
                {
                    session.Score++;
                    await DbUser.UpdateUserStat(chatId.ToString(), session.Sentences[session.CurrentIndex].VerbInf, false);
                    await _bot.SendMessage(chatId, ComplimentGenerator.GetRandomCompliment());
                }
                else
                {
                    await DbUser.UpdateUserStat(chatId.ToString(), session.Sentences[session.CurrentIndex].VerbInf, true);
                    await _bot.SendMessage(chatId, "Неверно! Правильно: " + rightAns);
                }
            }
            session.CurrentIndex++;
            await SendNextQuestion(chatId, session);
        }
    }
}
