using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.Json;

namespace TestJSONParser
{
	class Program
	{
		static void Main(string[] args)
		{
			/*Hmm...
            Func<int> foo = () => 123;
            Func<object> up1 = JSON.UpCast(() => foo());
            object n = JSON.Call(typeof(int), up1);*/

			BasicTests.MostBasicTest();
			BasicTests.DateTimeTest();
			BasicTests.PersonTest();
			BasicTests.PersonAnonTest();
			BasicTests.Top15Youtube2013Test();
			BasicTests.SmallTest();
			BasicTests.HugeTest();
			BasicTests.FathersTest();
			OtherTests.RickStrahlsDynJSONTests();
		}
	}
}