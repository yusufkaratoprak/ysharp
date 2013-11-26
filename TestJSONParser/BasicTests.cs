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
		const string SMALL_TEST_FILE_PATH = @"..\..\small.json.txt";
		const string FATHERS_TEST_FILE_PATH = @"..\..\fathers.json.txt";
#if WITH_HUGE_TEST
		const string HUGE_TEST_FILE_PATH = @"..\..\huge.json.txt"; // avg: 180mb ~ 20sec
#endif

		internal static class Sample_Revivers
		{
			internal static readonly Func<Outer, Type, string, object> CamelCaseToPascalCase =
				JSON.Map(default(string)).
							Using // Turn some key names from lower camel case to Pascal case:
							(
								(outer, type, value) => ((outer.Key == typeof(string)) && !value.StartsWith("%")) ? (Func<object>)
									(() => String.Concat((char)(value[0] - 32), value.Substring(1))) : null
							);

			internal static readonly Func<Outer, Type, double, object> DoubleToInteger =
				JSON.Map(default(double)).
					Using
					(
						(outer, type, value) =>
							((outer.Key == null) && ((type == typeof(int)) || (type == typeof(object)) || (outer.Type == typeof(int)))) ? (Func<object>)
								(() => Convert.ToInt32(value)) :
								null
					);

			internal static readonly Func<Outer, Type, string, object> Person_Codes_Key =
				JSON.Map(default(string)).
					Using
					(
						(outer, type, value) =>
							(
								(outer.Key == typeof(object)) &&
								(type == typeof(int))
							) ? (Func<object>)
								(() => int.Parse((value[0] != '$') ? value : value.Substring(1))) :
								null
					);
		}

		public static void CastTests()
		{
			Func<int, decimal, string> fn_s = (n, x) => (n + x).ToString();
			Func<int, decimal, object> fn_o = JSON.UpCast(fn_s);

			object s = (string)fn_o(37, 100.5m);

			System.Diagnostics.Debug.Assert(decimal.Parse(s as string) == 137.5m);
		}

		public static void MostBasicTest()
		{
			Console.Clear();
			Console.WriteLine("Most Basic Tests");
			Console.WriteLine();

			string testerr;
			try
			{
				testerr = JSON.Map("").FromJson("");
			}
			catch (Exception ex)
			{
				testerr = ex.Message;
			}

			// Basic, common atom cases types:
			string test0 = JSON.Map("").FromJson("\"\"");
			string test1 = JSON.Map("").FromJson("\" \"");
			string test2 = JSON.Map("").FromJson("\"\\ta\"");
			double test3 = JSON.Map(0.0).FromJson("123.456");
			double test4 = JSON.Map(0.0).FromJson("789");
			string test5 = JSON.Map("").FromJson("\"\"");
			object[] test6 = JSON.Map(null as object[]).FromJson("[]");
			double[] test7 = JSON.Map(null as double[]).FromJson("[1,2,3]");
			object test8 = JSON.Map(null as object).FromJson("{\"First\":\"John\",\"Last\":\"Smith\"}");

			// Let's go anonymous types, now:
			var test9 = JSON.Map(new { Id = "" }).FromJson("{\"Id\":\"Something\"}");
			var test10 = JSON.Map(new[] { new { Id = .0 } }).FromJson("[{\"Id\":1}, {\"Id\":2}]");
			var test11 = JSON.Map(new { ZipCode = 0 }).
				FromJson
				(
					" { ZipCode: 75015 } ",
					new ParserSettings { AcceptIdentifiers = true },
					Sample_Revivers.DoubleToInteger
				);

			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(testerr));
			System.Diagnostics.Debug.Assert(test0 == "");
			System.Diagnostics.Debug.Assert(test1 == " ");
			System.Diagnostics.Debug.Assert(test2 == "\ta");
			System.Diagnostics.Debug.Assert(test3 == 123.456);
			System.Diagnostics.Debug.Assert(test4 == 789.0);
			System.Diagnostics.Debug.Assert(test5 == "");
			System.Diagnostics.Debug.Assert(test6.Length == 0);
			System.Diagnostics.Debug.Assert(test7[1] == 2.0);
			System.Diagnostics.Debug.Assert((string)test8.Object()["First"] == "John");
			System.Diagnostics.Debug.Assert((string)test8.Object()["Last"] == "Smith");
			System.Diagnostics.Debug.Assert(test9.Id == "Something");
			System.Diagnostics.Debug.Assert(test10[1].Id == 2.0);
			System.Diagnostics.Debug.Assert(test11.ZipCode == 75015);
			Console.Write("Passed - Press a key...");
			Console.ReadKey();
		}

		public static void DateTimeTest()
		{
			Console.Clear();
			Console.WriteLine("Basic Tests - DateTime");
			Console.WriteLine();

			var DATE_JSON = new
			{
				the_Year = 0,
				the_Month = 0,
				the_Day = 0
			};

			// Reviver that must be in this local scope,
			// because of the anonymous type it uses:
			var ToDateTime =
				JSON.Map(DATE_JSON).
					Using
					(
						(outer, type, value) =>
							((outer.Key == null) && (type == typeof(DateTime))) ? (Func<object>)
								(
									() =>
										(value != null) ?
											new DateTime(value.the_Year, value.the_Month, value.the_Day) :
											default(DateTime)
								) :
								null
					);

			DateTime dateTime = JSON.Map(DATE_JSON).
				FromJson
				(
					default(DateTime),
					@" { ""the_Year"": 1970, ""the_Month"": 5, ""the_Day"": 10 }",
					Sample_Revivers.DoubleToInteger,
					ToDateTime
				);

			Console.WriteLine(dateTime);
			Console.WriteLine();
			System.Diagnostics.Debug.Assert(dateTime == new DateTime(1970, 5, 10));
			Console.Write("Passed - Press a key...");
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


		public static void PersonTest()
		{
			Console.Clear();
			Console.WriteLine("Basic Tests - Person");
			Console.WriteLine();

			Person person = JSON.Map(null as Person).
				FromJson
				(
					@"
					{
						""Addresses"": {
							""Main"" : { City: ""Paris"" },
							Secondary : { ""City"": ""Geneva"" }
						},
						""Codes"": {
							$1: ""one"",
							$2: ""two"",
							$3: ""three"",
							$4: ""four"",
							$5: ""five""
						},
						""Name"": ""Peter"",
						""Computers"": [
							{ ""Type"": ""Laptop"" },
							{ Type: ""Phone"" }
						]
					}   ",
					new ParserSettings { AcceptIdentifiers = true },
					Sample_Revivers.Person_Codes_Key
				);
			System.Diagnostics.Debug.Assert(person.Name == "Peter");
			System.Diagnostics.Debug.Assert(person.Codes.Keys.Count == 5);
			System.Diagnostics.Debug.Assert(person.Codes[5] == "five");
			System.Diagnostics.Debug.Assert(person.Computers.Count == 2);
			System.Diagnostics.Debug.Assert(person.Computers[0].Type == "Laptop");
			System.Diagnostics.Debug.Assert(person.Computers[1].Type == "Phone");
			System.Diagnostics.Debug.Assert(person.Addresses["Main"].City == "Paris");
			System.Diagnostics.Debug.Assert(person.Addresses["Secondary"].City == "Geneva");
			Console.Write("Passed - Press a key...");
			Console.ReadKey();
		}

		public static void PersonAnonymousTest()
		{
			Console.Clear();
			Console.WriteLine("Basic Tests - Person (anonymous types)");
			Console.WriteLine();

			var PERSON_JSON = new
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

			var person = JSON.Map(PERSON_JSON).
				FromJson
				(
					@"
					{
						Addresses : [
							{ ""City"": ""Paris"" },
							{ City: ""Geneva"" }
						],
						""Name"" : ""Paul"",
						Codes   : {
							$1: ""one"",
							$2: ""two"",
							$3: ""three"",
							$4: ""four"",
							$5: ""five""
						},
						""Computers"": [
							{ Type : ""Laptop"" },
							{ ""Type"" : ""Phone"" }
						]
					}   ",
					new ParserSettings { AcceptIdentifiers = true },
					Sample_Revivers.Person_Codes_Key
				);
			System.Diagnostics.Debug.Assert(person.Name == "Paul");
			System.Diagnostics.Debug.Assert(person.Codes.Keys.Count == 5);
			System.Diagnostics.Debug.Assert(person.Codes[5] == "five");
			System.Diagnostics.Debug.Assert(person.Computers.Length == 2);
			System.Diagnostics.Debug.Assert(person.Computers[0].Type == "Laptop");
			System.Diagnostics.Debug.Assert(person.Computers[1].Type == "Phone");
			System.Diagnostics.Debug.Assert(person.Addresses[0].City == "Paris");
			System.Diagnostics.Debug.Assert(person.Addresses[1].City == "Geneva");
			Console.Write("Passed - Press a key...");
			Console.ReadKey();
		}

		public class User
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		public class Venue
		{
			public int Id { get; set; }
			public string Address { get; set; }
		}

		public class Message
		{
			public string Text { get; set; }
			public int FromId { get; set; }
		}

		/* Deals with this SO question :
		 * 
		 * http://stackoverflow.com/questions/19023696/deserialize-dictionarystring-t
		 */
		public static void SO_19023696()
		{
			Console.Clear();
			Console.WriteLine("StackOverflow question 19023696 - Polymorphic, key-driven Test");
			Console.WriteLine();
			string myJson = @"
            [
                {
                  ""%user%"" : { ""id"": 1, ""name"": ""Alex""} ,
                  ""%venue%"" : { ""id"": 465, ""address"": ""Thomas at 68th Street"" },
                  ""%message%"" : { ""text"": ""hello"", ""fromId"": 78 }
                },
                {
                  ""%user%"" : { ""id"": 2, ""name"": ""Carl""} ,
                  ""%message%"" : { ""text"": ""bye"", ""fromId"": 79 }
                }
            ]";

			Dictionary<string, object>[] parsed =
				JSON.Map(null as Dictionary<string, object>[]).
					FromJson
					(
						myJson,
						JSON.Map(default(Dictionary<string, object>)).
							Using // Deal with the main issue raised by the SO question:
							(
								(outer, type, value) =>
									((outer.Hash != null) && outer.Hash.ContainsKey("Name") ? (Func<object>)
									(() => new User { Id = (int)outer.Hash["Id"], Name = (string)outer.Hash["Name"] }) :
										((outer.Hash != null) && outer.Hash.ContainsKey("Address") ? (Func<object>)
										(() => new Venue { Id = (int)outer.Hash["Id"], Address = (string)outer.Hash["Address"] }) :
											((outer.Hash != null) && outer.Hash.ContainsKey("Text") ? (Func<object>)
											(() => new Message { FromId = (int)outer.Hash["FromId"], Text = (string)outer.Hash["Text"] }) :
												null
											)
										)
									)
							),
						Sample_Revivers.CamelCaseToPascalCase,
						Sample_Revivers.DoubleToInteger
					);
			System.Diagnostics.Debug.Assert(parsed[0]["%user%"] is User);
			System.Diagnostics.Debug.Assert(parsed[0]["%venue%"] is Venue);
			System.Diagnostics.Debug.Assert(parsed[0]["%message%"] is Message);
			System.Diagnostics.Debug.Assert(parsed[1]["%user%"] is User);
			System.Diagnostics.Debug.Assert(parsed[1]["%message%"] is Message);
			Console.Write("Passed - Press a key...");
			Console.ReadKey();
		}

		public static void SmallTest()
		{
			Console.Clear();
			string small = System.IO.File.ReadAllText(SMALL_TEST_FILE_PATH);
			Console.WriteLine("Small Test - JSON parse... {0} bytes ({1} kb)", small.Length, ((decimal)small.Length / (decimal)1024));
			Console.WriteLine();

			Console.WriteLine("\tParsed by {0} in...", typeof(Parser).FullName);
			DateTime start = DateTime.Now;
			var obj = JSON.Map(null as object).FromJson(small);
			Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start).TotalMilliseconds);
			Console.WriteLine();
			Console.Write("Press a key...");
			Console.ReadKey();
		}

		public static void Top15Youtube2013Test()
		{
			Console.Clear();
			Console.WriteLine("Top 15 Youtube 2013 Test - JSON parse...");
			Console.WriteLine();

			System.Net.WebRequest www = System.Net.WebRequest.Create("https://gdata.youtube.com/feeds/api/videos?q=2013&max-results=15&v=2&alt=jsonc");
			using (System.IO.Stream stream = www.GetResponse().GetResponseStream())
			{
				var YOUTUBE_JSON = new
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

				var parsed = JSON.Map(YOUTUBE_JSON).
					FromJson
					(
						stream,
						// Needed for Youtube's JSON values such as "uploaded", "updated", etc:
						JSON.Map(default(string)).
							Using
							(
								(outer, type, value) =>
								(
									(outer.Key == null) &&
									(type == typeof(DateTime))
								) ? (Func<object>)
									(() => (!String.IsNullOrEmpty(value) ? DateTime.Parse(value) : default(DateTime))) :
									null
							),
						// Needed to turn keys from lower camel case to Pascal case:
						Sample_Revivers.CamelCaseToPascalCase
					);

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
				Console.Write("Press a key...");
				Console.ReadKey();
			}
		}

		// Note: fathers.json.txt was generated using:
		// http://experiments.mennovanslooten.nl/2010/mockjson/tryit.html
		// avg: file size ~ parse exec time (on Lenovo Win7 PC, i5, 2.50GHz, 6Gb)
		// small.json.txt... avg: 4kb... parsed in ~1ms
		// fathers.json.txt... avg: 12mb... parsed in ~3sec (30k msg... => 10k msg/sec)
		// huge.json.txt... avg: 180mb... parsed in ~20sec
		public static void FathersTest()
		{
			Console.Clear();
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

			Console.WriteLine();
			Console.WriteLine("\tParsed by {0} in...", typeof(Parser).FullName);
			DateTime start2 = DateTime.Now;
			var myObj = JSON.Map(null as object).FromJson(json);
			Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start2).TotalMilliseconds);
			Console.WriteLine();

			Console.WriteLine("Press '1' to inspect our result object,\r\nany other key to inspect Microsoft's JS serializer result object...");
			var parsed = ((Console.ReadKey().KeyChar == '1') ? myObj : msObj);

			IList<object> fathers = parsed.Object()["fathers"].Array();
			Console.WriteLine();
			Console.WriteLine("Found : {0} fathers", fathers.Count);
			Console.WriteLine();
			Console.WriteLine("Press a key to list them...");
			Console.WriteLine();
			Console.ReadKey();
			Console.WriteLine();
			foreach (object father in fathers)
			{
				var name = (string)father.Object()["name"];
				var sons = father.Object()["sons"].Array();
				var daughters = father.Object()["daughters"].Array();
				Console.WriteLine("{0}", name);
				Console.WriteLine("\thas {0} son(s), and {1} daughter(s)", sons.Count, daughters.Count);
				Console.WriteLine();
			}
			Console.WriteLine("Press a key...");
			Console.ReadKey();
		}

		public class Fatherhood
		{
			public IList<Father> Fathers { get; set; }
		}

		public class Father
		{
			public int Id { get; set; }
			public bool Married { get; set; }
			public string Name { get; set; }
			public IList<Son> Sons { get; set; }
			public IList<Daughter> Daughters { get; set; }
		}

		public class Son
		{
			public int Age { get; set; }
			public string Name { get; set; }
		}

		public class Daughter
		{
			public int Age { get; set; }
			public string Name { get; set; }
		}

		public static void FathersTestTyped()
		{
			Console.Clear();
			string json = System.IO.File.ReadAllText(FATHERS_TEST_FILE_PATH);
			Console.WriteLine("Fathers Test (strongly typed) - JSON parse... {0} kb ({1} mb)", (int)(json.Length / 1024), (int)(json.Length / (1024 * 1024)));
			Console.WriteLine();
			Console.WriteLine("\tParsed by {0} in...", typeof(Parser).FullName);
			Console.WriteLine();
			DateTime start = DateTime.Now;
			var parsed = JSON.Map(null as Fatherhood).
				FromJson
				(
					json,
					Sample_Revivers.CamelCaseToPascalCase,
					Sample_Revivers.DoubleToInteger
				);
			Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start).TotalMilliseconds);
			System.Diagnostics.Debug.Assert(parsed != null);
			System.Diagnostics.Debug.Assert(parsed.Fathers != null);
			System.Diagnostics.Debug.Assert(parsed.Fathers.Count > 0);
			Console.WriteLine();
			Console.WriteLine("Press a key...");
			Console.ReadKey();
		}

		public static void HugeTest()
		{
#if WITH_HUGE_TEST
			Console.Clear();
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
			obj = JSON.Map(null as object).FromJson(json);
			Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start2).TotalMilliseconds);
			Console.WriteLine();
			Console.Write("Press a key...");
			Console.ReadKey();
#endif
		}
	}
}