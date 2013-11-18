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
            BasicTests.DateTimeTest();
            BasicTests.PersonTest();
            BasicTests.PersonAnonTest();
            BasicTests.Top10Youtube2013Test();
            BasicTests.SmallTest();
            BasicTests.HugeTest();
            BasicTests.FathersTest();
        }
    }
}