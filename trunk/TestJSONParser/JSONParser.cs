using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text.Notations
{
    namespace JSON
    {
        public static class Extensions
        {
            public static IDictionary<string, object> JSONObject(this object obj)
            {
                return (IDictionary<string, object>)obj;
            }

            public static IList<object> JSONArray(this object obj)
            {
                return (IList<object>)obj;
            }

            public static object ParseJSON(this object obj, string text)
            {
                return obj.ParseJSON<object>(text);
            }

            public static object ParseJSON(this object obj, System.IO.Stream stream)
            {
                return obj.ParseJSON<object>(stream);
            }

            public static object ParseJSON(this object obj, System.IO.StreamReader reader)
            {
                return obj.ParseJSON<object>(reader);
            }

            public static T ParseJSON<T>(this T prototype, string text)
            {
                return new JSONParser().Parse(text, prototype);
            }

            public static T ParseJSON<T>(this T prototype, System.IO.Stream stream)
            {
                return new JSONParser().Parse(stream, prototype);
            }

            public static T ParseJSON<T>(this T prototype, System.IO.StreamReader reader)
            {
                return new JSONParser().Parse(reader, prototype);
            }
        }

        public class JSONParser
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
            /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/). This can be, either:
            /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
            /// </summary>
            /// <param name="text">The string of JSON text to parse.</param>
            /// <returns>The deserialized object.</returns>
            public object Parse(string text)
            {
                return DoParse(text);
            }

            /// <summary>
            /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/). This can be, either:
            /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
            /// </summary>
            /// <param name="stream">The stream of JSON text to parse.</param>
            /// <returns>The deserialized object.</returns>
            public object Parse(System.IO.Stream stream)
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
                {
                    return DoParse(reader);
                }
            }

            /// <summary>
            /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/). This can be, either:
            /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
            /// </summary>
            /// <param name="reader">The reader of JSON text to parse.</param>
            /// <returns>The deserialized object.</returns>
            public object Parse(System.IO.StreamReader reader)
            {
                return DoParse(reader);
            }

            /// <summary>
            /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the specified type.
            /// </summary>
            /// <param name="text">The string of JSON text to parse.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(string text)
            {
                return (T)DoParse(text, typeof(T));
            }

            /// <summary>
            /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
            /// </summary>
            /// <param name="text">The string of JSON text to parse.</param>
            /// <param name="prototype">The prototype for the target type.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(string text, T prototype)
            {
                return Parse<T>(text);
            }

            /// <summary>
            /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the specified type.
            /// </summary>
            /// <param name="stream">The stream of JSON text to parse.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(System.IO.Stream stream)
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
                {
                    return (T)DoParse(reader, typeof(T));
                }
            }

            /// <summary>
            /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
            /// </summary>
            /// <param name="stream">The stream of JSON text to parse.</param>
            /// <param name="prototype">The prototype for the target type.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(System.IO.Stream stream, T prototype)
            {
                return Parse<T>(stream);
            }

            /// <summary>
            /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the specified type.
            /// </summary>
            /// <param name="reader">The reader of JSON text to parse.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(System.IO.StreamReader reader)
            {
                return (T)DoParse(reader, typeof(T));
            }

            /// <summary>
            /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
            /// </summary>
            /// <param name="reader">The reader of JSON text to parse.</param>
            /// <param name="prototype">The prototype for the target type.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(System.IO.StreamReader reader, T prototype)
            {
                return Parse<T>(reader);
            }

            private object DoParse(object source)
            {
                return DoParse(source, null);
            }

            private object DoParse(object source, /*(provision...)*/Type type)
            {
                Func<string, int, Exception> error = (message, index) => new Exception(String.Format("{0} at index {1}", message, index));
                System.IO.StreamReader sr = (source as System.IO.StreamReader);
                StringBuilder cs = new StringBuilder();
                Hashtable rtti = new Hashtable();
                string text = (source as string);
                int len = (text ?? String.Empty).Length;
                char[] wc = new char[1];
                int at = 0;
                type = (type ?? typeof(object));
                Func<bool> atEndOfStream = () => sr.EndOfStream;
                Func<bool> atEndOfText = () => (at >= len);
                Func<char> readFromStream = delegate()
                {
                    sr.Read(wc, 0, 1);
                    return wc[0];
                };
                Func<char> readFromText = () => text[at++];
                Func<bool> atEnd = ((sr != null) ? atEndOfStream : atEndOfText);
                Func<char> read = ((sr != null) ? readFromStream : readFromText);
                Func<Type, object> parse = null;
                object value = null;
                bool data = true;
                char ch = ' ';
                Func<Type, Hashtable, string, bool, Type> ensure = delegate(Type tt, Hashtable tm, string pn, bool anon)
                {
                    if (tt != typeof(object))
                    {
                        var tp = tt.GetProperty(pn);
                        if ((tp != null) && (tp.CanWrite || anon))
                            if (!tm.ContainsKey(pn))
                                return (Type)(tm[tp] = tt.GetProperty(pn).PropertyType);
                            else
                                return (Type)tm[tp];
                        else
                            return null;
                    }
                    return null;
                };
                Func<Type, Hashtable> target = delegate(Type rt)
                {
                    if (rt != typeof(object))
                        return (Hashtable)(!rtti.ContainsKey(rt) ? (rtti[rt] = new Hashtable()) : rtti[rt]);
                    else
                        return null;
                };
                Func<bool> cont = delegate()
                {
                    if (!atEnd())
                        ch = read();
                    else
                        data = false;
                    return data;
                };
                Func<char, bool> next = delegate(char c)
                {
                    if (c != ch)
                        throw error(String.Format("Expected '{0}' instead of '{1}'", c, ch), at);
                    if (!atEnd())
                        ch = read();
                    else
                        data = false;
                    return data;
                };
                Func<double> num = delegate()
                {
                    cs.Length = 0;
                    if (ch == '-')
                    {
                        cs.Append('-');
                        next('-');
                    }
                    while ((ch >= '0') && (ch <= '9'))
                    {
                        cs.Append(ch);
                        cont();
                    }
                    if (ch == '.')
                    {
                        cs.Append('.');
                        while (cont() && (ch >= '0') && (ch <= '9'))
                            cs.Append(ch);
                    }
                    if ((ch == 'e') || (ch == 'E'))
                    {
                        cs.Append(ch);
                        cont();
                        if ((ch == '-') || (ch == '+'))
                        {
                            cs.Append(ch);
                            cont();
                        }
                        while ((ch >= '0') && (ch <= '9'))
                        {
                            cs.Append(ch);
                            cont();
                        }
                    }
                    return double.Parse(cs.ToString());
                };
                Func<string> str = delegate()
                {
                    int hex, i, uffff;
                    cs.Length = 0;
                    if (ch == '"')
                    {
                        while (cont())
                        {
                            if (ch == '"')
                            {
                                cont();
                                return cs.ToString();
                            }
                            if (ch == '\\')
                            {
                                cont();
                                if (ch == 'u')
                                {
                                    uffff = 0;
                                    for (i = 0; i < 4; i += 1)
                                    {
                                        cont();
                                        hex = Convert.ToInt32(String.Empty + ch, 16);
                                        uffff = uffff * 16 + hex;
                                    }
                                    cs.Append(Convert.ToChar(uffff));
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
                                        cs.Append(ESC[ch]);
                                    else
                                        break;
                                }
                            }
                            else
                                cs.Append(ch);
                        }
                    }
                    throw error("Bad string", at);
                };
                Action space = delegate()
                {
                    while (data && (ch <= ' '))
                        cont();
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
                    throw error(String.Format("Unexpected '{0}'", ch), at);
                };
                Func<Type, IEnumerable> list = delegate(Type t)
                {
                    IList<object> l = new List<object>();
                    if (ch == '[')
                    {
                        next('[');
                        space();
                        if (ch == ']')
                        {
                            next(']');
                            return l;
                        }
                        while (data)
                        {
                            l.Add(parse(typeof(object)));
                            space();
                            if (ch == ']')
                            {
                                next(']');
                                return l;
                            }
                            next(',');
                            space();
                        }
                    }
                    throw error("Bad array", at);
                };
                Func<Type, object> obj = delegate(Type t)
                {
                    var anon = (t.Name.StartsWith("<>") ? new List<object>() : null);
                    var actr = ((anon != null) ? t.GetConstructors()[0].GetParameters() : null);
                    string key;
                    object o;
                    if (ch == '{')
                    {
                        var d = new Dictionary<string, object>();
                        if (t != typeof(object) && (anon == null))
                        {
                            var ctor = t.GetConstructors().OrderBy(c => c.GetParameters().Length).First();
                            var carg = ctor.GetParameters();
                            o = Activator.CreateInstance(t, new object[carg.Length]);
                        }
                        else
                            o = d;
                        Hashtable ti = target(t);
                        next('{');
                        space();
                        if (ch == '}')
                        {
                            next('}');
                            return ((anon != null) ? Activator.CreateInstance(t, anon.ToArray()) : o);
                        }
                        while (data)
                        {
                            key = String.Intern(str());
                            Type nt;
                            space();
                            next(':');
                            if ((nt = ensure(t, ti, key, (anon != null))) == null)
                            {
                                if (d.ContainsKey(key))
                                    throw error(String.Format("Duplicate key \"{0}\"", key), at);
                                d[key] = parse(typeof(object));
                            }
                            else
                            {
                                if (anon != null)
                                {
                                    if ((from arg in actr where arg.Name == key select arg).Count() > 0)
                                        anon.Add(parse(nt));
                                }
                                else
                                    t.GetProperty(key).SetValue(o, parse(nt), null);
                            }
                            space();
                            if (ch == '}')
                            {
                                next('}');
                                return ((anon != null) ? Activator.CreateInstance(t, anon.ToArray()) : o);
                            }
                            next(',');
                            space();
                        }
                    }
                    throw error("Bad object", at);
                };
                parse = delegate(Type t)
                {
                    space();
                    switch (ch)
                    {
                        case '{':
                            return obj(t);
                        case '[':
                            return list(t);
                        case '"':
                            return str();
                        case '-':
                            return num();
                        default:
                            return ((ch >= '0') && (ch <= '9') ? num() : word());
                    }
                };
                value = parse(type ?? typeof(object));
                space();
                if (data)
                    throw error("Syntax error", at);
                return value;
            }
        }
    }
}