using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    public class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                char choice;
                Console.Clear();
                Console.WriteLine("Press \"1\" for \"That's Life\" sample");
                Console.WriteLine("Press \"2\" for \"Watching TV\" sample");
                Console.WriteLine("Press \"3\" for \"Watching TV Advanced\" sample");
                Console.WriteLine();
                Console.WriteLine("Press ESC to exit");
                if ((choice = Console.ReadKey().KeyChar) != 27)
                    switch (choice)
                    {
                        case '1':
                            ThatsLifeSample.Run();
                            break;
                        case '2':
                            WatchingTvSample.Run();
                            break;
                        case '3':
                            WatchingTvSampleAdvanced.Run();
                            break;
                        default:
                            break;
                    }
                else
                    break;
            }
        }
    }
}