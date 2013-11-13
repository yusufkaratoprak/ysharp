using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace TestJSONParser
{
	class Program
	{
		// Note: fathers.json.txt was generated using:
		// http://experiments.mennovanslooten.nl/2010/mockjson/tryit.html
		private const string FATHERS_TEST_FILE_PATH = @"..\..\fathers.json.txt";
		private const string SMALL_TEST_FILE_PATH = @"..\..\small.json.txt";

		static void Main(string[] args)
		{
			string small = System.IO.File.ReadAllText(SMALL_TEST_FILE_PATH);
			Console.WriteLine("Small Test - JSON parse... {0} bytes ({1} kb)", small.Length, ((decimal)small.Length / (decimal)1024));
			Console.WriteLine();

			var parser = new JSONParser();
			Console.WriteLine("\tParsed by {0} in...", parser.GetType().FullName);
			DateTime start = DateTime.Now;
			object obj = parser.Parse(small);
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
			var msObj = serializer.Deserialize<object>(json);
			Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start1).TotalMilliseconds);
			Console.WriteLine();

			var jsonParser = new JSONParser();
			Console.WriteLine("\tParsed by {0} in...", jsonParser.GetType().FullName);
			DateTime start2 = DateTime.Now;
			object myObj = jsonParser.Parse(json);
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
			foreach (object item in items)
			{
				var father = (IDictionary<string, object>)item;
				var name = (string)father["name"];
				var sons = (IList<object>)father["sons"];
				var daughters = (IList<object>)father["daughters"];
				Console.WriteLine("{0} : {1} son(s), and {2} daughter(s)", name, sons.Count, daughters.Count);
			}
			Console.WriteLine();
			Console.WriteLine("The end... Press a key...");

			Console.ReadKey();
		}
	}
}
