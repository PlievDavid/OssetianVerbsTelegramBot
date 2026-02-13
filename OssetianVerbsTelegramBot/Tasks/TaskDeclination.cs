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
        public TaskDeclination(TelegramBotClient bot):base(bot) { }
        public override async Task SendNextQuestion()
        {
            var sentence = DbSentencesImport.GetRandomSentenceByVerbInf(_session.Verbs[_session.CurrentIndex].Inf);
            _session.Sentences.Add(sentence);
            await _bot.SendMessage(chatId, $"№{_session.CurrentIndex + 1}/10\n\nПереведите предложение: <b>{sentence.Russian}</b>", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        public async Task HandleMessageAnswer(Message message)
        {
            var rightAns = _session.Sentences[_session.CurrentIndex].Ossetian.ToLower();

            string msgText = message.Text.ToLower();
            if (rightAns.Contains(","))
            {
                var temp = rightAns.Split(", ");
                for (int i = 0; i < temp.Length; i++)
                {
                    if (msgText == temp[i])
                    {
                        _session.Score++;
                        await DbUser.UpdateUserStat(chatId.ToString(), _session.Sentences[_session.CurrentIndex].VerbInf, false);
                        await _bot.SendMessage(chatId, ComplimentGenerator.GetRandomCompliment());
                        break;
                    }
                    if (i==temp.Count()-1)
                    {
                        await DbUser.UpdateUserStat(chatId.ToString(), _session.Sentences[_session.CurrentIndex].VerbInf, true);
                        await _bot.SendMessage(chatId, "Неверно! Правильно: " + rightAns);
                    }
                }
            }
            else
            {
                if (msgText == rightAns)
                {
                    await HandleCorrectAnswer();
                }
                else
                {
                    await HandleIncorrectAnswer();
                }
            }
            _session.CurrentIndex++;

            if (_session.CurrentIndex < _session.Verbs.Count)
                await SendNextQuestion();
            else
                await EndTask();
        }
    }
}
