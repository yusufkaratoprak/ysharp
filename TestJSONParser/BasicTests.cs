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

		internal static class Sample_Revivers
		{
			internal static readonly Reviver<double, int> ToInteger =
				Map.Value(default(double), default(int)).
					Using
					(
						(type, key, value) =>
							() => Convert.ToInt32(value)
					);

			internal static readonly Reviver<string, int> Person_Codes_Key =
				Map.Value(default(string), default(int)).
					Using
					(
						(type, key, value) =>
							(key == typeof(int)) ?
								(Func<int>)
								(() => int.Parse((value[0] != '$') ? value : value.Substring(1))) :
								null
					);
		}

		public static void MostBasicTest()
		{
			Console.Clear();
			Console.WriteLine("Most Basic Tests");
			Console.WriteLine();

			string testerr;
			try
			{
				testerr = "".FromJson("");
			}
			catch (Exception ex)
			{
				testerr = ex.Message;
			}
			var test0 = "".FromJson("\"\"");
			var test1 = "".FromJson("\" \"");
			var test2 = "".FromJson("\"\\ta\"");
			var test3 = default(double).FromJson("123.456");
			var test4 = default(double).FromJson("789");
			var test5 = "".FromJson("\"\"");
			var test6 = (null as object[]).FromJson("[]");
			var test7 = (null as double[]).FromJson("[1,2,3]");
			var test8 = (null as object).FromJson("{\"First\":\"John\",\"Last\":\"Smith\"}");
			var test9 = new { Id = "" }.FromJson("{\"Id\":\"Something\"}");
			var test10 = new[] { new { Id = .0 } }.FromJson("[{\"Id\":1}, {\"Id\":2}]");
			var test11 = new { ZipCode = 0 }.
				FromJson
				(
					" { ZipCode: 75015 } ",
					new ParserSettings { AcceptIdentifiers = true },
					Map.Value(default(double), default(int)).
						Using
						(
							(type, key, value) =>
								() => Convert.ToInt32(value)
						)
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
			System.Diagnostics.Debug.Assert((string)test8.JsonObject()["First"] == "John");
			System.Diagnostics.Debug.Assert((string)test8.JsonObject()["Last"] == "Smith");
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
				Map.Value(DATE_JSON, default(DateTime)).
					Using
					(
						(type, key, value) =>
							(type == typeof(DateTime)) ?
								(Func<DateTime>)
								(
									() =>
										(value != null) ?
											new DateTime(value.the_Year, value.the_Month, value.the_Day) :
											DateTime.Now
								) :
								null
					);

			DateTime dateTime = DATE_JSON.
				FromJson
				(
					default(DateTime),
					@" { ""the_Year"": 1970, ""the_Month"": 5, ""the_Day"": 10 }",
					Sample_Revivers.ToInteger,
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
			var person = (null as Person).
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

		public static void PersonAnonTest()
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

			var person = PERSON_JSON.
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

		public static void Top15Youtube2013Test()
		{
			Console.Clear();
			Console.WriteLine("Top 15 Youtube 2013 Test - JSON parse...");
			Console.WriteLine();
			System.Net.WebRequest www = System.Net.WebRequest.Create("https://gdata.youtube.com/feeds/api/videos?q=2013&max-results=15&v=2&alt=jsonc");
			using (System.IO.Stream stream = www.GetResponse().GetResponseStream())
			{
				// Yup, as simple as this, step #1:
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

				// And as easy as that, step #2:
				var parsed = YOUTUBE_JSON.
					FromJson
					(
						stream,
						Map.Value(default(string), default(string)).
							Using
							(
								// turn Youtube's JSON keys from lower camel case to Pascal case:
								(type, key, value) =>
									(key == typeof(string)) ?
										(Func<string>)
										(() => String.Concat((char)(value[0] - 32), value.Substring(1))) :
										null
							),
						Map.Value(default(string), default(DateTime)).
							Using
							(
								(type, key, value) =>
									(type == typeof(DateTime)) ?
										(Func<DateTime>)
										(() => (!String.IsNullOrEmpty(value) ? DateTime.Parse(value) : DateTime.Now)) :
										null
							)
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
		// avg: file size ~ exec time (on Lenovo Win7 PC, i5, 2.50GHz, 6Gb)
		// small.json.txt... avg: 4kb ~ 1ms
		// fathers.json.txt... avg: 12mb ~ 1sec
		// huge.json.txt... avg: 180mb ~ 20sec
		public static void SmallTest()
		{
			Console.Clear();
			string small = System.IO.File.ReadAllText(SMALL_TEST_FILE_PATH);
			Console.WriteLine("Small Test - JSON parse... {0} bytes ({1} kb)", small.Length, ((decimal)small.Length / (decimal)1024));
			Console.WriteLine();

			Console.WriteLine("\tParsed by {0} in...", typeof(Parser).FullName);
			DateTime start = DateTime.Now;
			var obj = (null as object).FromJson(small);
			Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start).TotalMilliseconds);
			Console.WriteLine();
			Console.Write("Press a key...");
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
			obj = (null as object).FromJson(json);
			Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start2).TotalMilliseconds);
			Console.WriteLine();
			Console.Write("Press a key...");
			Console.ReadKey();
#endif
		}

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
			Console.Write("Press a key...");
			Console.ReadKey();

			Console.WriteLine("\tParsed by {0} in...", typeof(Parser).FullName);
			DateTime start2 = DateTime.Now;
			var myObj = (null as object).FromJson(json);
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
			Console.WriteLine("The end... Press a key...");
			Console.ReadKey();
		}
	}
}