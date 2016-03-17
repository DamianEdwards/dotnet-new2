using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_new2
{
    public static class ConsoleUtils
    {
        public static int ReadInt(int max)
        {
            var buffer = new List<char>();

            while (true)
            {
                var key = Console.ReadKey(true);
                if (char.IsDigit(key.KeyChar))
                {
                    Console.Write(key.KeyChar);
                    buffer.Add(key.KeyChar);
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    if (!buffer.Any())
                    {
                        // No chars entered so just return 1
                        Console.WriteLine();
                        return 1;
                    }

                    int selected;
                    if (int.TryParse(new string(buffer.ToArray()), out selected) && selected <= max)
                    {
                        // Number entered is valid so return it
                        Console.WriteLine();
                        return selected;
                    }

                    // Number entered is invalid, clear the selection
                    for (int i = 0; i < buffer.Count; i++)
                    {
                        Console.Write("\b"); // backspace
                    }
                    // BUG: Following code is throwing System.IO.IOException: The handle is invalid
                    //Console.SetCursorPosition(Console.CursorLeft - buffer.Count, Console.CursorTop);
                    //Console.Write(new string (' ', buffer.Count));
                    //Console.SetCursorPosition(Console.CursorLeft - buffer.Count, Console.CursorTop);
                    buffer.Clear();
                }
            }
        }
    }
}
