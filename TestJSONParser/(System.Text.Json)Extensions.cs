/*
 * Copyright (c) 2013 Cyril Jandia
 *
 * http://www.cjandia.com/
 *
Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
``Software''), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ``AS IS'', WITHOUT WARRANTY OF ANY KIND, EXPRESS
OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL CYRIL JANDIA BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of Cyril Jandia shall
not be used in advertising or otherwise to promote the sale, use or
other dealings in this Software without prior written authorization
from Cyril Jandia.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text.Json
{
	public static class Extensions
	{
		public static T As<T>(this object obj)
		{
			return (T)obj;
		}

		public static T As<T>(this object obj, T prototype)
		{
			return (T)obj;
		}

		public static T FromJson<T>(this T prototype, string text)
		{
			return (T)new Parser().Parse(text, prototype);
		}

		public static R FromJson<T, R>(this T prototype, R value, string text)
		{
			return (R)new Parser().Parse(text, prototype);
		}

		public static T FromJson<T>(this T prototype, string text, ParserSettings settings)
		{
			return (T)new Parser().Parse(text, settings, prototype);
		}

		public static R FromJson<T, R>(this T prototype, R value, string text, ParserSettings settings)
		{
			return (R)new Parser().Parse(text, settings, prototype);
		}

		public static T FromJson<T>(this T prototype, string text, params Reviver[] revivers)
		{
			return (T)new Parser().Parse(text, prototype, revivers);
		}

		public static R FromJson<T, R>(this T prototype, R value, string text, params Reviver[] revivers)
		{
			return (R)new Parser().Parse(text, prototype, revivers);
		}

		public static T FromJson<T>(this T prototype, string text, ParserSettings settings, params Reviver[] revivers)
		{
			return (T)new Parser().Parse(text, settings, prototype, revivers);
		}

		public static R FromJson<T, R>(this T prototype, R value, string text, ParserSettings settings, params Reviver[] revivers)
		{
			return (R)new Parser().Parse(text, settings, prototype, revivers);
		}

		public static T FromJson<T>(this T prototype, System.IO.Stream stream)
		{
			return (T)new Parser().Parse(stream, prototype);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.Stream stream)
		{
			return (R)new Parser().Parse(stream, prototype);
		}

		public static T FromJson<T>(this T prototype, System.IO.Stream stream, ParserSettings settings)
		{
			return (T)new Parser().Parse(stream, settings, prototype);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.Stream stream, ParserSettings settings)
		{
			return (R)new Parser().Parse(stream, settings, prototype);
		}

		public static T FromJson<T>(this T prototype, System.IO.Stream stream, params Reviver[] revivers)
		{
			return (T)new Parser().Parse(stream, prototype, revivers);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.Stream stream, params Reviver[] revivers)
		{
			return (R)new Parser().Parse(stream, prototype, revivers);
		}

		public static T FromJson<T>(this T prototype, System.IO.Stream stream, ParserSettings settings, params Reviver[] revivers)
		{
			return (T)new Parser().Parse(stream, settings, prototype, revivers);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.Stream stream, ParserSettings settings, params Reviver[] revivers)
		{
			return (R)new Parser().Parse(stream, settings, prototype, revivers);
		}

		public static T FromJson<T>(this T prototype, System.IO.StreamReader reader)
		{
			return (T)new Parser().Parse(reader, prototype);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.StreamReader reader)
		{
			return (R)new Parser().Parse(reader, prototype);
		}

		public static T FromJson<T>(this T prototype, System.IO.StreamReader reader, ParserSettings settings)
		{
			return (T)new Parser().Parse(reader, settings, prototype);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.StreamReader reader, ParserSettings settings)
		{
			return (R)new Parser().Parse(reader, settings, prototype);
		}

		public static T FromJson<T>(this T prototype, System.IO.StreamReader reader, params Reviver[] revivers)
		{
			return (T)new Parser().Parse(reader, prototype, revivers);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.StreamReader reader, params Reviver[] revivers)
		{
			return (R)new Parser().Parse(reader, prototype, revivers);
		}

		public static T FromJson<T>(this T prototype, System.IO.StreamReader reader, ParserSettings settings, params Reviver[] revivers)
		{
			return (T)new Parser().Parse(reader, settings, prototype, revivers);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.StreamReader reader, ParserSettings settings, params Reviver[] revivers)
		{
			return (R)new Parser().Parse(reader, settings, prototype, revivers);
		}

		public static IDictionary<string, object> JsonObject(this object obj)
		{
			return (IDictionary<string, object>)obj;
		}

		public static IList<object> JsonArray(this object obj)
		{
			return (IList<object>)obj;
		}
	}
}