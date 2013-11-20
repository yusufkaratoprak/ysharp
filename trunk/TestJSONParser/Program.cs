using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestJSONParser
{
	class Program
	{
		static void Main(string[] args)
		{
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