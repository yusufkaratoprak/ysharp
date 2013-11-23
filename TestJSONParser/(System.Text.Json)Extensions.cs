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

Inquiries : ysharp {dot} design {at} gmail {dot} com
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text.Json
{
    public partial struct JSON
	{
		public static JSON<TValue> Map<TValue>(TValue value)
		{
			return default(JSON<TValue>);
		}

		public static JSON<TValue, TResult> Map<TValue, TResult>(TValue from, TResult to)
		{
			return default(JSON<TValue, TResult>);
		}
	}

    public struct JSON<TValue> { }

    public struct JSON<TValue, TResult> { }

	public static class Extensions
	{
		public static IList<TValue> IList<TValue>(this Type self, TValue prototype)
		{
            return List<TValue>(self, prototype);
		}

        public static List<TValue> List<TValue>(this Type self, TValue prototype)
		{
			return new List<TValue>();
		}

        public static IDictionary<TKey, TValue> IDictionary<TKey, TValue>(this Type self, TKey protoKey, TValue protoValue)
		{
            return Dictionary<TKey, TValue>(self, protoKey, protoValue);
		}

        public static Dictionary<TKey, TValue> Dictionary<TKey, TValue>(this Type self, TKey protoKey, TValue protoValue)
		{
            return new Dictionary<TKey, TValue>();
		}

		public static T As<T>(this object obj)
		{
			return (T)obj;
		}

		public static T As<T>(this object obj, T prototype)
		{
			return (T)obj;
		}

		public static Reviver<TValue, TValue> Using<TValue>(this JSON<TValue> self, Reviver<TValue, TValue> reviver)
		{
			return reviver;
		}

		public static Reviver<TValue, TResult> Using<TValue, TResult>(this JSON<TValue, TResult> self, Reviver<TValue, TResult> reviver)
		{
			return reviver;
		}

		public static TValue FromJson<TValue>(this JSON<TValue> self, string text)
		{
			return new Parser().Parse<TValue, TValue>(text, default(TValue));
		}

		public static TResult FromJson<TValue, TResult>(this JSON<TValue, TResult> self, TResult prototype, string text)
		{
			return new Parser().Parse<TValue, TResult>(text, default(TValue));
		}

		public static TValue FromJson<TValue>(this JSON<TValue> self, string text, ParserSettings settings)
		{
			return new Parser().Parse<TValue, TValue>(text, settings, default(TValue));
		}

		public static TResult FromJson<TValue, TResult>(this JSON<TValue, TResult> self, TResult prototype, string text, ParserSettings settings)
		{
			return new Parser().Parse<TValue, TResult>(text, settings, default(TValue));
		}

		public static TValue FromJson<TValue>(this JSON<TValue> self, string text, params Delegate[] revivers)
		{
			return new Parser().Parse<TValue, TValue>(text, default(TValue), revivers);
		}

		public static TResult FromJson<TValue, TResult>(this JSON<TValue, TResult> self, TResult prototype, string text, params Delegate[] revivers)
		{
			return new Parser().Parse<TValue, TResult>(text, default(TValue), revivers);
		}

		public static TValue FromJson<TValue>(this JSON<TValue> self, string text, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<TValue, TValue>(text, settings, default(TValue), revivers);
		}

		public static TResult FromJson<TValue, TResult>(this JSON<TValue, TResult> self, TResult prototype, string text, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<TValue, TResult>(text, settings, default(TValue), revivers);
		}

		public static TValue FromJson<TValue>(this JSON<TValue> self, System.IO.Stream stream)
		{
			return new Parser().Parse<TValue, TValue>(stream, default(TValue));
		}

		public static TResult FromJson<TValue, TResult>(this JSON<TValue, TResult> self, TResult prototype, System.IO.Stream stream)
		{
			return new Parser().Parse<TValue, TResult>(stream, default(TValue));
		}

		public static TValue FromJson<TValue>(this JSON<TValue> self, System.IO.Stream stream, ParserSettings settings)
		{
			return new Parser().Parse<TValue, TValue>(stream, settings, default(TValue));
		}

		public static TResult FromJson<TValue, TResult>(this JSON<TValue, TResult> self, TResult prototype, System.IO.Stream stream, ParserSettings settings)
		{
			return new Parser().Parse<TValue, TResult>(stream, settings, default(TValue));
		}

		public static TValue FromJson<TValue>(this JSON<TValue> self, System.IO.Stream stream, params Delegate[] revivers)
		{
			return new Parser().Parse<TValue, TValue>(stream, default(TValue), revivers);
		}

		public static TResult FromJson<TValue, TResult>(this JSON<TValue, TResult> self, TResult prototype, System.IO.Stream stream, params Delegate[] revivers)
		{
			return new Parser().Parse<TValue, TResult>(stream, default(TValue), revivers);
		}

		public static TValue FromJson<TValue>(this JSON<TValue> self, System.IO.Stream stream, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<TValue, TValue>(stream, settings, default(TValue), revivers);
		}

		public static TResult FromJson<TValue, TResult>(this JSON<TValue, TResult> self, TResult prototype, System.IO.Stream stream, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<TValue, TResult>(stream, settings, default(TValue), revivers);
		}

		public static TValue FromJson<TValue>(this JSON<TValue> self, System.IO.StreamReader reader)
		{
			return new Parser().Parse<TValue, TValue>(reader, default(TValue));
		}

		public static TResult FromJson<TValue, TResult>(this JSON<TValue, TResult> self, TResult prototype, System.IO.StreamReader reader)
		{
			return new Parser().Parse<TValue, TResult>(reader, default(TValue));
		}

		public static TValue FromJson<TValue>(this JSON<TValue> self, System.IO.StreamReader reader, ParserSettings settings)
		{
			return new Parser().Parse<TValue, TValue>(reader, settings, default(TValue));
		}

		public static TResult FromJson<TValue, TResult>(this JSON<TValue, TResult> self, TResult prototype, System.IO.StreamReader reader, ParserSettings settings)
		{
			return new Parser().Parse<TValue, TResult>(reader, settings, default(TValue));
		}

		public static TValue FromJson<TValue>(this JSON<TValue> self, System.IO.StreamReader reader, params Delegate[] revivers)
		{
			return new Parser().Parse<TValue, TValue>(reader, default(TValue), revivers);
		}

		public static TResult FromJson<TValue, TResult>(this JSON<TValue, TResult> self, TResult prototype, System.IO.StreamReader reader, params Delegate[] revivers)
		{
			return new Parser().Parse<TValue, TResult>(reader, default(TValue), revivers);
		}

		public static TValue FromJson<TValue>(this JSON<TValue> self, System.IO.StreamReader reader, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<TValue, TValue>(reader, settings, default(TValue), revivers);
		}

		public static TResult FromJson<TValue, TResult>(this JSON<TValue, TResult> self, TResult prototype, System.IO.StreamReader reader, ParserSettings settings, params Delegate[] revivers)
		{
			return new Parser().Parse<TValue, TResult>(reader, settings, default(TValue), revivers);
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