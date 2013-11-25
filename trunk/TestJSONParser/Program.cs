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
			BasicTests.CastTests();
			BasicTests.MostBasicTest();
			BasicTests.DateTimeTest();
			BasicTests.PersonTest();
			BasicTests.PersonAnonymousTest();
			BasicTests.SO_19023696();
			BasicTests.SmallTest();
			BasicTests.Top15Youtube2013Test();
			BasicTests.FathersTest();
			BasicTests.FathersTestTyped();
			BasicTests.HugeTest();
			OtherTests.RickStrahlsDynJSONTests();
		}
	}
}