//#define WITH_HUGE_TEST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.Json;

namespace TestJSONParser
{
    public class BasicTests
    {
        const string SMALL_TEST_FILE_PATH = @"..\..\small.json.txt"; // avg: 4kb ~ 1ms
        const string FATHERS_TEST_FILE_PATH = @"..\..\fathers.json.txt"; // avg: 12mb ~ 1sec
#if WITH_HUGE_TEST
        const string HUGE_TEST_FILE_PATH = @"..\..\huge.json.txt"; // avg: 180mb ~ 20sec
#endif
        public static void DateTimeTest()
        {
            Console.WriteLine("Basic Tests - DateTime");
            Console.WriteLine();

            var JSON_DATE = new
            {
                Year = 0,
                Month = 0,
                Day = 0
            };

            Reviver ToDate =
                (target, type, key, value) =>
                    ((target == typeof(int)) && (key == null)) ?
                        (Func<object>)(() => Convert.ToInt32(value)) :
                        ((target == JSON_DATE.GetType()) && (key == null)) ?
                            (Func<object>)
                            (
                                () => new DateTime
                                (
                                    value.As(JSON_DATE).Year,
                                    value.As(JSON_DATE).Month,
                                    value.As(JSON_DATE).Day
                                )
                            ) :
                            null;

            var dateTime =
                JSON_DATE.
                FromJson
                (
                    @" { ""Year"": 1970, ""Month"": 5, ""Day"": 10 }",
                    Parse.Value,
                    ToDate
                ).As<DateTime>();

            Console.WriteLine(dateTime);
            Console.WriteLine();
            System.Diagnostics.Debug.Assert(dateTime == new DateTime(1970, 5, 10));
            Console.WriteLine("Passed - Press a key...");
            Console.WriteLine();
            Console.ReadKey();
        }

        public class Computer { public string Type { get; set; } }

        public class Address { public string City { get; set; } }

        public class Person
        {
            public string Name { get; set; }
            public IList<Computer> Computers { get; set; }
            public IDictionary<int, string> Codes { get; set; }
            public IDictionary<string, Address> Addresses { get; set; }
        }

        static Reviver integerKey =
            (target, type, key, value) =>
                ((target == typeof(int)) && (key is bool)) ?
                (Func<object>)
                (() => Convert.ToInt32(value)) :
                null;

        public static void PersonTest()
        {
            Console.WriteLine("Basic Tests - Person");
            Console.WriteLine();
            var person = Parse.Value.
                As<Person>
                (
                    @"
                        {
                            ""Addresses"": {
                                ""Main"" : { ""City"": ""Paris"" },
                                ""Secondary"" : { ""City"": ""Geneva"" }
                            },
                            ""Codes"": {
                                ""1"": ""one"",
                                ""2"": ""two"",
                                ""3"": ""three"",
                                ""4"": ""four"",
                                ""5"": ""five""
                            },
                            ""Name"": ""Peter"",
                            ""Computers"": [
                                { ""Type"": ""Laptop"" },
                                { ""Type"": ""Phone"" }
                            ]
                        }   ",
                    integerKey
                );
            System.Diagnostics.Debug.Assert(person.Name == "Peter");
            System.Diagnostics.Debug.Assert(person.Codes.Keys.Count == 5);
            System.Diagnostics.Debug.Assert(person.Codes[5] == "five");
            System.Diagnostics.Debug.Assert(person.Computers.Count == 2);
            System.Diagnostics.Debug.Assert(person.Computers[1].Type == "Phone");
            System.Diagnostics.Debug.Assert(person.Addresses["Main"].City == "Paris");
            System.Diagnostics.Debug.Assert(person.Addresses["Secondary"].City == "Geneva");
            Console.WriteLine("Passed - Press a key...");
            Console.WriteLine();
            Console.ReadKey();
        }

        public static void PersonAnonTest()
        {
            Console.WriteLine("Basic Tests - Person (anonymous types)");
            Console.WriteLine();

            var PERSON = new
            {
                Name = "",
                Computers = new[]
                {
                    new
                    {
                        Type = ""
                    }
                },
                Addresses = new[]
                {
                    new
                    {
                        City = ""
                    }
                },
                Codes = (null as IDictionary<int, string>)
            };

            var person = PERSON.
                FromJson
                (
                    @"
                        {
                            ""Addresses"": [
                                { ""City"": ""Paris"" },
                                { ""City"": ""Geneva"" }
                            ],
                            ""Name"": ""Paul"",
                            ""Codes"": {
                                ""1"": ""one"",
                                ""2"": ""two"",
                                ""3"": ""three"",
                                ""4"": ""four"",
                                ""5"": ""five""
                            },
                            ""Computers"": [
                                { ""Type"": ""Laptop"" },
                                { ""Type"": ""Phone"" }
                            ]
                        }   ",
                    integerKey
                );
            System.Diagnostics.Debug.Assert(person.Name == "Paul");
            System.Diagnostics.Debug.Assert(person.Codes.Keys.Count == 5);
            System.Diagnostics.Debug.Assert(person.Codes[5] == "five");
            System.Diagnostics.Debug.Assert(person.Computers.Length == 2);
            System.Diagnostics.Debug.Assert(person.Computers[1].Type == "Phone");
            System.Diagnostics.Debug.Assert(person.Addresses[0].City == "Paris");
            System.Diagnostics.Debug.Assert(person.Addresses[1].City == "Geneva");
            Console.WriteLine("Passed - Press a key...");
            Console.WriteLine();
            Console.ReadKey();
        }

