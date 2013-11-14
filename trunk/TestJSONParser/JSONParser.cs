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
                return new Parser().Parse(text, prototype);
            }

            public static T ParseJSON<T>(this T prototype, System.IO.Stream stream)
            {
                return new Parser().Parse(stream, prototype);
            }

            public static T ParseJSON<T>(this T prototype, System.IO.StreamReader reader)
            {
                return new Parser().Parse(reader, prototype);
            }
        }

        public class Parser
        {
            private static readonly IDictionary<char, char> ESC = new Dictionary<char, char>();
            private StringBuilder cs = new StringBuilder();

            static Parser()
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
                System.IO.StreamReader sr = (source as System.IO.StreamReader);
                string text = (source as string);
                int len = (text ?? String.Empty).Length;
                char[] wc = new char[1];
                int at = 0;
                Func<bool> atEndOfStream = delegate()
                {
                    return sr.EndOfStream;
                };
                Func<char> readFromStream = delegate()
                {
                    sr.Read(wc, 0, 1);
                    return wc[0];
                };
                Func<bool> atEndOfText = delegate()
                {
                    return (at >= len);
                };
                Func<char> readFromText = delegate()
                {
                    return text[at++];
                };
                Func<object> val = null;
                Func<bool> atEnd = null;
                Func<char> read = null;
                object value = null;
                bool data = true;
                char ch = ' ';
                Func<string, Exception> error = delegate(string message)
                {
                    return new Exception(String.Format("{0} at index {1}", message, at));
                };
                atEnd = ((sr != null) ? atEndOfStream : atEndOfText);
                read = ((sr != null) ? readFromStream : readFromText);
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
                        throw error(String.Format("Expected '{0}' instead of '{1}'", c, ch));
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
                    throw error("Bad string");
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
                    throw error(String.Format("Unexpected '{0}'", ch));
                };
                Func<IList<object>> list = delegate()
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
                            l.Add(val());
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
                    throw error("Bad array");
                };
                Func<object> obj = delegate()
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
                            key = String.Intern(str());
                            space();
                            next(':');
                            if (o.ContainsKey(key))
                                throw error(String.Format("Duplicate key \"{0}\"", key));
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
                            return obj();
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
                value = val();
                space();
                if (data)
                    throw error("Syntax error");
                return value;
            }
        }
    }
}