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
            public static IDictionary<string, object> AsJSONObject(this object obj)
            {
                return (IDictionary<string, object>)obj;
            }

            public static IList<object> AsJSONArray(this object obj)
            {
                return (IList<object>)obj;
            }

            public static object FromJSON(this object obj, string text)
            {
                return FromJSON(obj, null, text);
            }

            public static object FromJSON(this object obj, JSONParserSettings settings, string text)
            {
                return obj.FromJSON<object>(settings, text);
            }

            public static object FromJSON(this object obj, System.IO.Stream stream)
            {
                return FromJSON(obj, null, stream);
            }

            public static object FromJSON(this object obj, JSONParserSettings settings, System.IO.Stream stream)
            {
                return obj.FromJSON<object>(settings, stream);
            }

            public static object FromJSON(this object obj, System.IO.StreamReader reader)
            {
                return FromJSON(obj, null, reader);
            }

            public static object FromJSON(this object obj, JSONParserSettings settings, System.IO.StreamReader reader)
            {
                return obj.FromJSON<object>(settings, reader);
            }

            public static T FromJSON<T>(this T prototype, string text)
            {
                return FromJSON<T>(prototype, null, text);
            }

            public static T FromJSON<T>(this T prototype, JSONParserSettings settings, string text)
            {
                return new JSONParser().Parse(text, settings, prototype);
            }

            public static T FromJSON<T>(this T prototype, System.IO.Stream stream)
            {
                return FromJSON<T>(prototype, null, stream);
            }

            public static T FromJSON<T>(this T prototype, JSONParserSettings settings, System.IO.Stream stream)
            {
                return new JSONParser().Parse(stream, settings, prototype);
            }

            public static T FromJSON<T>(this T prototype, System.IO.StreamReader reader)
            {
                return FromJSON<T>(prototype, null, reader);
            }

            public static T FromJSON<T>(this T prototype, JSONParserSettings settings, System.IO.StreamReader reader)
            {
                return new JSONParser().Parse(reader, settings, prototype);
            }
        }

        public class JSONParserSettings
        {
            public int LiteralsBuffer { get; set; }
        }

        public class JSONParser
        {
            internal class JSONPhrase
            {
                private static readonly IDictionary<char, char> ESC = new Dictionary<char, char>();
                private const char NEXT = (char)0;
                private const int LSIZ = 4096;

                private static readonly JSONParserSettings DefaultSettings =
                    new JSONParserSettings
                    {
                        LiteralsBuffer = LSIZ
                    };

                private JSONParserSettings config;
                private System.IO.StreamReader sr;
                private Hashtable rtti;
                private string text;
                private int lsiz;
                private int len;
                private StringBuilder sb;
                private char[] cs;
                private char[] wc;
                private bool data;
                private char ch;
                private int ci;
                private int at;
                private Func<char, bool> read;

                internal JSONPhrase(JSONParserSettings settings, object source)
                {
                    Initialize(settings);
                    Prepare(source);
                }

                private void Initialize(JSONParserSettings settings)
                {
                    config = (settings ?? DefaultSettings);
                }

                private void Prepare(object source)
                {
                    lsiz = (((lsiz = config.LiteralsBuffer) > 0) ? lsiz : LSIZ);

                    sr = (source as System.IO.StreamReader);
                    rtti = new Hashtable();
                    text = (source as string);
                    len = (text ?? String.Empty).Length;
                    sb = null;
                    cs = new char[lsiz];
                    wc = new char[1];
                    data = true;
                    ch = ' ';
                    ci = 0;
                    at = 0;
                    read = ((sr != null) ? (Func<char, bool>)ReadFromStream : (Func<char, bool>)ReadFromText);
                }

                private Exception error(string message, int index)
                {
                    return new Exception(String.Format("{0} at index {1}", message, index));
                }

                private bool ReadFromStream(char c)
                {
                    int r;
                    if ((c != NEXT) && (c != ch))
                        throw error(String.Format("Expected '{0}' instead of '{1}'", c, ch), at);
                    at += (r = sr.Read(wc, 0, 1));
                    ch = wc[0];
                    data = (r > 0);
                    return true;
                }

                private bool ReadFromText(char c)
                {
                    if ((c != NEXT) && (c != ch))
                        throw error(String.Format("Expected '{0}' instead of '{1}'", c, ch), at);
                    ch = text[at++];
                    data = (at < len);
                    return true;
                }

                private void Append(char c)
                {
                    if (ci < lsiz)
                        cs[ci++] = c;
                    else
                        if (sb != null)
                            sb.Append(c);
                        else
                            sb = new StringBuilder(new String(cs, 0, ci)).Append(c);
                }

                private Type Ensure(Type tt, Hashtable tm, string pn, bool anon)
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
                }

                private Hashtable Target(Type t)
                {
                    return (Hashtable)(!rtti.ContainsKey(t) ? (rtti[t] = new Hashtable()) : rtti[t]);
                }

                private object Typed(object o, Hashtable h, string k)
                {
                    if (!h.ContainsKey(k))
                    {
                        if (o is Type)
                        {
                            var m = ((Type)o).GetProperty(k);
                            return ((m != null) ? (h[k] = m) : null);
                        }
                        else
                        {
                            var p =
                                ((System.Reflection.ParameterInfo[])o).Select
                                (
                                    (a, i) =>
                                    new { Param = a, Index = i }
                                ).
                                FirstOrDefault(c => c.Param.Name == k);
                            return ((p != null) ? (h[k] = p.Index) : null);
                        }
                    }
                    else
                        return h[k];
                }

                private object Word()
                {
                    switch (ch)
                    {
                        case 't':
                            if (data) read('t');
                            if (data) read('r');
                            if (data) read('u');
                            if (data) read('e');
                            return true;
                        case 'f':
                            if (data) read('f');
                            if (data) read('a');
                            if (data) read('l');
                            if (data) read('s');
                            if (data) read('e');
                            return false;
                        case 'n':
                            if (data) read('n');
                            if (data) read('u');
                            if (data) read('l');
                            if (data) read('l');
                            return null;
                    }
                    throw error(String.Format("Unexpected '{0}'", ch), at);
                }

                private double Num()
                {
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
                        if (data) read(NEXT);
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
                    return double.Parse((sb != null) ? sb.ToString() : new String(cs, 0, ci));
                }

                private string Str()
                {
                    int hex, i, uffff;
                    sb = null;
                    ci = 0;
                    if (ch == '"')
                    {
                        while (data ? read(NEXT) : false)
                        {
                            if (ch == '"')
                            {
                                if (data) read(NEXT);
                                return ((sb != null) ? sb.ToString() : new String(cs, 0, ci));
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
                    throw error("Bad string", at);
                }

                private object Obj(Type t)
                {
                    bool root = (t == typeof(object));
                    bool anon =
                    (
                        (t.Name[0] == '<') &&
                        t.IsSealed &&
                        (
                            t.GetCustomAttributes
                            (
                                typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                                false
                            ).FirstOrDefault() != null
                        )
                    );
                    var ctor = (!root ? (!anon ? t.GetConstructors().OrderBy(c => c.GetParameters().Length).First() : t.GetConstructors()[0]) : null);
                    var arg = (!root ? ctor.GetParameters() : null);
                    var cta = (!root ? new object[arg.Length] : null);
                    object obj = null;
                    string key;
                    if (ch == '{')
                    {
                        IDictionary<string, object> d = null;
                        if (!root)
                        {
                            if (!anon)
                                obj = Activator.CreateInstance(t, cta);
                        }
                        else
                            obj = (d = new Dictionary<string, object>());
                        var ti = (!root ? Target(t) : null);
                        if (data) read('{');
                        while (data && (ch <= ' ')) // Spaces
                            read(NEXT);
                        if (ch == '}')
                        {
                            if (data) read('}');
                            return (anon ? Activator.CreateInstance(t, cta) : obj);
                        }
                        while (data)
                        {
                            object tm;
                            key = String.Intern(Str());
                            tm = (!root ? Typed((anon ? (object)arg : t), ti, key) : null);
                            while (data && (ch <= ' ')) // Spaces
                                read(NEXT);
                            if (data) read(':');
                            if ((tm != null) && !root)
                            {
                                if (!anon)
                                {
                                    var p = (System.Reflection.PropertyInfo)tm;
                                    p.SetValue(obj, CompileTo(p.PropertyType, true), null);
                                }
                                else
                                {
                                    int i = (int)tm;
                                    cta[i] = CompileTo(arg[i].ParameterType, true);
                                }
                            }
                            else
                            {
                                var o = CompileTo(typeof(object), true);
                                if (root)
                                {
                                    if (d.ContainsKey(key))
                                        throw error(String.Format("Duplicate key \"{0}\"", key), at);
                                    d[key] = o;
                                }
                            }
                            while (data && (ch <= ' ')) // Spaces
                                read(NEXT);
                            if (ch == '}')
                            {
                                if (data) read('}');
                                return (anon ? Activator.CreateInstance(t, cta) : obj);
                            }
                            if (data) read(',');
                            while (data && (ch <= ' ')) // Spaces
                                read(NEXT);
                        }
                    }
                    throw error("Bad object", at);
                }

                private IEnumerable Arr(Type t)
                {
                    Type it = null;
                    bool ar = t.IsArray;
                    bool ie = (ar || ((it = t.GetInterfaces().Where(i => typeof(IEnumerable).IsAssignableFrom(i)).FirstOrDefault()) != null));
                    Type et = (ie ? (ar ? t.GetElementType() : t.GetGenericArguments()[0]) : null);
                    Type lt = ((et != null) ? typeof(List<>).MakeGenericType(et) : typeof(List<object>));
                    IList l = (IList)Activator.CreateInstance(lt, null);
                    if (ch == '[')
                    {
                        if (data) read('[');
                        while (data && (ch <= ' ')) // Spaces
                            read(NEXT);
                        if (ch == ']')
                        {
                            if (data) read(']');
                            return (ar ? (IEnumerable)lt.GetMethod("ToArray").Invoke(l, null) : l);
                        }
                        while (data)
                        {
                            l.Add(CompileTo((et ?? typeof(object)), true));
                            while (data && (ch <= ' ')) // Spaces
                                read(NEXT);
                            if (ch == ']')
                            {
                                if (data) read(']');
                                return (ar ? (IEnumerable)lt.GetMethod("ToArray").Invoke(l, null) : l);
                            }
                            if (data) read(',');
                            while (data && (ch <= ' ')) // Spaces
                                read(NEXT);
                        }
                    }
                    throw error("Bad array", at);
                }

                private object CompileTo(Type target, bool parse)
                {
                    while (data && (ch <= ' ')) // Spaces
                        read(NEXT);
                    switch (ch)
                    {
                        case '{':
                            return Obj(target);
                        case '[':
                            return Arr(target);
                        case '"':
                            return Str();
                        case '-':
                            return Num();
                        default:
                            return ((ch >= '0') && (ch <= '9') ? Num() : Word());
                    }
                }

                internal object CompileTo(Type target)
                {
                    object value = CompileTo((target ?? typeof(object)), true);
                    while (data && (ch <= ' ')) // Spaces
                        read(NEXT);
                    if (data)
                        throw error("Syntax error", at);
                    return value;
                }

                static JSONPhrase()
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
            }

            private object Parse(object source, JSONParserSettings settings)
            {
                return Parse(source, settings, null);
            }

            private object Parse(object source, JSONParserSettings settings, Type type)
            {
                return new JSONPhrase((settings ?? Settings), source).CompileTo(type);
            }

            public JSONParser Configure()
            {
                return Configure(null);
            }

            public JSONParser Configure(JSONParserSettings settings)
            {
                Settings = settings;
                return this;
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
                return Parse(text, null);
            }

            /// <summary>
            /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/). This can be, either:
            /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
            /// </summary>
            /// <param name="text">The string of JSON text to parse.</param>
            /// <param name="settings">The parser settings to use.</param>
            /// <returns>The deserialized object.</returns>
            public object Parse(string text, JSONParserSettings settings)
            {
                return Parse(text as object, settings);
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
                return Parse(stream, null);
            }

            /// <summary>
            /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/). This can be, either:
            /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
            /// </summary>
            /// <param name="stream">The stream of JSON text to parse.</param>
            /// <param name="settings">The parser settings to use.</param>
            /// <returns>The deserialized object.</returns>
            public object Parse(System.IO.Stream stream, JSONParserSettings settings)
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
                {
                    return Parse(reader, settings);
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
                return Parse(reader, null);
            }

            /// <summary>
            /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/). This can be, either:
            /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
            /// </summary>
            /// <param name="reader">The reader of JSON text to parse.</param>
            /// <param name="settings">The parser settings to use.</param>
            /// <returns>The deserialized object.</returns>
            public object Parse(System.IO.StreamReader reader, JSONParserSettings settings)
            {
                return Parse(reader as object, settings);
            }

            /// <summary>
            /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the specified type.
            /// </summary>
            /// <param name="text">The string of JSON text to parse.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(string text)
            {
                return (T)Parse<T>(text, null);
            }

            /// <summary>
            /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the specified type.
            /// </summary>
            /// <param name="text">The string of JSON text to parse.</param>
            /// <param name="settings">The parser settings to use.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(string text, JSONParserSettings settings)
            {
                return (T)Parse(text as object, settings, typeof(T));
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
                return Parse<T>(text, null, prototype);
            }

            /// <summary>
            /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
            /// </summary>
            /// <param name="text">The string of JSON text to parse.</param>
            /// <param name="settings">The parser settings to use.</param>
            /// <param name="prototype">The prototype for the target type.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(string text, JSONParserSettings settings, T prototype)
            {
                return Parse<T>(text, settings);
            }

            /// <summary>
            /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the specified type.
            /// </summary>
            /// <param name="stream">The stream of JSON text to parse.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(System.IO.Stream stream)
            {
                return Parse<T>(stream, null);
            }

            /// <summary>
            /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the specified type.
            /// </summary>
            /// <param name="stream">The stream of JSON text to parse.</param>
            /// <param name="settings">The parser settings to use.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(System.IO.Stream stream, JSONParserSettings settings)
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
                {
                    return (T)Parse(reader as object, settings, typeof(T));
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
                return Parse<T>(stream, null, prototype);
            }

            /// <summary>
            /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
            /// </summary>
            /// <param name="stream">The stream of JSON text to parse.</param>
            /// <param name="settings">The parser settings to use.</param>
            /// <param name="prototype">The prototype for the target type.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(System.IO.Stream stream, JSONParserSettings settings, T prototype)
            {
                return Parse<T>(stream, settings);
            }

            /// <summary>
            /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the specified type.
            /// </summary>
            /// <param name="reader">The reader of JSON text to parse.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(System.IO.StreamReader reader)
            {
                return Parse<T>(reader, null);
            }

            /// <summary>
            /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the specified type.
            /// </summary>
            /// <param name="reader">The reader of JSON text to parse.</param>
            /// <param name="settings">The parser settings to use.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(System.IO.StreamReader reader, JSONParserSettings settings)
            {
                return (T)Parse(reader as object, settings, typeof(T));
            }

            /// <summary>
            /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
            /// </summary>
            /// <param name="reader">The reader of JSON text to parse.</param>
            /// <param name="prototype">The prototype for the target type.</param>
            /// <returns>The deserialized object.</returns>
            /// <summary>
            public T Parse<T>(System.IO.StreamReader reader, T prototype)
            {
                return Parse<T>(reader, null, prototype);
            }

            /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
            /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
            /// </summary>
            /// <param name="reader">The reader of JSON text to parse.</param>
            /// <param name="settings">The parser settings to use.</param>
            /// <param name="prototype">The prototype for the target type.</param>
            /// <returns>The deserialized object.</returns>
            public T Parse<T>(System.IO.StreamReader reader, JSONParserSettings settings, T prototype)
            {
                return Parse<T>(reader, settings);
            }

            public JSONParserSettings Settings { get; set; }
        }
    }
}