        public static void Top10Youtube2013Test()
        {
            Console.WriteLine("Top 10 Youtube 2013 Test - JSON parse...");
            Console.WriteLine();
            System.Net.WebRequest www = System.Net.WebRequest.Create("https://gdata.youtube.com/feeds/api/videos?q=2013&max-results=10&v=2&alt=jsonc");
            using (System.IO.Stream stream = www.GetResponse().GetResponseStream())
            {
                // Yup, as simple as this, step #1:
                var YOUTUBE_SCHEMA = new
                {
                    Data = new
                    {
                        Items = new[]
                        {
                            new
                            {
                                Title = "",
                                Category = "",
                                Uploaded = DateTime.Now,
                                Updated = DateTime.Now,
                                Player = new
                                {
                                    Default = ""
                                }
                            }
                        }
                    }
                };

                // And as easy as that, step #2:
                var parsed = YOUTUBE_SCHEMA.
                    FromJson
                    (
                        stream,
                        (target, type, key, value) =>
                            // maps: "data" => "Data", "items" => "Items", "title" => "Title", ...etc
                            (key is bool) ?
                                (Func<object>)(() => String.Concat((char)(value.ToString()[0] - 32), value.ToString().Substring(1))) :
                                null,
                        (target, type, key, value) =>
                            ((target == typeof(DateTime)) && (key == null)) ?
                                (Func<object>)(() => DateTime.Parse((string)value)) :
                                null
                    );

                Console.WriteLine();
                foreach (var item in parsed.Data.Items)
                {
                    var title = item.Title;
                    var category = item.Category;
                    var uploaded = item.Uploaded;
                    var player = item.Player;
                    var link = player.Default;
                    Console.WriteLine("\t\"{0}\" (category: {1}, uploaded: {2})", title, category, uploaded);
                    Console.WriteLine("\t\tURL: {0}", link);
                    Console.WriteLine();
                }
                Console.WriteLine("Press a key...");
                Console.WriteLine();
                Console.ReadKey();
            }
        }

        // Note: fathers.json.txt was generated using:
        // http://experiments.mennovanslooten.nl/2010/mockjson/tryit.html
        // avg: file size ~ exec time (on Lenovo Win7 PC, i5, 2.50GHz, 6Gb)
        // small.json.txt... avg: 4kb ~ 1ms
        // fathers.json.txt... avg: 12mb ~ 1sec
        // huge.json.txt... avg: 180mb ~ 20sec
        public static void SmallTest()
        {
            string small = System.IO.File.ReadAllText(SMALL_TEST_FILE_PATH);
            Console.WriteLine("Small Test - JSON parse... {0} bytes ({1} kb)", small.Length, ((decimal)small.Length / (decimal)1024));
            Console.WriteLine();

            Console.WriteLine("\tParsed by {0} in...", typeof(Parser).FullName);
            DateTime start = DateTime.Now;
            var obj = Parse.Value.FromJson<object>(small);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start).TotalMilliseconds);
            Console.WriteLine();
            Console.WriteLine("Press a key...");
            Console.WriteLine();
            Console.ReadKey();
        }

        public static void HugeTest()
        {
#if WITH_HUGE_TEST
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

            Console.WriteLine("\tParsed by {0} in...", typeof(Parser).FullName);
            DateTime start2 = DateTime.Now;
            obj = Parse.Value.FromJson<object>(json);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start2).TotalMilliseconds);
            Console.WriteLine();
            Console.WriteLine("Press a key...");
            Console.WriteLine();
            Console.ReadKey();
#endif
        }

        public static void FathersTest()
        {
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

            Console.WriteLine("\tParsed by {0} in...", typeof(Parser).FullName);
            DateTime start2 = DateTime.Now;
            var myObj = Parse.Value.FromJson<object>(json);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start2).TotalMilliseconds);
            Console.WriteLine();

            Console.WriteLine("Press '1' to inspect our result object,\r\nany other key to inspect Microsoft's JS serializer result object...");
            var parsed = ((Console.ReadKey().KeyChar == '1') ? myObj : msObj);

            IList<object> fathers = parsed.JsonObject()["fathers"].JsonArray();
            Console.WriteLine();
            Console.WriteLine("Found : {0} fathers", fathers.Count);
            Console.WriteLine();
            Console.WriteLine("Press a key to list them...");
            Console.WriteLine();
            Console.ReadKey();
            Console.WriteLine();
            foreach (object father in fathers)
            {
                var name = (string)father.JsonObject()["name"];
                var sons = father.JsonObject()["sons"].JsonArray();
                var daughters = father.JsonObject()["daughters"].JsonArray();
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