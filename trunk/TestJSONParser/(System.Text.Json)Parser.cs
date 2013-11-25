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
	public struct Outer
	{
		public readonly Type Type;
		public readonly IDictionary<string, object> Hash;
		public readonly object Key;

		public Outer(Type type, object key) : this(type, null, key) { }
		public Outer(Type type, IDictionary<string, object> hash, object key)
		{
			Type = type;
			Hash = hash;
			Key = key;
		}
	}

	public class ParserSettings
	{
		public bool AcceptIdentifiers { get; set; }
		public bool WithoutObjectHash { get; set; }
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
				value = ((found = ContainsKey(key)) ? (TValue)this[key] : default(TValue));
				return found;
			}

			internal TValue Set(TKey key, TValue value)
			{
				if (!ContainsKey(key))
					Add(key, value);
				return value;
			}
		}

		internal class PropInfo
		{
			private readonly Action<object, object> DSet;
			internal readonly System.Reflection.PropertyInfo Data;

			internal PropInfo(System.Reflection.PropertyInfo prop)
			{
				Data = prop;
				DSet = ISet();
			}

			private Action<object, object> ISet()
			{
				var instance = System.Linq.Expressions.Expression.Parameter(typeof(object), "instance");
				var value = System.Linq.Expressions.Expression.Parameter(typeof(object), "value");
				System.Linq.Expressions.UnaryExpression instanceCast = (!Data.DeclaringType.IsValueType) ? System.Linq.Expressions.Expression.TypeAs(instance, Data.DeclaringType) : System.Linq.Expressions.Expression.Convert(instance, Data.DeclaringType);
				System.Linq.Expressions.UnaryExpression valueCast = (!Data.PropertyType.IsValueType) ? System.Linq.Expressions.Expression.TypeAs(value, Data.PropertyType) : System.Linq.Expressions.Expression.Convert(value, Data.PropertyType);
				return System.Linq.Expressions.Expression.Lambda<Action<object, object>>(System.Linq.Expressions.Expression.Call(instanceCast, Data.GetSetMethod(), valueCast), new System.Linq.Expressions.ParameterExpression[] { instance, value }).Compile();
			}

			internal void Set(object instance, object value)
			{
				DSet(instance, value);
			}
		}

		internal class Phrase
		{
			private static readonly IDictionary<char, char> ESC = new Dictionary<char, char>();

			private Cache<Type, Cache<string, PropInfo>> props;
			private Cache<Type, Cache<string, int>> anons;
			private Cache<Delegate, JSON.Callee> rces;
			private Cache<Type, Type> arts;
			private Delegate[] revs;
			private ParserSettings cfg;
			private System.IO.StreamReader str;
			private string txt;
			private bool ids;
			private bool noh;
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
				props = new Cache<Type, Cache<string, PropInfo>>();
				anons = new Cache<Type, Cache<string, int>>();
				rces = new Cache<Delegate, JSON.Callee>();
				arts = new Cache<Type, Type>();
				revs = (Delegate[])(revivers ?? new Delegate[] { }).Clone();
				cfg = (settings ?? DefaultSettings);
				ids = cfg.AcceptIdentifiers;
				noh = cfg.WithoutObjectHash;
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

			private void Known(Type type, out Cache<string, PropInfo> pi, out Cache<string, int> ti)
			{
				if (!props.Get(type, out pi))
					pi = props.Set(type, new Cache<string, PropInfo>());
				if (!anons.Get(type, out ti))
					ti = anons.Set(type, new Cache<string, int>());
			}

			private object Typed(object obj, object hash, string key)
			{
				if (obj is Type)
				{
					var rpi = (Cache<string, PropInfo>)hash;
					PropInfo pi;
					if (!rpi.Get(key, out pi))
					{
						var type = (Type)obj;
						var prop = type.GetProperty(key);
						pi = rpi.Set(key, ((prop != null) && prop.CanWrite) ? new PropInfo(prop) : null);
					}
					return pi;
				}
				else
				{
					var rti = (Cache<string, int>)hash;
					int k;
					if (!rti.Get(key, out k))
					{
						var a = (System.Reflection.ParameterInfo[])obj;
						int i = a.Length;
						while (--i >= 0)
						{
							rti.Set(a[i].Name, i + 1);
							if (key == a[i].Name)
								k = (i + 1);
						}
					}
					return k;
				}
			}

			private Func<object> Map<TValue>(Outer outer, Type type, TValue value)
			{
				return Map(outer, typeof(TValue), type, value);
			}

			private Func<object> Map(Outer outer, Type match, Type type, object value)
			{
				Func<object> mapper = null;
				if
				(
					(revs.Length > 0) &&
					(
						(
							(value != null) && match.IsAssignableFrom(value.GetType())
						) ||
						(
							(value == null) && (match.IsClass || match.IsInterface)
						)
					)
				)
				{
					for (int i = 0; i < revs.Length; i++)
					{
						var candidate = revs[i];
						if
						(
							(candidate == null) ||
							!candidate.GetType().IsGenericType ||
							(candidate.GetType().GetGenericTypeDefinition() != typeof(Func<,,,>))
						)
							continue;
						var args = candidate.GetType().GetGenericArguments();
						if
						(
							(args[0] != typeof(Outer)) ||
							(args[1] != typeof(Type)) ||
							(!args[2].IsAssignableFrom(match)) ||
							(args[3] != typeof(object))
						)
							continue;
						JSON.Callee ce;
						if (!rces.Get(candidate, out ce))
							ce = rces.Set(candidate, JSON.Callable(typeof(object), typeof(Outer), typeof(Type), match, candidate));
						mapper = (ce.Invoke(outer, type, value) as Func<object>);
						if (mapper != null)
							break;
					}
				}
				return mapper;
			}

			private Outer Outer(Type type, object key)
			{
				return new Outer(type, key);
			}

			private Outer Outer(Type type, IDictionary<string, object> hash, object key)
			{
				return new Outer(type, hash, key);
			}

			private object Word(Type type)
			{
				Func<object> mapped;
				switch (ch)
				{
					case 't':
						if (data) read('t');
						if (data) read('r');
						if (data) read('u');
						if (data) read('e');
						mapped = Map(Outer(type, null), typeof(bool), true);
						return ((mapped != null) ? mapped() : true);
					case 'f':
						if (data) read('f');
						if (data) read('a');
						if (data) read('l');
						if (data) read('s');
						if (data) read('e');
						mapped = Map(Outer(type, null), typeof(bool), false);
						return ((mapped != null) ? mapped() : false);
					case 'n':
						if (data) read('n');
						if (data) read('u');
						if (data) read('l');
						if (data) read('l');
						mapped = Map(Outer(type, null), typeof(object), null as object);
						return ((mapped != null) ? mapped() : null);
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
				var mapped = Map(Outer(type, null), typeof(double), n);
				return ((mapped != null) ? mapped() : n);
			}

			private object Literal(Type type, bool key)
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
							var mapped = Map(Outer(type, (key ? typeof(string) : null)), typeof(string), s);
							return ((mapped != null) ? mapped() : s);
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
								var mapped = Map(Outer(type, (key ? typeof(string) : null)), typeof(string), s);
								return ((mapped != null) ? mapped() : s);
							}
					}
				}
				throw Error("Bad string");
			}

			private Type Realizes(Type type, Type generic)
			{
				var itfs = type.GetInterfaces();
				foreach (var it in itfs)
					if (it.IsGenericType && it.GetGenericTypeDefinition() == generic)
						return type;
				if (type.IsGenericType && type.GetGenericTypeDefinition() == generic)
					return type;
				if (type.BaseType == null)
					return null;
				return Realizes(type.BaseType, generic);
			}

			private object Object(Type type, IDictionary<string, object> hash)
			{
				type = (type ?? typeof(object));
				bool isd = typeof(System.Collections.IDictionary).IsAssignableFrom(type);
				bool ish = (isd || (type.IsGenericType && (Realizes(type, typeof(IDictionary<,>)) != null)));
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
					Cache<string, PropInfo> pi = null;
					Cache<string, int> ti = null;
					if (!isa)
						o = Activator.CreateInstance(type, arg);
					if (typeof(System.Collections.IDictionary).IsAssignableFrom(type))
						d = (System.Collections.IDictionary)o;
					if (!obj)
						Known(type, out pi, out ti);
					if (data) read('{');
					while (data && (ch <= ' ')) // Spaces
						read(NEXT);
					if (ch == '}')
					{
						if (data) read('}');
						o = (isa ? Activator.CreateInstance(type, arg) : o);
						if (hash != null)
						{
							var mapped = Map(Outer(o.GetType(), hash, null), typeof(object), o.GetType(), o);
							o = ((mapped != null) ? mapped() : o);
						}
						return o;
					}
					bool t = false;
					while (data)
					{
						string k = (Literal(type, true) as string);
						object m = null;
						if (!obj && (k == null))
							throw Error("Bad object key");
						if (isa)
						{
							int n;
							if (!t)
							{
								n = (int)(m = Typed(cta, ti, k));
								t = true;
							}
							else
								m = (ti.Get(k, out n) ? (object)n : null);
							m = ((n > 0) ? (object)(n - 1) : null);
						}
						else
						{
							if (!isa)
								m = (!obj ? Typed(type, pi, k) : null);
						}
						while (data && (ch <= ' ')) // Spaces
							read(NEXT);
						if (data) read(':');
						if (m != null)
						{
							if (!isa)
							{
								var tp = (PropInfo)m;
								var p = tp.Data;
								var q = Compile(p.PropertyType);
								var mapped = Map(Outer(type, null), ((q != null) ? q.GetType() : typeof(object)), p.PropertyType, q);
								var v = ((mapped != null) ? mapped() : q);
								tp.Set(o, v);
								if (hash != null)
									hash[p.Name] = v;
							}
							else
							{
								int i = (int)m;
								var p = cta[i];
								var q = Compile(p.ParameterType);
								var mapped = Map(Outer(type, null), ((q != null) ? q.GetType() : typeof(object)), p.ParameterType, q);
								var v = ((mapped != null) ? mapped() : q);
								arg[i] = v;
								if (hash != null)
									hash[p.Name] = v;
							}
						}
						else if (d != null)
						{
							var v = Compile(dvt);
							var mapped = Map(Outer(d.GetType(), typeof(object)), typeof(string), dkt, k);
							var h = ((mapped != null) ? mapped() : k);
							if (h == null)
								throw Error("Bad key");
							if (d.Contains(h))
								throw Error(String.Format("Duplicate key \"{0}\"", h));
							mapped = Map(Outer(dvt, null), ((v != null) ? v.GetType() : typeof(object)), dvt, v);
							d[h] = ((mapped != null) ? mapped() : v);
							if (hash != null)
								hash[k] = d[h];
						}
						else
							Compile(null as Type);
						while (data && (ch <= ' ')) // Spaces
							read(NEXT);
						if (ch == '}')
						{
							if (data) read('}');
							o = (isa ? Activator.CreateInstance(type, arg) : o);
							if (hash != null)
							{
								var mapped = Map(Outer(o.GetType(), hash, null), o.GetType(), typeof(object), o);
								o = ((mapped != null) ? mapped() : o);
							}
							return o;
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
				bool isa = type.IsArray;
				System.Collections.IList lst;
				Type art, elt;
				if (!arts.Get(type, out art))
				{
					var nt = Realizes(type, typeof(IEnumerable<>));
					elt = (isa ? type.GetElementType() : ((nt != null) ? nt.GetGenericArguments()[0] : typeof(object)));
					art = arts.Set(type, typeof(List<>).MakeGenericType(elt));
				}
				else
					elt = (isa ? type.GetElementType() : art.GetGenericArguments()[0]);
				lst = (System.Collections.IList)Activator.CreateInstance(art);
				if (ch == '[')
				{
					if (data) read('[');
					while (data && (ch <= ' ')) // Spaces
						read(NEXT);
					if (ch == ']')
					{
						if (data) read(']');
						if (isa)
						{
							System.Array arr = System.Array.CreateInstance(elt, lst.Count);
							lst.CopyTo(arr, 0);
							return arr;
						}
						else
							return lst;
					}
					while (data)
					{
						lst.Add(Compile(elt));
						while (data && (ch <= ' ')) // Spaces
							read(NEXT);
						if (ch == ']')
						{
							if (data) read(']');
							if (isa)
							{
								System.Array arr = System.Array.CreateInstance(elt, lst.Count);
								lst.CopyTo(arr, 0);
								return arr;
							}
							else
								return lst;
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
						var hash = (!noh ? new Dictionary<string, object>() : null);
						return Object(type, hash);
					case '[':
						return Array(type);
					case '"':
						return Literal(type, false);
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
				var mapped = Map(Outer(to, null), ((obj != null) ? obj.GetType() : typeof(object)), to, obj);
				return ((mapped != null) ? mapped() : obj);
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