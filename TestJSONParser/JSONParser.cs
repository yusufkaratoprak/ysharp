using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text.Json
{
    public static class Extensions
    {
        public static T As<T>(this object obj, T prototype)
        {
            return (T)obj;
        }

        public static IDictionary<string, object> JsonObject(this object obj)
        {
            return (IDictionary<string, object>)obj;
        }

        public static IList<object> JsonArray(this object obj)
        {
            return (IList<object>)obj;
        }

        public static object FromJson(this object obj, string text)
        {
            return FromJson(obj, text, null);
        }

        public static object FromJson(this object obj, string text, Reviver<object> reviver)
        {
            return obj.FromJson<object>(text, reviver);
        }

        public static object FromJson(this object obj, System.IO.Stream stream)
        {
            return FromJson(obj, stream, null);
        }

        public static object FromJson(this object obj, System.IO.Stream stream, Reviver<object> reviver)
        {
            return obj.FromJson<object>(stream, reviver);
        }

        public static object FromJson(this object obj, System.IO.StreamReader reader)
        {
            return FromJson(obj, reader, null);
        }

        public static object FromJson(this object obj, System.IO.StreamReader reader, Reviver<object> reviver)
        {
            return obj.FromJson<object>(reader, reviver);
        }

        public static T FromJSON<T>(this T prototype, string text)
        {
            return FromJson<T>(prototype, text, null);
        }

        public static T FromJson<T>(this T prototype, string text, Reviver<object> reviver)
        {
            return new Parser().Parse(text, prototype, reviver);
        }

        public static T FromJson<T>(this T prototype, System.IO.Stream stream)
        {
            return FromJson<T>(prototype, stream, null);
        }

        public static T FromJson<T>(this T prototype, System.IO.Stream stream, Reviver<object> reviver)
        {
            return new Parser().Parse(stream, prototype, reviver);
        }

        public static T FromJson<T>(this T prototype, System.IO.StreamReader reader)
        {
            return FromJson<T>(prototype, reader, null);
        }

        public static T FromJson<T>(this T prototype, System.IO.StreamReader reader, Reviver<object> reviver)
        {
            return new Parser().Parse(reader, prototype, reviver);
        }
    }

    public delegate T Reviver<T>(Type type, string key, T value);

    public class ParserSettings
    {
        public bool AcceptIdentifiers { get; set; }
        public int LiteralsBuffer { get; set; }
    }

    public class Parser
    {
        internal class Phrase
        {
            private static readonly IDictionary<char, char> ESC = new Dictionary<char, char>();
            private const char NEXT = (char)0;
            private const int LSIZE = 4096;

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

            private static ParserSettings DefaultSettings
            {
                get
                {
                    return new ParserSettings
                    {
                        AcceptIdentifiers = false,
                        LiteralsBuffer = LSIZE
                    };
                }
            }

            internal Phrase(ParserSettings settings, object input)
            {
                config = (settings ?? DefaultSettings);
                rtti = new System.Collections.Hashtable();
                ids = config.AcceptIdentifiers;
                lsz = (((lsz = config.LiteralsBuffer) > 0) ? lsz : LSIZE);

                str = (input as System.IO.StreamReader);
                txt = (input as string);
                len = (txt ?? String.Empty).Length;
                sb = null;
                cs = new char[lsz];
                wc = new char[1];
                data = true;
                ch = ' ';
                ci = 0;
                at = 0;
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
                ch = wc[0];
                data = (r > 0);
                return true;
            }

            private bool ReadFromString(char c)
            {
                if ((c != NEXT) && (c != ch))
                    throw Error(String.Format("Expected '{0}' instead of '{1}'", c, ch));
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
                        {
                            if (a[i].Name == key)
                                break;
                        }
                        return ((i >= 0) ? (hash[key] = i) : null);
                    }
                }
                else
                    return hash[key];
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
                throw Error(String.Format("Unexpected '{0}'", ch));
            }

            private double Number()
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

            private string Literal(bool key)
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
                else
                {
                    if (key && ids)
                    {
                        if ((ch == '$') || (ch == '_') || ((ch >= 'A') && (ch <= 'Z')) || ((ch >= 'a') && (ch <= 'z')))
                            Append(ch);
                        else
                            throw Error("Bad identifier");
                        while (data ? read(NEXT) : false)
                            if ((ch == '$') || (ch == '_') || ((ch >= 'A') && (ch <= 'Z')) || ((ch >= 'a') && (ch <= 'z')))
                                Append(ch);
                            else if ((ch > ' ') && (ch != ':'))
                                throw Error("Bad identifier");
                            else
                                return ((sb != null) ? sb.ToString() : new String(cs, 0, ci));
                    }
                }
                throw Error("Bad string");
            }

            private object Object(Type type, Reviver<object> reviver)
            {
                bool obj = (type == typeof(object));
                bool isa = ((type.Name[0] == '<') && type.IsSealed);
                var ctr = (!obj ? (!isa ? type.GetConstructors().OrderBy(c => c.GetParameters().Length).First() : type.GetConstructors()[0]) : null);
                var cta = (!obj ? ctr.GetParameters() : null);
                var arg = (!obj ? new object[cta.Length] : null);
                object o = null;
                string k;
                if (ch == '{')
                {
                    var d = (obj ? new Dictionary<string, object>() : null);
                    if (!obj)
                    {
                        if (!isa)
                            o = Activator.CreateInstance(type, arg);
                    }
                    else
                        o = d;
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
                        object m;
                        k = String.Intern(Literal(true));
                        m = (!obj ? Typed((isa ? (object)cta : type), ti, k) : null);
                        while (data && (ch <= ' ')) // Spaces
                            read(NEXT);
                        if (data) read(':');
                        if ((m != null) && !obj)
                        {
                            if (!isa)
                            {
                                var p = (System.Reflection.PropertyInfo)m;
                                var v = CompileTo(p.PropertyType, reviver, true);
                                p.SetValue(o, ((reviver != null) ? reviver(type, k, v) : v), null);
                            }
                            else
                            {
                                int i = (int)m;
                                var v = CompileTo(cta[i].ParameterType, reviver, true);
                                arg[i] = ((reviver != null) ? reviver(type, k, v) : v);
                            }
                        }
                        else
                        {
                            var v = CompileTo(typeof(object), reviver, true);
                            if (obj)
                            {

                                if (d.ContainsKey(k))
                                    throw Error(String.Format("Duplicate key \"{0}\"", k));
                                d[k] = ((reviver != null) ? reviver(type, k, v) : v);
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

            private System.Collections.IEnumerable Array(Type type, Reviver<object> reviver)
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
                        l.Add(CompileTo(et, reviver, true));
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

            private object CompileTo(Type type, Reviver<object> reviver, bool parse)
            {
                type = (type ?? typeof(object));
                while (data && (ch <= ' ')) // Spaces
                    read(NEXT);
                switch (ch)
                {
                    case '{':
                        return Object(type, reviver);
                    case '[':
                        return Array(type, reviver);
                    case '"':
                        return Literal(false);
                    case '-':
                        return Number();
                    default:
                        return ((ch >= '0') && (ch <= '9') ? Number() : Word());
                }
            }

            internal object CompileTo(Type type, Reviver<object> reviver)
            {
                var obj = CompileTo(type, reviver, true);
                while (data && (ch <= ' ')) // Spaces
                    read(NEXT);
                if (data)
                    throw Error("Unexpected content");
                return obj;
            }

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
        }

        private object Parse(object input, ParserSettings settings)
        {
            return Parse(input, settings, null as Reviver<object>);
        }

        private object Parse(object input, ParserSettings settings, Reviver<object> reviver)
        {
            return Parse(input, settings, null, reviver);
        }

        private object Parse(object input, ParserSettings settings, Type type)
        {
            return Parse(input, settings, type, null);
        }

        private object Parse(object input, ParserSettings settings, Type type, Reviver<object> reviver)
        {
            return new Phrase((settings ?? Settings), input).CompileTo(type, reviver);
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
        public object Parse(string text, ParserSettings settings)
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
        public object Parse(System.IO.Stream stream, ParserSettings settings)
        {
            return Parse(stream, settings, null);
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/). This can be, either:
        /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse(System.IO.Stream stream, ParserSettings settings, Reviver<object> reviver)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                return Parse(reader, settings, reviver);
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
            return Parse(reader, null as Reviver<object>);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/). This can be, either:
        /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse(System.IO.StreamReader reader, Reviver<object> reviver)
        {
            return Parse(reader, null, reviver);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/). This can be, either:
        /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse(System.IO.StreamReader reader, ParserSettings settings)
        {
            return Parse(reader as object, settings, null as Reviver<object>);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/). This can be, either:
        /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse(System.IO.StreamReader reader, ParserSettings settings, Reviver<object> reviver)
        {
            return Parse(reader as object, settings, reviver);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(string text)
        {
            return Parse<T>(text, null as Reviver<object>);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(string text, Reviver<object> reviver)
        {
            return Parse<T>(text, null, reviver);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(string text, ParserSettings settings)
        {
            return Parse<T>(text, settings, null);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(string text, ParserSettings settings, Reviver<object> reviver)
        {
            return (T)Parse(text as object, settings, typeof(T), reviver);
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
            return Parse<T>(text, prototype, null);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(string text, T prototype, Reviver<object> reviver)
        {
            return Parse<T>(text, null, prototype, reviver);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(string text, ParserSettings settings, T prototype)
        {
            return Parse<T>(text, settings, prototype, null);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(string text, ParserSettings settings, T prototype, Reviver<object> reviver)
        {
            return (T)Parse(text as object, settings, typeof(T), reviver);
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(System.IO.Stream stream)
        {
            return Parse<T>(stream, null as Reviver<object>);
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(System.IO.Stream stream, Reviver<object> reviver)
        {
            return Parse<T>(stream, null, reviver);
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(System.IO.Stream stream, ParserSettings settings)
        {
            return Parse<T>(stream, settings, null);
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(System.IO.Stream stream, ParserSettings settings, Reviver<object> reviver)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                return (T)Parse(reader as object, settings, typeof(T), reviver);
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
            return Parse<T>(stream, prototype, null);
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(System.IO.Stream stream, T prototype, Reviver<object> reviver)
        {
            return Parse<T>(stream, null, prototype, reviver);
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(System.IO.Stream stream, ParserSettings settings, T prototype)
        {
            return Parse<T>(stream, settings, prototype, null);
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(System.IO.Stream stream, ParserSettings settings, T prototype, Reviver<object> reviver)
        {
            return Parse<T>(stream, settings, reviver);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(System.IO.StreamReader reader)
        {
            return Parse<T>(reader, null as Reviver<object>);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(System.IO.StreamReader reader, Reviver<object> reviver)
        {
            return Parse<T>(reader, null, reviver);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(System.IO.StreamReader reader, ParserSettings settings)
        {
            return Parse<T>(reader, settings, null);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(System.IO.StreamReader reader, ParserSettings settings, Reviver<object> reviver)
        {
            return (T)Parse(reader as object, settings, typeof(T), reviver);
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
            return Parse<T>(reader, prototype, null);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        /// <summary>
        public T Parse<T>(System.IO.StreamReader reader, T prototype, Reviver<object> reviver)
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
        public T Parse<T>(System.IO.StreamReader reader, ParserSettings settings, T prototype)
        {
            return Parse<T>(reader, settings, prototype, null);
        }

        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <param name="reviver">The reviver to use.</param>
        /// <returns>The deserialized object.</returns>
        public T Parse<T>(System.IO.StreamReader reader, ParserSettings settings, T prototype, Reviver<object> reviver)
        {
            return (T)Parse(reader as object, settings, typeof(T), reviver);
        }

        public ParserSettings Settings { get; set; }
    }
}