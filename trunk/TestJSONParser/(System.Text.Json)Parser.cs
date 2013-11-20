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
	public delegate Func<R> Reviver<T, R>(Type type, Type key, T value);

	public class ParserSettings
	{
		public bool AcceptIdentifiers { get; set; }
		public int LiteralBufferSize { get; set; }
	}

	public class Parser
	{
		private const char NEXT = (char)0;
		private const int LSIZE = 4096;

		internal class Phrase
		{
			private static readonly IDictionary<char, char> ESC = new Dictionary<char, char>();

			private ParserSettings config;
			private System.Collections.Hashtable rtti;
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

			internal Phrase(ParserSettings settings, object input)
			{
				config = (settings ?? DefaultSettings);
				rtti = new System.Collections.Hashtable();
				ids = config.AcceptIdentifiers;
				lsz = (((lsz = config.LiteralBufferSize) > 0) ? lsz : LSIZE);

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

			private System.Collections.Hashtable Known(Type type)
			{
				return (System.Collections.Hashtable)(!rtti.ContainsKey(type) ? (rtti[type] = new System.Collections.Hashtable()) : rtti[type]);
			}

			private object Typed(object obj, System.Collections.Hashtable hash, string key)
			{
				if (!hash.ContainsKey(key))
				{
					if (obj is Type)
					{
						var p = ((Type)obj).GetProperty(key);
						return ((p != null) ? (hash[key] = p) : null);
					}
					else
					{
						var a = (System.Reflection.ParameterInfo[])obj;
						int i = a.Length;
						while (--i >= 0)
							if (a[i].Name == key)
								break;
						return ((i >= 0) ? (hash[key] = i) : null);
					}
				}
				else
					return hash[key];
			}

			private Delegate Map<T>(Delegate[] revivers, Type type, Type key, T value)
			{
				return Map(revivers, type, key, typeof(T), value);
			}

			private Delegate Map(Delegate[] revivers, Type outer, Type type, Type value, object obj)
			{
				Delegate mapper = null;
				if
				(
					(revivers != null) && (revivers.Length > 0) &&
					(
						(
							(obj != null) && value.IsAssignableFrom(obj.GetType())
						) ||
						(
							(obj == null) && (value.IsClass || value.IsInterface) && !value.IsGenericTypeDefinition
						)
					)
				)
					for (int i = 0; i < revivers.Length; i++)
					{
						var candidate = revivers[i];
						if
						(
							(candidate == null) ||
							!candidate.GetType().IsGenericType ||
							(candidate.GetType().GetGenericTypeDefinition() != typeof(Reviver<,>)) ||
							!candidate.GetType().GetGenericArguments()[0].IsAssignableFrom(value)
						)
							continue;
						if ((mapper = (revivers[i].DynamicInvoke(outer, type, obj) as Delegate)) != null)
							break;
					}
				return mapper;
			}

			private object Word(Type type, params Delegate[] revivers)
			{
				Delegate mapped;
				switch (ch)
				{
					case 't':
						if (data) read('t');
						if (data) read('r');
						if (data) read('u');
						if (data) read('e');
						mapped = Map(revivers, type, null, true);
						return ((mapped != null) ? mapped.DynamicInvoke() : true);
					case 'f':
						if (data) read('f');
						if (data) read('a');
						if (data) read('l');
						if (data) read('s');
						if (data) read('e');
						mapped = Map(revivers, type, null, false);
						return ((mapped != null) ? mapped.DynamicInvoke() : false);
					case 'n':
						if (data) read('n');
						if (data) read('u');
						if (data) read('l');
						if (data) read('l');
						mapped = Map(revivers, type, null, null as object);
						return ((mapped != null) ? mapped.DynamicInvoke() : null);
				}
				throw Error(String.Format("Unexpected '{0}'", ch));
			}

			private object Number(Type type, params Delegate[] revivers)
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
				var mapped = Map(revivers, type, null, n);
				return ((mapped != null) ? mapped.DynamicInvoke() : n);
			}

			private object Literal(Type type, bool key, params Delegate[] revivers)
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
							var mapped = Map(revivers, type, (key ? typeof(string) : null), s);
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
					if (key && ids)
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
								var mapped = Map(revivers, type, (key ? typeof(string) : null), s);
								return ((mapped != null) ? mapped.DynamicInvoke() : s);
							}
					}
				}
				throw Error("Bad string");
			}

			private bool Realizes(Type given, Type generic)
			{
				var itfs = given.GetInterfaces();
				foreach (var it in itfs)
					if (it.IsGenericType && it.GetGenericTypeDefinition() == generic)
						return true;
				if (given.IsGenericType && given.GetGenericTypeDefinition() == generic)
					return true;
				if (given.BaseType == null)
					return false;
				return Realizes(given.BaseType, generic);
			}

			private object Object(Type type, params Delegate[] revivers)
			{
				Func<Type, bool> dit = (t) => (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IDictionary<,>)));
				bool obj = ((type = (type ?? typeof(object))) == typeof(object));
				bool ish = (!obj && typeof(System.Collections.IDictionary).IsAssignableFrom(type));
				Type did = (!obj && !ish && type.IsGenericType && Realizes(type, typeof(IDictionary<,>)) ? type : null);
				Type dkt = ((did != null) ? did.GetGenericArguments()[0] : null);
				Type dvt = ((did != null) ? did.GetGenericArguments()[1] : null);
				bool isd = (ish || (did != null));
				bool dyn = (obj || isd);
				bool isa = (!dyn && (type.Name[0] == '<') && type.IsSealed);
				var ctr = (!dyn ? (!isa ? type.GetConstructors().OrderBy(c => c.GetParameters().Length).First() : type.GetConstructors()[0]) : null);
				var cta = (!dyn ? ctr.GetParameters() : null);
				var arg = (!dyn ? new object[cta.Length] : null);
				object o = null;
				dkt = (ish ? typeof(object) : dkt);
				dvt = (ish ? typeof(object) : dvt);
				if (ch == '{')
				{
					var d = (dyn ? ((did != null) ? (System.Collections.IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(dkt, dvt), null) : (obj ? (System.Collections.IDictionary)new Dictionary<string, object>() : new System.Collections.Hashtable())) : null);
					if (!dyn)
					{
						if (!isa)
							o = Activator.CreateInstance(type, arg);
					}
					else
						o = d;
					var ti = (!dyn ? Known(type) : null);
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
						string k = (Literal(type, true, revivers) as string);
						object m;
						if (!dyn && (k == null))
							throw Error("Bad object key");
						k = ((k != null) ? String.Intern(k) : k);
						m = (!dyn ? Typed((isa ? (object)cta : type), ti, k) : null);
						while (data && (ch <= ' ')) // Spaces
							read(NEXT);
						if (data) read(':');
						if (m != null)
						{
							if (!isa)
							{
								var p = (System.Reflection.PropertyInfo)m;
								var t = p.PropertyType;
								var v = Compile(t, true, revivers);
								var mapped = Map(revivers, type, null, ((v != null) ? v.GetType() : typeof(object)), v);
								p.SetValue(o, ((mapped != null) ? mapped.DynamicInvoke() : v), null);
							}
							else
							{
								int i = (int)m;
								var t = cta[i].ParameterType;
								var v = Compile(t, true, revivers);
								var mapped = Map(revivers, type, null, ((v != null) ? v.GetType() : typeof(object)), v);
								arg[i] = ((mapped != null) ? mapped.DynamicInvoke() : v);
							}
						}
						else
						{
							var v = Compile(dvt, true, revivers);
							if (dyn)
							{
								var mapped = Map(revivers, d.GetType(), dkt, k);
								var h = ((mapped != null) ? mapped.DynamicInvoke() : k);
								if (h == null)
									throw Error("Bad key");
								if (d.Contains(h))
									throw Error(String.Format("Duplicate key \"{0}\"", h));
								mapped = Map(revivers, dvt, null, ((v != null) ? v.GetType() : typeof(object)), v);
								d[h] = ((mapped != null) ? mapped.DynamicInvoke() : v);
							}
						}
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

			private System.Collections.IEnumerable Array(Type type, params Delegate[] revivers)
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
						l.Add(Compile(et, true, revivers));
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

			private object Compile(Type type, bool parse, params Delegate[] revivers)
			{
				type = (type ?? typeof(object));
				while (data && (ch <= ' ')) // Spaces
					read(NEXT);
				switch (ch)
				{
					case '{':
						return Object(type, revivers);
					case '[':
						return Array(type, revivers);
					case '"':
						return Literal(type, false, revivers);
					case '-':
						return Number(type, revivers);
					default:
						return ((ch >= '0') && (ch <= '9') ? Number(type, revivers) : Word(type, revivers));
				}
			}

			internal object Compile(Type to, Type from, params Delegate[] revivers)
			{
				var obj = Compile(from, true, revivers);
				while (data && (ch <= ' ')) // Spaces
					read(NEXT);
				if (data)
					throw Error("Unexpected content");
				var mapped = Map(revivers, to, null, ((obj != null) ? obj.GetType() : typeof(object)), obj);
				return ((mapped != null) ? mapped.DynamicInvoke() : obj);
			}
		}

		protected R CompileTo<R>(object input, ParserSettings settings, Type from, params Delegate[] revivers)
		{
			return (R)new Phrase((settings ?? Settings), input).Compile(typeof(R), from, revivers);
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

		public R Parse<R>(string text)
		{
			return CompileTo<R>(text, null, null);
		}

		public R Parse<R>(string text, ParserSettings settings)
		{
			return CompileTo<R>(text, settings, null);
		}

		public R Parse<R>(System.IO.Stream stream)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CompileTo<R>(reader, null, null);
			}
		}

		public R Parse<R>(System.IO.Stream stream, ParserSettings settings)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CompileTo<R>(reader, settings, null);
			}
		}

		public R Parse<R>(System.IO.Stream stream, ParserSettings settings, params Delegate[] revivers)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CompileTo<R>(reader, settings, null, revivers);
			}
		}

		public R Parse<R>(System.IO.StreamReader reader)
		{
			return CompileTo<R>(reader, null, null);
		}

		public R Parse<R>(System.IO.StreamReader reader, params Delegate[] revivers)
		{
			return CompileTo<R>(reader, null, null, revivers);
		}

		public R Parse<R>(System.IO.StreamReader reader, ParserSettings settings)
		{
			return CompileTo<R>(reader, settings, null);
		}

		public R Parse<R>(System.IO.StreamReader reader, ParserSettings settings, params Delegate[] revivers)
		{
			return CompileTo<R>(reader, settings, null, revivers);
		}

		public R Parse<T, R>(string text, T prototype)
		{
			return CompileTo<R>(text, null, typeof(T));
		}

		public R Parse<T, R>(string text, T prototype, params Delegate[] revivers)
		{
			return CompileTo<R>(text, null, typeof(T), revivers);
		}

		public R Parse<T, R>(string text, ParserSettings settings, T prototype)
		{
			return CompileTo<R>(text, settings, typeof(T));
		}

		public R Parse<T, R>(string text, ParserSettings settings, T prototype, params Delegate[] revivers)
		{
			return CompileTo<R>(text, settings, typeof(T), revivers);
		}

		public R Parse<T, R>(System.IO.Stream stream, T prototype)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CompileTo<R>(reader, null, typeof(T));
			}
		}

		public R Parse<T, R>(System.IO.Stream stream, T prototype, params Delegate[] revivers)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CompileTo<R>(reader, null, typeof(T), revivers);
			}
		}

		public R Parse<T, R>(System.IO.Stream stream, ParserSettings settings, T prototype)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CompileTo<R>(reader, settings, typeof(T));
			}
		}

		public R Parse<T, R>(System.IO.Stream stream, ParserSettings settings, T prototype, params Delegate[] revivers)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				return CompileTo<R>(reader, settings, typeof(T), revivers);
			}
		}

		public R Parse<T, R>(System.IO.StreamReader reader, T prototype)
		{
			return CompileTo<R>(reader, null, typeof(T));
		}

		public R Parse<T, R>(System.IO.StreamReader reader, ParserSettings settings, T prototype)
		{
			return CompileTo<R>(reader, settings, typeof(T));
		}

		public R Parse<T, R>(System.IO.StreamReader reader, T prototype, params Delegate[] revivers)
		{
			return CompileTo<R>(reader, null, typeof(T), revivers);
		}

		public R Parse<T, R>(System.IO.StreamReader reader, ParserSettings settings, T prototype, params Delegate[] revivers)
		{
			return CompileTo<R>(reader, settings, typeof(T), revivers);
		}

		public ParserSettings Settings { get; set; }
	}
}