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
	public struct Map
	{
		public static Map<T, R> Value<T, R>(T from, R to)
		{
			return default(Map<T, R>);
		}
	}

	public struct Map<T, R>
	{
	}

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

		public static Reviver<T, R> Using<T, R>(this Map<T, R> result, Reviver<T, R> reviver)
		{
			return reviver;
		}

		public static T FromJson<T>(this T prototype, string text)
		{
			return new Parser().Parse<T, T>(text, prototype);
		}

		public static R FromJson<T, R>(this T prototype, R value, string text)
		{
			return new Parser().Parse<T, R>(text, prototype);
		}

		public static T FromJson<T>(this T prototype, string text, ParserSettings settings)
		{
			return new Parser().Parse<T, T>(text, settings, prototype);
		}

		public static R FromJson<T, R>(this T prototype, R value, string text, ParserSettings settings)
		{
			return new Parser().Parse<T, R>(text, settings, prototype);
		}

		public static T FromJson<T>(this T prototype, string text, params Delegate[] revivers)
		{
			return new Parser().Parse<T, T>(text, prototype, revivers);
		}

		public static R FromJson<T, R>(this T prototype, R value, string text, params Delegate[] revivers)
		{
			return new Parser().Parse<T, R>(text, prototype, revivers);
		}

		public static T FromJson<T>(this T prototype, string text, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<T, T>(text, settings, prototype, revivers);
		}

		public static R FromJson<T, R>(this T prototype, R value, string text, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<T, R>(text, settings, prototype, revivers);
		}

		public static T FromJson<T>(this T prototype, System.IO.Stream stream)
		{
			return new Parser().Parse<T, T>(stream, prototype);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.Stream stream)
		{
			return new Parser().Parse<T, R>(stream, prototype);
		}

		public static T FromJson<T>(this T prototype, System.IO.Stream stream, ParserSettings settings)
		{
			return new Parser().Parse<T, T>(stream, settings, prototype);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.Stream stream, ParserSettings settings)
		{
			return new Parser().Parse<T, R>(stream, settings, prototype);
		}

		public static T FromJson<T>(this T prototype, System.IO.Stream stream, params Delegate[] revivers)
		{
			return new Parser().Parse<T, T>(stream, prototype, revivers);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.Stream stream, params Delegate[] revivers)
		{
			return new Parser().Parse<T, R>(stream, prototype, revivers);
		}

		public static T FromJson<T>(this T prototype, System.IO.Stream stream, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<T, T>(stream, settings, prototype, revivers);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.Stream stream, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<T, R>(stream, settings, prototype, revivers);
		}

		public static T FromJson<T>(this T prototype, System.IO.StreamReader reader)
		{
			return new Parser().Parse<T, T>(reader, prototype);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.StreamReader reader)
		{
			return new Parser().Parse<T, R>(reader, prototype);
		}

		public static T FromJson<T>(this T prototype, System.IO.StreamReader reader, ParserSettings settings)
		{
			return new Parser().Parse<T, T>(reader, settings, prototype);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.StreamReader reader, ParserSettings settings)
		{
			return new Parser().Parse<T, R>(reader, settings, prototype);
		}

		public static T FromJson<T>(this T prototype, System.IO.StreamReader reader, params Delegate[] revivers)
		{
			return new Parser().Parse<T, T>(reader, prototype, revivers);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.StreamReader reader, params Delegate[] revivers)
		{
			return new Parser().Parse<T, R>(reader, prototype, revivers);
		}

		public static T FromJson<T>(this T prototype, System.IO.StreamReader reader, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<T, T>(reader, settings, prototype, revivers);
		}

		public static R FromJson<T, R>(this T prototype, R value, System.IO.StreamReader reader, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<T, R>(reader, settings, prototype, revivers);
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