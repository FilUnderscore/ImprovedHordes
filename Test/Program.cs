using System;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine("{{{3}}}", 2, '{', '}');
        }

        private static string GetWithin(string expr, int level, char start, char end)
        {
            // {{{}}}

            int currentLevel = 0;
            int startIndex = 0, endIndex = 0;

            for (var i = 0; i < expr.Length; i++)
            {
                if (expr[i] == start && currentLevel < level)
                {
                    startIndex = i;
                    currentLevel++;
                }
                else if (expr[i] == end && currentLevel == level)
                {
                    endIndex = i;
                    break;
                }
            }

            return expr.Substring(startIndex, endIndex - startIndex);
        }
    }
}
