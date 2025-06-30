using System;
using System.Linq;

namespace Example.Frontend
{
    public static class DemoCheck
    {
        public static bool ClientBasedImplementation;
        public static bool TaskCancelation;
        public static bool Dispose;
        public static bool PropertySet;
        public static bool PropertyGet;
        public static bool TaskExecution;
        public static bool BackwardCall;
        public static bool BasicNonParamsCall;
        public static bool EventCallback;
        public static bool CreatingPrinter;

        public static void Show()
        {
            Console.WriteLine("======================== Demo summary ========================");
            var fields = typeof(DemoCheck).GetFields().OrderBy(i => i.Name).ToList();
            foreach (var field in fields)
            {
                var value = (bool)field.GetValue(null)!;
                if (value)
                {
                    Console.BackgroundColor = ConsoleColor.Green;
                }
                else Console.BackgroundColor = ConsoleColor.Gray;

                Console.Write($"{field.Name}");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine();
            }
        }
    }
}