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
	public delegate Func<TResult> Reviver<TValue, TResult>(Outer outer, TValue value);

    public struct Outer
    {
        public readonly Type Type; public readonly object Key;
        public Outer(Type type, object key) { Type = type; Key = key; }
    }

	public class ParserSettings
	{
		public bool AcceptIdentifiers { get; set; }
		public int LiteralBufferSize { get; set; }
	}

	public class Parser
	{
		private const char NEXT = (char)0;
		private const int LSIZE = 4096;

        internal class Cache<TKey, TValue> : Dictionary<TKey, TValue>
        {
            internal bool Get(TKey key, out TValue value)
            {
                bool found;
                value = ((found = ContainsKey(key)) ? this[key] : default(TValue));
                return found;
            }

            internal TValue Set(TKey key, TValue value)
            {
                if (!ContainsKey(key))
                    Add(key, value);
                return value;
            }
        }

		internal class Phrase
		{
			private static readonly IDictionary<char, char> ESC = new Dictionary<char, char>();

            private Cache<Type, Cache<string, object>> rtti;
            private Delegate[] revs;
            private ParserSettings cfg;
			private System.IO.StreamReader str;
			private string txt;
			private bool ids;
			private int lsz;
			private int len;
			private StringBuilder sb;
			private char[] cs;
			private char[] wc;
			private bool data;
			private char ch;
			private int ci;
			private int at;
			private Func<char, bool> read;

			static Phrase()
			{
				ESC['"'] = '"';
				ESC['\\'] = '\\';
				ESC['/'] = '/';
				ESC['b'] = '\b';
				ESC['f'] = '\f';
				ESC['n'] = '\n';
				ESC['r'] = '\r';
				ESC['t'] = '\t';
			}

			internal Phrase(ParserSettings settings, object input, Delegate[] revivers)
			{
                rtti = new Cache<Type, Cache<string, object>>();
                revs = (Delegate[])(revivers ?? new Delegate[] { }).Clone();
				cfg = (settings ?? DefaultSettings);
				ids = cfg.AcceptIdentifiers;
				lsz = (((lsz = cfg.LiteralBufferSize) > 0) ? lsz : LSIZE);

				str = (input as System.IO.StreamReader);
				txt = (input as string);
				len = (txt ?? String.Empty).Length;
				cs = new char[lsz];
				wc = new char[1];
				data = true;
				ch = ' ';
				read = ((str != null) ? (Func<char, bool>)ReadFromStream : (Func<char, bool>)ReadFromString);
			}

			private Exception Error(string message)
			{
				return new Exception(String.Format("{0} at offset {1}", message, at));
			}

			private bool ReadFromStream(char c)
			{
				int r;
				if ((c != NEXT) && (c != ch))
					throw Error(String.Format("Expected '{0}' instead of '{1}'", c, ch));
				at += (r = str.Read(wc, 0, 1));
				data = (r > 0);
				ch = (data ? wc[0] : ch);
				return true;
			}

			private bool ReadFromString(char c)
			{
				if ((c != NEXT) && (c != ch))
					throw Error(String.Format("Expected '{0}' instead of '{1}'", c, ch));
				if (at < len)
					ch = txt[at++];
				data = (at < len);
				return true;
			}

			private void Append(char c)
			{
				if (ci < lsz)
					cs[ci++] = c;
				else
					if (sb != null)
						sb.Append(c);
					else
						sb = new StringBuilder(new String(cs, 0, ci)).Append(c);
			}

			private Cache<string, object> Known(Type type)
			{
                Cache<string, object> data;
				return (!rtti.Get(type, out data) ? rtti.Set(type, new Cache<string, object>()) : data);
			}

			private object Typed(object obj, Cache<string, object> hash, string key)
			{
                object item;
				if (!hash.Get(key, out item))
				{
					if (obj is Type)
					{
						var p = ((Type)obj).GetProperty(key);
						return (((p != null) && p.CanWrite) ? hash.Set(key, p) : null);
					}
					else
					{
						var a = (System.Reflection.ParameterInfo[])obj;
						int i = a.Length;
						while (--i >= 0)
							if (a[i].Name == key)
								break;
						return ((i >= 0) ? hash.Set(key, i) : null);
					}
				}
				else
					return item;
			}

			private Delegate Map<TValue>(Outer outer, TValue value)
			{
				return Map(outer, typeof(TValue), value);
			}

			private Delegate Map(Outer outer, Type value, object obj)
			{
				Delegate mapper = null;
				if
				(
					(revs.Length > 0) &&
					(
						(
							(obj != null) && value.IsAssignableFrom(obj.GetType())
						) ||
						(
							(obj == null) && (value.IsClass || value.IsInterface) && !value.IsGenericTypeDefinition
						)
					)
				)
					for (int i = 0; i < revs.Length; i++)
					{
						var candidate = revs[i];
						if
						(
							(candidate == null) ||
							!candidate.GetType().IsGenericType ||
							(candidate.GetType().GetGenericTypeDefinition() != typeof(Reviver<,>)) ||
							!candidate.GetType().GetGenericArguments()[0].IsAssignableFrom(value)
						)
							continue;
						if ((mapper = (candidate.DynamicInvoke(outer, obj) as Delegate)) != null)
							break;
					}
				return mapper;
			}

            private Outer Outer(Type type, object key)
            {
                return new Outer(type, key);
            }

			private object Word(Type type)
			{
				Delegate mapped;
				switch (ch)
				{
					case 't':
						if (data) read('t');
						if (data) read('r');
						if (data) read('u');
						if (data) read('e');
						mapped = Map(Outer(type, null), true);
						return ((mapped != null) ? mapped.DynamicInvoke() : true);
					case 'f':
						if (data) read('f');
						if (data) read('a');
						if (data) read('l');
						if (data) read('s');
						if (data) read('e');
						mapped = Map(Outer(type, null), false);
						return ((mapped != null) ? mapped.DynamicInvoke() : false);
					case 'n':
						if (data) read('n');
						if (data) read('u');
						if (data) read('l');
						if (data) read('l');
						mapped = Map(Outer(type, null), null as object);
						return ((mapped != null) ? mapped.DynamicInvoke() : null);
				}
				throw Error(String.Format("Unexpected '{0}'", ch));
			}

			private object Number(Type type)
			{
				double n;
				sb = null;
				ci = 0;
				if (ch == '-')
				{
					Append('-');
					if (data) read('-');
				}
				while ((ch >= '0') && (ch <= '9'))
				{
					Append(ch);
					if (data) read(NEXT); else break;
				}
				if (ch == '.')
				{
					Append('.');
					while ((data ? read(NEXT) : false) && (ch >= '0') && (ch <= '9'))
						Append(ch);
				}
				if ((ch == 'e') || (ch == 'E'))
				{
					Append(ch);
					if (data) read(NEXT);
					if ((ch == '-') || (ch == '+'))
					{
						Append(ch);
						if (data) read(NEXT);
					}
					while ((ch >= '0') && (ch <= '9'))
					{
						Append(ch);
						if (data) read(NEXT);
					}
				}
				n = double.Parse((sb != null) ? sb.ToString() : new String(cs, 0, ci));
				var mapped = Map(Outer(type, null), n);
				return ((mapped != null) ? mapped.DynamicInvoke() : n);
			}

			private object Literal(Type type, object key)
			{
				int hex, i, uffff;
				string s;
				sb = null;
				ci = 0;
				if (ch == '"')
				{
					while (data ? read(NEXT) : false)
					{
						if (ch == '"')
						{
							if (data) read(NEXT);
							s = ((sb != null) ? sb.ToString() : new String(cs, 0, ci));
							var mapped = Map(Outer(type, key), s);
							return ((mapped != null) ? mapped.DynamicInvoke() : s);
						}
						if (ch == '\\')
						{
							if (data) read(NEXT);
							if (ch == 'u')
							{
								uffff = 0;
								for (i = 0; i < 4; i += 1)
								{
									if (data) read(NEXT);
									hex = Convert.ToInt32(String.Empty + ch, 16);
									uffff = uffff * 16 + hex;
								}
								Append(Convert.ToChar(uffff));
							}
							else
							{
								bool stop;
								switch (ch)
								{
									case '"':
										stop = false;
										break;
									case '\\':
										stop = false;
										break;
									case '/':
										stop = false;
										break;
									case 'b':
										stop = false;
										break;
									case 'f':
										stop = false;
										break;
									case 'n':
										stop = false;
										break;
									case 'r':
										stop = false;
										break;
									case 't':
										stop = false;
										break;
									default:
										stop = true;
										break;
								}
								if (!stop)
									Append(ESC[ch]);
								else
									break;
							}
						}
						else
							Append(ch);
					}
				}
				else
				{
					if ((key != null) && ids)
					{
						if ((ch == '$') || ((ch >= 'A') && (ch <= 'Z')) || (ch == '_') || ((ch >= 'a') && (ch <= 'z')))
							Append(ch);
						else
							throw Error("Bad identifier");
						while (data ? read(NEXT) : false)
							if ((ch == '$') || ((ch >= '0') && (ch <= '9')) || ((ch >= 'A') && (ch <= 'Z')) || (ch == '_') || ((ch >= 'a') && (ch <= 'z')))
								Append(ch);
							else if ((ch > ' ') && (ch != ':'))
								throw Error("Bad identifier");
							else
							{
								s = ((sb != null) ? sb.ToString() : new String(cs, 0, ci));
								var mapped = Map(Outer(type, key), s);
								return ((mapped != null) ? mapped.DynamicInvoke() : s);
							}
					}
				}
				throw Error("Bad string");
			}

			private bool Realizes(Type type, Type generic)
			{
				var itfs = type.GetInterfaces();
				foreach (var it in itfs)
					if (it.IsGenericType && it.GetGenericTypeDefinition() == generic)
						return true;
				if (type.IsGenericType && type.GetGenericTypeDefinition() == generic)
					return true;
				if (type.BaseType == null)
					return false;
				return Realizes(type.BaseType, generic);
			}

			private object Object(Type type)
			{
				type = (type ?? typeof(object));
				bool isd = typeof(System.Collections.IDictionary).IsAssignableFrom(type);
				bool ish = (isd || (type.IsGenericType && Realizes(type, typeof(IDictionary<,>))));
				Type dkt = null, dvt = null;
				if (ish && !isd)
				{
					dkt = type.GetGenericArguments()[0];
					dvt = type.GetGenericArguments()[1];
				}
				bool isa = (!ish && (type.Name[0] == '<') && type.IsSealed);
				var ctr = (isa ? type.GetConstructors()[0] : null);
				var cta = ((ctr != null) ? ctr.GetParameters() : null);
				var arg = ((cta != null) ? new object[cta.Length] : null);
				object o = null;
				bool obj;
				if (type.IsInterface)
				{
					if (ish && !isd)
						type = typeof(Dictionary<,>).MakeGenericType(dkt, dvt);
					else if (isd)
						type = typeof(System.Collections.Hashtable);
					else
						type = typeof(object);
				}
				if (obj = (type == typeof(object)))
				{
					type = typeof(Dictionary<string, object>);
					dkt = typeof(string);
					dvt = typeof(object);
				}
				if (ch == '{')
				{
					System.Collections.IDictionary d = null;
					if (!isa)
						o = Activator.CreateInstance(type, arg);
					if (typeof(System.Collections.IDictionary).IsAssignableFrom(type))
						d = (System.Collections.IDictionary)o;
					var ti = (!obj ? Known(type) : null);
					if (data) read('{');
					while (data && (ch <= ' ')) // Spaces
						read(NEXT);
					if (ch == '}')
					{
						if (data) read('}');
						return (isa ? Activator.CreateInstance(type, arg) : o);
					}
					while (data)
					{
						string k = (Literal(type, typeof(string)) as string);
						object m;
						if (!obj && (k == null))
							throw Error("Bad object key");
						k = ((k != null) ? String.Intern(k) : k);
						m = (!obj ? Typed((isa ? (object)cta : type), ti, k) : null);
						while (data && (ch <= ' ')) // Spaces
							read(NEXT);
						if (data) read(':');
						if (m != null)
						{
							if (!isa)
							{
								var p = (System.Reflection.PropertyInfo)m;
								var t = p.PropertyType;
								var v = Compile(t);
								var mapped = Map(Outer(type, null), ((v != null) ? v.GetType() : typeof(object)), v);
								p.SetValue(o, ((mapped != null) ? mapped.DynamicInvoke() : v), null);
							}
							else
							{
								int i = (int)m;
								var t = cta[i].ParameterType;
								var v = Compile(t);
								var mapped = Map(Outer(type, null), ((v != null) ? v.GetType() : typeof(object)), v);
								arg[i] = ((mapped != null) ? mapped.DynamicInvoke() : v);
							}
						}
						else if (d != null)
						{
							var v = Compile(dvt);
							var mapped = Map(Outer(d.GetType(), dkt), k);
							var h = ((mapped != null) ? mapped.DynamicInvoke() : k);
							if (h == null)
								throw Error("Bad key");
							if (d.Contains(h))
								throw Error(String.Format("Duplicate key \"{0}\"", h));
							mapped = Map(Outer(dvt, null), ((v != null) ? v.GetType() : typeof(object)), v);
							d[h] = ((mapped != null) ? mapped.DynamicInvoke() : v);
						}
						else
							Compile(null as Type);
						while (data && (ch <= ' ')) // Spaces
							read(NEXT);
						if (ch == '}')
						{
							if (data) read('}');
							 return (isa ? Activator.CreateInstance(type, arg) : o);
						}
						if (data) read(',');
						while (data && (ch <= ' ')) // Spaces
							read(NEXT);
					}
				}
				throw Error("Bad object");
			}

			private System.Collections.IEnumerable Array(Type type)
			{
				var isa = type.IsArray;
				var ie = (isa || (type.GetInterfaces().Where(i => typeof(System.Collections.IEnumerable).IsAssignableFrom(i)).FirstOrDefault() != null));
				var et = (ie ? (isa ? type.GetElementType() : type.GetGenericArguments()[0]) : null);
				var lt = ((et != null) ? typeof(List<>).MakeGenericType(et) : typeof(List<object>));
				var l = (System.Collections.IList)Activator.CreateInstance(lt, null);
				if (ch == '[')
				{
					if (data) read('[');
					while (data && (ch <= ' ')) // Spaces
						read(NEXT);
					if (ch == ']')
					{
						if (data) read(']');
						return (isa ? (System.Collections.IEnumerable)lt.GetMethod("ToArray").Invoke(l, null) : l);
					}
					while (data)
					{
						l.Add(Compile(et));
						while (data && (ch <= ' ')) // Spaces
							read(NEXT);
						if (ch == ']')
						{
							if (data) read(']');
							return (isa ? (System.Collections.IEnumerable)lt.GetMethod("ToArray").Invoke(l, null) : l);
						}
						if (data) read(',');
						while (data && (ch <= ' ')) // Spaces
							read(NEXT);
					}
				}
				throw Error("Bad array");
			}

			private object Compile(Type type)
			{
				type = (type ?? typeof(object));
				while (data && (ch <= ' ')) // Spaces
					read(NEXT);
				switch (ch)
				{
					case '{':
						return Object(type);
					case '[':
						return Array(type);
					case '"':
						return Literal(type, null);
					case '-':
						return Number(type);
					default:
						return ((ch >= '0') && (ch <= '9') ? Number(type) : Word(type));
				}
			}

			internal object Compile(Type to, Type from)
			{
				var obj = Compile(from);
				while (data && (ch <= ' ')) // Spaces
					read(NEXT);
				if (data)
					throw Error("Unexpected content");
				var mapped = Map(Outer(to, null), ((obj != null) ? obj.GetType() : typeof(object)), obj);
				return ((mapped != null) ? mapped.DynamicInvoke() : obj);
			}
		}

		protected TResult CreateAs<TResult>(object input, ParserSettings settings, Type from, params Delegate[] revivers)
		{
			return (TResult)new Phrase((settings ?? Settings), input, revivers).Compile(typeof(TResult), from);
		}

		public static ParserSettings DefaultSettings
		{
			get
			{
				return new ParserSettings
				{
					LiteralBufferSize = LSIZE
				};
			}
		}

		public Parser() : this(null) { }

		public Parser(ParserSettings settings)
		{
			Configure(settings);
		}

		public Parser Configure()
		{
			return Configure(null);
		}

		public Parser Configure(ParserSettings settings)
		{
			Settings = settings;
			return this;
		}

		public TResult Parse<TResult>(string text)
		{
			return CreateAs<TResult>(text, null, null);
		}

		public TResult Parse<TResult>(string text, ParserSettings settings)
		{
			return CreateAs<TResult>(text, settings, null);
		}

		public TResult Parse<TResult>(System.IO.Stream stream)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CreateAs<TResult>(reader, null, null);
			}
		}

		public TResult Parse<TResult>(System.IO.Stream stream, ParserSettings settings)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CreateAs<TResult>(reader, settings, null);
			}
		}

		public TResult Parse<TResult>(System.IO.Stream stream, ParserSettings settings, params Delegate[] revivers)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CreateAs<TResult>(reader, settings, null, revivers);
			}
		}

		public TResult Parse<TResult>(System.IO.StreamReader reader)
		{
			return CreateAs<TResult>(reader, null, null);
		}

		public TResult Parse<TResult>(System.IO.StreamReader reader, params Delegate[] revivers)
		{
			return CreateAs<TResult>(reader, null, null, revivers);
		}

		public TResult Parse<TResult>(System.IO.StreamReader reader, ParserSettings settings)
		{
			return CreateAs<TResult>(reader, settings, null);
		}

		public TResult Parse<TResult>(System.IO.StreamReader reader, ParserSettings settings, params Delegate[] revivers)
		{
			return CreateAs<TResult>(reader, settings, null, revivers);
		}

		public TResult Parse<TValue, TResult>(string text, TValue prototype)
		{
			return CreateAs<TResult>(text, null, typeof(TValue));
		}

		public TResult Parse<TValue, TResult>(string text, TValue prototype, params Delegate[] revivers)
		{
			return CreateAs<TResult>(text, null, typeof(TValue), revivers);
		}

		public TResult Parse<TValue, TResult>(string text, ParserSettings settings, TValue prototype)
		{
			return CreateAs<TResult>(text, settings, typeof(TValue));
		}

		public TResult Parse<TValue, TResult>(string text, ParserSettings settings, TValue prototype, params Delegate[] revivers)
		{
			return CreateAs<TResult>(text, settings, typeof(TValue), revivers);
		}

		public TResult Parse<TValue, TResult>(System.IO.Stream stream, TValue prototype)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CreateAs<TResult>(reader, null, typeof(TValue));
			}
		}

		public TResult Parse<TValue, TResult>(System.IO.Stream stream, TValue prototype, params Delegate[] revivers)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CreateAs<TResult>(reader, null, typeof(TValue), revivers);
			}
		}

		public TResult Parse<TValue, TResult>(System.IO.Stream stream, ParserSettings settings, TValue prototype)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CreateAs<TResult>(reader, settings, typeof(TValue));
			}
		}

		public TResult Parse<TValue, TResult>(System.IO.Stream stream, ParserSettings settings, TValue prototype, params Delegate[] revivers)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CreateAs<TResult>(reader, settings, typeof(TValue), revivers);
			}
		}

		public TResult Parse<TValue, TResult>(System.IO.StreamReader reader, TValue prototype)
		{
			return CreateAs<TResult>(reader, null, typeof(TValue));
		}

		public TResult Parse<TValue, TResult>(System.IO.StreamReader reader, ParserSettings settings, TValue prototype)
		{
			return CreateAs<TResult>(reader, settings, typeof(TValue));
		}

		public TResult Parse<TValue, TResult>(System.IO.StreamReader reader, TValue prototype, params Delegate[] revivers)
		{
			return CreateAs<TResult>(reader, null, typeof(TValue), revivers);
		}

		public TResult Parse<TValue, TResult>(System.IO.StreamReader reader, ParserSettings settings, TValue prototype, params Delegate[] revivers)
		{
			return CreateAs<TResult>(reader, settings, typeof(TValue), revivers);
		}

		public ParserSettings Settings { get; set; }
	}
}