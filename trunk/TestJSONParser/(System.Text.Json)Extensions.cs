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
	public class Value
	{
		public static Value<T> Map<T>(T value)
		{
			return default(Value<T>);
		}

		public static Value<T, R> Map<T, R>(T from, R to)
		{
			return default(Value<T, R>);
		}
	}

	public class Value<T> { }

	public class Value<T, R> { }

	public static class Extensions
	{
		public static IList<T> IList<T>(this Type anchor, T prototype)
		{
			return List<T>(anchor, prototype);
		}

		public static List<T> List<T>(this Type anchor, T prototype)
		{
			return new List<T>();
		}

		public static IDictionary<K, V> IDictionary<K, V>(this Type anchor, K prototypeKey, V prototypeValue)
		{
			return Dictionary<K, V>(anchor, prototypeKey, prototypeValue);
		}

		public static Dictionary<K, V> Dictionary<K, V>(this Type anchor, K prototypeKey, V prototypeValue)
		{
			return new Dictionary<K, V>();
		}

		public static T As<T>(this object obj)
		{
			return (T)obj;
		}

		public static T As<T>(this object obj, T prototype)
		{
			return (T)obj;
		}

		public static Reviver<T, R> Using<T, R>(this Value<T, R> result, Reviver<T, R> reviver)
		{
			return reviver;
		}

		public static T FromJson<T>(this Value<T> prototype, string text)
		{
			return new Parser().Parse<T, T>(text, default(T));
		}

		public static R FromJson<T, R>(this Value<T, R> prototype, R value, string text)
		{
			return new Parser().Parse<T, R>(text, default(T));
		}

		public static T FromJson<T>(this Value<T> prototype, string text, ParserSettings settings)
		{
			return new Parser().Parse<T, T>(text, settings, default(T));
		}

		public static R FromJson<T, R>(this Value<T, R> prototype, R value, string text, ParserSettings settings)
		{
			return new Parser().Parse<T, R>(text, settings, default(T));
		}

		public static T FromJson<T>(this Value<T> prototype, string text, params Delegate[] revivers)
		{
			return new Parser().Parse<T, T>(text, default(T), revivers);
		}

		public static R FromJson<T, R>(this Value<T, R> prototype, R value, string text, params Delegate[] revivers)
		{
			return new Parser().Parse<T, R>(text, default(T), revivers);
		}

		public static T FromJson<T>(this Value<T> prototype, string text, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<T, T>(text, settings, default(T), revivers);
		}

		public static R FromJson<T, R>(this Value<T, R> prototype, R value, string text, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<T, R>(text, settings, default(T), revivers);
		}

		public static T FromJson<T>(this Value<T> prototype, System.IO.Stream stream)
		{
			return new Parser().Parse<T, T>(stream, default(T));
		}

		public static R FromJson<T, R>(this Value<T, R> prototype, R value, System.IO.Stream stream)
		{
			return new Parser().Parse<T, R>(stream, default(T));
		}

		public static T FromJson<T>(this Value<T> prototype, System.IO.Stream stream, ParserSettings settings)
		{
			return new Parser().Parse<T, T>(stream, settings, default(T));
		}

		public static R FromJson<T, R>(this Value<T, R> prototype, R value, System.IO.Stream stream, ParserSettings settings)
		{
			return new Parser().Parse<T, R>(stream, settings, default(T));
		}

		public static T FromJson<T>(this Value<T> prototype, System.IO.Stream stream, params Delegate[] revivers)
		{
			return new Parser().Parse<T, T>(stream, default(T), revivers);
		}

		public static R FromJson<T, R>(this Value<T, R> prototype, R value, System.IO.Stream stream, params Delegate[] revivers)
		{
			return new Parser().Parse<T, R>(stream, default(T), revivers);
		}

		public static T FromJson<T>(this Value<T> prototype, System.IO.Stream stream, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<T, T>(stream, settings, default(T), revivers);
		}

		public static R FromJson<T, R>(this Value<T, R> prototype, R value, System.IO.Stream stream, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<T, R>(stream, settings, default(T), revivers);
		}

		public static T FromJson<T>(this Value<T> prototype, System.IO.StreamReader reader)
		{
			return new Parser().Parse<T, T>(reader, default(T));
		}

		public static R FromJson<T, R>(this Value<T, R> prototype, R value, System.IO.StreamReader reader)
		{
			return new Parser().Parse<T, R>(reader, default(T));
		}

		public static T FromJson<T>(this Value<T> prototype, System.IO.StreamReader reader, ParserSettings settings)
		{
			return new Parser().Parse<T, T>(reader, settings, default(T));
		}

		public static R FromJson<T, R>(this Value<T, R> prototype, R value, System.IO.StreamReader reader, ParserSettings settings)
		{
			return new Parser().Parse<T, R>(reader, settings, default(T));
		}

		public static T FromJson<T>(this Value<T> prototype, System.IO.StreamReader reader, params Delegate[] revivers)
		{
			return new Parser().Parse<T, T>(reader, default(T), revivers);
		}

		public static R FromJson<T, R>(this Value<T, R> prototype, R value, System.IO.StreamReader reader, params Delegate[] revivers)
		{
			return new Parser().Parse<T, R>(reader, default(T), revivers);
		}

		public static T FromJson<T>(this Value<T> prototype, System.IO.StreamReader reader, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<T, T>(reader, settings, default(T), revivers);
		}

		public static R FromJson<T, R>(this Value<T, R> prototype, R value, System.IO.StreamReader reader, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<T, R>(reader, settings, default(T), revivers);
		}

		public static IDictionary<string, object> Object(this object obj)
		{
			return (IDictionary<string, object>)obj;
		}

		public static IList<object> Array(this object obj)
		{
			return (IList<object>)obj;
		}
	}
}