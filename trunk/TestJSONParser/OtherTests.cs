using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Newtonsoft.Json.Linq;
//using Newtonsoft.Json;

using System.Text.Json;

/*
 * See:
 * 
 * Using JSON.NET for dynamic JSON parsing
 * 
 * http://west-wind.com/weblog/posts/2012/Aug/30/Using-JSONNET-for-dynamic-JSON-parsing
 * 
 * by Rick Strahl, 8/30/2012
 */
namespace TestJSONParser
{
	public static class OtherTests
	{
		/*
		// (code omitted)
        
		public class Song
		{
			public string SongName { get; set; }
			public string SongLength { get; set; }
		}

		public class Album
		{
			public DateTime Entered { get; set; }
			public string AlbumName { get; set; }
			public string Artist { get; set; }
			public int YearReleased { get; set; }
			public List<Song> Songs { get; set; }
		}

		[TestMethod]
		public void JsonArrayParsingTest()
		{
			JArray jsonVal = JArray.Parse(jsonString) as JArray;
			dynamic albums = jsonVal;

			foreach (dynamic album in albums)
			{
				Console.WriteLine(album.AlbumName + " (" + album.YearReleased.ToString() + ")");
				foreach (dynamic song in album.Songs)
				{
					Console.WriteLine("\t" + song.SongName);
				}
			}

			Console.WriteLine(albums[0].AlbumName);
			Console.WriteLine(albums[0].Songs[1].SongName);
		}
		// (more code omitted) 
		*/

		public static void RickStrahlsDynJSONTests()
		{
			Console.Clear();
			Console.WriteLine("Rick Strahl's sample");
			Console.WriteLine();
			Console.WriteLine("( @ http://west-wind.com/weblog/posts/2012/Aug/30/Using-JSONNET-for-dynamic-JSON-parsing )");
			Console.WriteLine();

			var Album = new
			{
				Entered = default(DateTime),
				AlbumName = "",
				Artist = "",
				YearReleased = 0,

				// As we want an IList<> of songs (another anonymous type),
				// one of our extension methods on System.Type will do the trick:
				Songs = (null as Type).List(new
				{
					SongName = "",
					SongLength = ""
				})
			};

			// We use this "new[] { Album }" to (strongly) type-shape what we're interested in,
			// here, by a prototype array of our previous anonymous type (the one held @ Album)
			var albums = JSON.Map(new[] { Album }).
				FromJson
				(
					jsonString,
                    // Revivers, to map doubles into ints, and strings into DateTimes:
					JSON.Map(default(double)).
						Using
						(
							(outer, type, value) =>
                                ((outer.Key == null) && (type == typeof(int))) ? (Func<object>)
							        (() => Convert.ToInt32(value)) :
									null
						),
					JSON.Map(default(string)).
						Using
						(
							(outer, type, value) =>
                                ((outer.Key == null) && (type == typeof(DateTime))) ? (Func<object>)
									(() => DateTime.Parse(value)) :
									null
						)
				);

			Console.WriteLine("All albums, all songs:");
			Console.WriteLine();

            foreach (var album in albums)
			{
				Console.WriteLine("\t" + album.AlbumName + " (" + album.YearReleased.ToString() + ")");
				foreach (var song in album.Songs)
					Console.WriteLine("\t\t" + song.SongName);
			}

			Console.WriteLine();
			Console.WriteLine("First album, first song:");
			Console.WriteLine();
			Console.WriteLine("\t" + albums[0].AlbumName);
			Console.WriteLine();
			Console.WriteLine("\t\t" + albums[0].Songs[1].SongName);
			Console.WriteLine();
			Console.WriteLine("The end... Press a key...");
			Console.ReadKey();
		}

		static string jsonString = @"[
{
""Id"": ""b3ec4e5c"",
""AlbumName"": ""Dirty Deeds Done Dirt Cheap"",
""Artist"": ""AC/DC"",
""YearReleased"": 1976,
""Entered"": ""2012-03-16T00:13:12.2810521-10:00"",
""AlbumImageUrl"": ""http://ecx.images-amazon.com/images/I/61kTaH-uZBL._AA115_.jpg"",
""AmazonUrl"": ""http://www.amazon.com/gp/product/B00008BXJ4/ref=as_li_ss_tl?ie=UTF8&tag=westwindtechn-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=B00008BXJ4"",
""Songs"": [
    {
    ""AlbumId"": ""b3ec4e5c"",
    ""SongName"": ""Dirty Deeds Done Dirt Cheap"",
    ""SongLength"": ""4:11""
    },
    {
    ""AlbumId"": ""b3ec4e5c"",
    ""SongName"": ""Love at First Feel"",
    ""SongLength"": ""3:10""
    },
    {
    ""AlbumId"": ""b3ec4e5c"",
    ""SongName"": ""Big Balls"",
    ""SongLength"": ""2:38""
    }
]
},
{
""Id"": ""67280fb8"",
""AlbumName"": ""Echoes, Silence, Patience & Grace"",
""Artist"": ""Foo Fighters"",
""YearReleased"": 2007,
""Entered"": ""2012-03-16T00:13:12.2810521-10:00"",
""AlbumImageUrl"": ""http://ecx.images-amazon.com/images/I/41mtlesQPVL._SL500_AA280_.jpg"",
""AmazonUrl"": ""http://www.amazon.com/gp/product/B000UFAURI/ref=as_li_ss_tl?ie=UTF8&tag=westwindtechn-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=B000UFAURI"",
""Songs"": [
    {
    ""AlbumId"": ""67280fb8"",
    ""SongName"": ""The Pretender"",
    ""SongLength"": ""4:29""
    },
    {
    ""AlbumId"": ""67280fb8"",
    ""SongName"": ""Let it Die"",
    ""SongLength"": ""4:05""
    },
    {
    ""AlbumId"": ""67280fb8"",
    ""SongName"": ""Erase/Replay"",
    ""SongLength"": ""4:13""
    }
]
},
{
""Id"": ""7b919432"",
""AlbumName"": ""End of the Silence"",
""Artist"": ""Henry Rollins Band"",
""YearReleased"": 1992,
""Entered"": ""2012-03-16T00:13:12.2800521-10:00"",
""AlbumImageUrl"": ""http://ecx.images-amazon.com/images/I/51FO3rb1tuL._SL160_AA160_.jpg"",
""AmazonUrl"": ""http://www.amazon.com/End-Silence-Rollins-Band/dp/B0000040OX/ref=sr_1_5?ie=UTF8&qid=1302232195&sr=8-5"",
""Songs"": [
    {
    ""AlbumId"": ""7b919432"",
    ""SongName"": ""Low Self Opinion"",
    ""SongLength"": ""5:24""
    },
    {
    ""AlbumId"": ""7b919432"",
    ""SongName"": ""Grip"",
    ""SongLength"": ""4:51""
    }
]
}
]";
	}
}
