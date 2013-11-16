//#define WITH_HUGE_TEST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.JSON;

namespace TestJSONParser
{
    class Program
    {
        // Note: fathers.json.txt was generated using:
        // http://experiments.mennovanslooten.nl/2010/mockjson/tryit.html
        // avg: file size ~ exec time (on Lenovo Win7 PC, i5, 2.50GHz, 6Gb)
        const string FATHERS_TEST_FILE_PATH = @"..\..\fathers.json.txt"; // avg: 12mb ~ 0.8sec
        const string SMALL_TEST_FILE_PATH = @"..\..\small.json.txt"; // avg: 4kb ~ 1ms
#if WITH_HUGE_TEST
        const string HUGE_TEST_FILE_PATH = @"..\..\huge.json.txt"; // avg: 180mb ~ 16sec
#endif
        static Parser parser = new Parser (new ParserSettings { LiteralsBuffer = 1024 });

#if WITH_HUGE_TEST
        static void HugeTest()
        {
            string json = System.IO.File.ReadAllText(HUGE_TEST_FILE_PATH);
            object obj;
            Console.WriteLine("Huge Test - JSON parse... {0} kb ({1} mb)", (int)(json.Length / 1024), (int)(json.Length / (1024 * 1024)));
            Console.WriteLine();

            /*var serializer = new System.Web.Script.Serialization.JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue
            };
            Console.WriteLine("\tParsed by {0} in...", serializer.GetType().FullName);
            DateTime start1 = DateTime.Now;
            obj = serializer.DeserializeObject(json);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start1).TotalMilliseconds);
            Console.WriteLine();*/

            Console.WriteLine("\tParsed by {0} in...", jsonParser.GetType().FullName);
            DateTime start2 = DateTime.Now;
            obj = jsonParser.Parse(json);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start2).TotalMilliseconds);
            Console.WriteLine("Press a key...");
            Console.WriteLine();
            Console.ReadKey();
        }
#endif

        static void Top10Youtube2013Test()
        {
            // Yup, as easy as this, step #1:
            var YOUTUBE_SCHEMA = new
                {
                    data = new
                    {
                        items = new[]
                        {
                            new
                            {
                                title = "",
                                category = "",
                                uploaded = "",
                                player =
                                new
                                {
                                    @default = ""
                                }
                            }
                        }
                    }
                };

            Console.WriteLine("Top 10 Youtube 2013 Test - JSON parse...");
            Console.WriteLine();
            System.Net.WebRequest www = System.Net.WebRequest.Create("https://gdata.youtube.com/feeds/api/videos?q=2013&max-results=10&v=2&alt=jsonc");
            using (System.IO.Stream stream = www.GetResponse().GetResponseStream())
            {
                // And as easy as that, step #2:
                var parsed = parser.Parse(stream, YOUTUBE_SCHEMA);

                Console.WriteLine();
                foreach (var item in parsed.data.items)
                {
                    var title = item.title;
                    var category = item.category;
                    var uploaded = item.uploaded;
                    var player = item.player;
                    var link = player.@default;
                    Console.WriteLine("\t\"{0}\" (category: {1}, uploaded: {2})", title, category, uploaded);
                    Console.WriteLine("\t\tURL: {0}", link);
                    Console.WriteLine();
                }
                Console.WriteLine("Press a key...");
                Console.WriteLine();
                Console.ReadKey();
            }
        }

        static void Main(string[] args)
        {
#if WITH_HUGE_TEST
            HugeTest();
#endif
            Top10Youtube2013Test();

            string small = System.IO.File.ReadAllText(SMALL_TEST_FILE_PATH);
            Console.WriteLine("Small Test - JSON parse... {0} bytes ({1} kb)", small.Length, ((decimal)small.Length / (decimal)1024));
            Console.WriteLine();

            Console.WriteLine("\tParsed by {0} in...", parser.GetType().FullName);
            DateTime start = DateTime.Now;
            var obj = parser.Parse(small);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start).TotalMilliseconds);
            Console.WriteLine();

            string json = System.IO.File.ReadAllText(FATHERS_TEST_FILE_PATH);
            Console.WriteLine("Fathers Test - JSON parse... {0} kb ({1} mb)", (int)(json.Length / 1024), (int)(json.Length / (1024 * 1024)));
            Console.WriteLine();

            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue
            };
            Console.WriteLine("\tParsed by {0} in...", serializer.GetType().FullName);
            DateTime start1 = DateTime.Now;
            var msObj = serializer.DeserializeObject(json);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start1).TotalMilliseconds);
            Console.WriteLine();

            Console.WriteLine("\tParsed by {0} in...", parser.GetType().FullName);
            DateTime start2 = DateTime.Now;
            var myObj = parser.Parse(json);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start2).TotalMilliseconds);
            Console.WriteLine();

            Console.WriteLine("Press '1' to inspect our result object,\r\nany other key to inspect Microsoft's JS serializer result object...");
            var parsed = ((Console.ReadKey().KeyChar == '1') ? myObj : msObj);

            IList<object> items = (IList<object>)((IDictionary<string, object>)parsed)["fathers"];
            Console.WriteLine();
            Console.WriteLine("Found : {0} fathers", items.Count);
            Console.WriteLine();
            Console.WriteLine("Press a key to list them...");
            Console.WriteLine();
            Console.ReadKey();
            Console.WriteLine();
            foreach (object item in items)
            {
                var father = item.JSONObject();
                var name = (string)father["name"];
                var sons = father["sons"].JSONArray();
                var daughters = father["daughters"].JSONArray();
                Console.WriteLine("{0}", name);
                Console.WriteLine("\thas {0} son(s), and {1} daughter(s)", sons.Count, daughters.Count);
                Console.WriteLine();
            }
            Console.WriteLine();
            Console.WriteLine("The end... Press a key...");

            Console.ReadKey();
        }
    }
}