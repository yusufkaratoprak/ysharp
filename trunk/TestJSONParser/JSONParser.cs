using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text
{
	public static class JSONParser
	{
		private static readonly IDictionary<char, char> ESC = new Dictionary<char, char>();

		static JSONParser()
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

		/// <summary>
		/// Converts the specified JSON string to the .NET equivalent of a JSON "value"
		/// (as defined by http://json.org/). This can be, either:
		/// null, or true/false, or a System.Double, or a System.String, or an object[], or an IDictionary(string => object)
		/// </summary>
		/// <param name="text">The JSON string to parse.</param>
		/// <returns>The deserialized object.</returns>
		public static object Parse(string text)
		{
			Func<object> val = null;
			object obj = null;
			bool data = true;
			char ch = ' ';
			int at = 0;
			Func<string, Exception> error = delegate(string message)
			{
				return new Exception(string.Format("{0} at index {1}", message, at));
			};
			Func<char?, bool> next = delegate(char? c)
			{
				if (c.HasValue && (c.Value != ch))
					throw error(string.Format("Expected '{0}' instead of '{1}'", c.Value, ch));
				if (at < text.Length)
				{
					ch = text[at];
					at += 1;
				}
				else
					data = false;
				return data;
			};
			Func<double> num = delegate()
			{
				StringBuilder ns = new StringBuilder();
				if (ch == '-')
				{
					ns.Append('-');
					next('-');
				}
				while ((ch >= '0') && (ch <= '9'))
				{
					ns.Append(ch);
					next(null);
				}
				if (ch == '.')
				{
					ns.Append('.');
					while (next(null) && (ch >= '0') && (ch <= '9'))
						ns.Append(ch);
				}
				if ((ch == 'e') || (ch == 'E'))
				{
					ns.Append(ch);
					next(null);
					if ((ch == '-') || (ch == '+'))
					{
						ns.Append(ch);
						next(null);
					}
					while ((ch >= '0') && (ch <= '9'))
					{
						ns.Append(ch);
						next(null);
					}
				}
				return double.Parse(ns.ToString());
			};
			Func<string> str = delegate()
			{
				StringBuilder cs = new StringBuilder();
				int hex, i, uffff;
				if (ch == '"')
				{
					while (next(null))
					{
						if (ch == '"')
						{
							next(null);
							return cs.ToString();
						}
						if (ch == '\\')
						{
							next(null);
							if (ch == 'u')
							{
								uffff = 0;
								for (i = 0; i < 4; i += 1)
								{
									hex = Convert.ToInt32(string.Empty + next(null), 16);
									uffff = uffff * 16 + hex;
								}
								cs.Append(Convert.ToChar(uffff));
							}
							else if (ESC.ContainsKey(ch))
								cs.Append(ESC[ch]);
							else
								break;
						}
						else
							cs.Append(ch);
					}
				}
				throw error("Bad string");
			};
			Action space = delegate()
			{
				while (data && (ch <= ' '))
					next(null);
			};
			Func<object> word = delegate()
			{
				switch (ch)
				{
					case 't':
						next('t');
						next('r');
						next('u');
						next('e');
						return true;
					case 'f':
						next('f');
						next('a');
						next('l');
						next('s');
						next('e');
						return false;
					case 'n':
						next('n');
						next('u');
						next('l');
						next('l');
						return null;
				}
				throw error(string.Format("Unexpected '{0}'", ch));
			};
			Func<object[]> list = delegate()
			{
				ArrayList items = new ArrayList();
				if (ch == '[')
				{
					next('[');
					space();
					if (ch == ']')
					{
						next(']');
						return items.ToArray();
					}
					while (data)
					{
						items.Add(val());
						space();
						if (ch == ']')
						{
							next(']');
							return items.ToArray();
						}
						next(',');
						space();
					}
				}
				throw error("Bad array");
			};
			Func<Object> hash = delegate()
			{
				IDictionary<string, object> o = new Dictionary<string, object>();
				string key;
				if (ch == '{')
				{
					next('{');
					space();
					if (ch == '}')
					{
						next('}');
						return o;
					}
					while (data)
					{
						key = str();
						space();
						next(':');
						if (o.ContainsKey(key))
							throw error(string.Format("Duplicate key \"{0}\"", key));
						o[key] = val();
						space();
						if (ch == '}')
						{
							next('}');
							return o;
						}
						next(',');
						space();
					}
				}
				throw error("Bad object");
			};
			val = delegate()
			{
				space();
				switch (ch)
				{
					case '{':
						return hash();
					case '[':
						return list();
					case '"':
						return str();
					case '-':
						return num();
					default:
						return ((ch >= '0') && (ch <= '9') ? num() : word());
				}
			};
			obj = val();
			space();
			if (data)
				throw error("Syntax error");
			return obj;
		}
	}
}