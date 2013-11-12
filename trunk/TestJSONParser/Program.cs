using System;
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
		private const string TEST_FILE_PATH = @"..\..\fathers.json.txt";

		static void Main(string[] args)
		{
			string json = System.IO.File.ReadAllText(TEST_FILE_PATH);
			Console.WriteLine("JSON parse... {0} kb ({1} mb)", (int)(json.Length / 1024), (int)(json.Length / (1024 * 1024)));
			Console.WriteLine();

			JavaScriptSerializer serializer = new JavaScriptSerializer
			{
				MaxJsonLength = int.MaxValue
			};
			Console.WriteLine("Parsed by {0} in...", serializer.GetType().FullName);
			DateTime st = DateTime.Now;
			var msObj = serializer.Deserialize<object>(json);
			Console.WriteLine("... {0} ms", (int)DateTime.Now.Subtract(st).TotalMilliseconds);
			Console.WriteLine();

			Console.WriteLine("Parsed by {0} in...", typeof(NetUtils.Helpers.JSONParser).FullName);
			DateTime start = DateTime.Now;
			object myObj = NetUtils.Helpers.JSONParser.Parse(json);
			Console.WriteLine();
			Console.WriteLine("... {0} ms", (int)DateTime.Now.Subtract(start).TotalMilliseconds);
			Console.WriteLine();

			Console.WriteLine("Press '1' to inspect our result object,\r\nany other key to inspect Microsoft's JS serializer result object...");
			// Works also: var parsed = msObj; for the object returned by Microsoft's JS serializer
			var parsed = ((Console.ReadKey().KeyChar == '1') ? myObj : msObj);

			object[] items = (object[])((IDictionary<string, object>)parsed)["fathers"];
			Console.WriteLine();
			Console.WriteLine("Found : {0} fathers", items.Length);
			Console.WriteLine("Press a key to list them...");
			Console.WriteLine();
			Console.ReadKey();
			foreach (object item in items)
			{
				var father = (IDictionary<string, object>)item;
				var name = (string)father["name"];
				var sons = (object[])father["sons"];
				var daughters = (object[])father["daughters"];
				Console.WriteLine("{0} : {1} son(s), and {2} daughter(s)", name, sons.Length, daughters.Length);
			}
			Console.WriteLine();
			Console.WriteLine("The end. Press a key...");

			Console.ReadKey();
		}
	}
}
