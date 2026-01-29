using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot
{
    public static class ComplimentGenerator
    {
        static Random rnd = new Random();

        static string[] Compliments = {
                "Правильно! 👍",
                "Верно! ✅",
                "Корректно 👌",
                "Точно! 🎯",
                "Ответ верный ✅",
                "Правильный ответ! 👏",
                "Верный ответ 👍",
                "Ответ принят ✅",
                "Верное решение! 🎉",
                "Правильно 😊",
                "Верно! 🥳",
                "Отлично! ⭐",
                "Супер! 🔥",
                "Молодец! ✨",
                "Браво! 🎊",
                "Идеально! 💯",
                "Прекрасно! 🌟",
                "Зачёт! 📝",
                "Принято! ✅",
                "Успех! 🚀"
        };

        static public string GetRandomCompliment()
        {
            return Compliments[rnd.Next(Compliments.Length)];
        }
    }
}
