using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text.JSON
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

        public static object FromJSON(this object obj, string text)
        {
            return FromJSON(obj, null, text);
        }

        public static object FromJSON(this object obj, ParserSettings settings, string text)
        {
            return obj.FromJSON<object>(settings, text);
        }

        public static object FromJSON(this object obj, System.IO.Stream stream)
        {
            return FromJSON(obj, null, stream);
        }

        public static object FromJSON(this object obj, ParserSettings settings, System.IO.Stream stream)
        {
            return obj.FromJSON<object>(settings, stream);
        }

        public static object FromJSON(this object obj, System.IO.StreamReader reader)
        {
            return FromJSON(obj, null, reader);
        }

        public static object FromJSON(this object obj, ParserSettings settings, System.IO.StreamReader reader)
        {
            return obj.FromJSON<object>(settings, reader);
        }

        public static T FromJSON<T>(this T prototype, string text)
        {
            return FromJSON<T>(prototype, null, text);
        }

        public static T FromJSON<T>(this T prototype, ParserSettings settings, string text)
        {
            return new Parser().Parse(text, settings, prototype);
        }

        public static T FromJSON<T>(this T prototype, System.IO.Stream stream)
        {
            return FromJSON<T>(prototype, null, stream);
        }

        public static T FromJSON<T>(this T prototype, ParserSettings settings, System.IO.Stream stream)
        {
            return new Parser().Parse(stream, settings, prototype);
        }

        public static T FromJSON<T>(this T prototype, System.IO.StreamReader reader)
        {
            return FromJSON<T>(prototype, null, reader);
        }

        public static T FromJSON<T>(this T prototype, ParserSettings settings, System.IO.StreamReader reader)
        {
            return new Parser().Parse(reader, settings, prototype);
        }
    }

    public class ParserSettings
    {
        public bool AcceptIdentifiers { get; set; }
        public int LiteralsBuffer { get; set; }
    }

    public class Parser
    {
        internal class JSONPhrase
        {
            private static readonly IDictionary<char, char> ESC = new Dictionary<char, char>();
            private const char NEXT = (char)0;
            private const int LSIZE = 4096;

            private ParserSettings config;
            private System.Collections.Hashtable rti;
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

            internal JSONPhrase(ParserSettings settings, object source)
            {
                config = (settings ?? DefaultSettings);

                ids = config.AcceptIdentifiers;
                lsz = (((lsz = config.LiteralsBuffer) > 0) ? lsz : LSIZE);
                rti = new System.Collections.Hashtable();
                str = (source as System.IO.StreamReader);
                txt = (source as string);
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

            private System.Collections.Hashtable Known(Type t)
            {
                return (System.Collections.Hashtable)(!rti.ContainsKey(t) ? (rti[t] = new System.Collections.Hashtable()) : rti[t]);
            }

            private object Typed(object o, System.Collections.Hashtable h, string k)
            {
                if (!h.ContainsKey(k))
                {
                    if (o is Type)
                    {
                        var p = ((Type)o).GetProperty(k);
                        return ((p != null) ? (h[k] = p) : null);
                    }
                    else
                    {
                        var a = (System.Reflection.ParameterInfo[])o;
                        int i = a.Length;
                        while (--i >= 0)
                        {
                            if (a[i].Name == k)
                                break;
                        }
                        return ((i >= 0) ? (h[k] = i) : null);
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
                throw Error(String.Format("Unexpected '{0}'", ch));
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

            private string Str(bool k)
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
                    if (k && ids)
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

            private object Obj(Type t)
            {
                bool obj = (t == typeof(object));
                bool isa = ((t.Name[0] == '<') && t.IsSealed);
                var ctr = (!obj ? (!isa ? t.GetConstructors().OrderBy(c => c.GetParameters().Length).First() : t.GetConstructors()[0]) : null);
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
                            o = Activator.CreateInstance(t, arg);
                    }
                    else
                        o = d;
                    var ti = (!obj ? Known(t) : null);
                    if (data) read('{');
                    while (data && (ch <= ' ')) // Spaces
                        read(NEXT);
                    if (ch == '}')
                    {
                        if (data) read('}');
                        return (isa ? Activator.CreateInstance(t, arg) : o);
                    }
                    while (data)
                    {
                        object m;
                        k = String.Intern(Str(true));
                        m = (!obj ? Typed((isa ? (object)cta : t), ti, k) : null);
                        while (data && (ch <= ' ')) // Spaces
                            read(NEXT);
                        if (data) read(':');
                        if ((m != null) && !obj)
                        {
                            if (!isa)
                            {
                                var p = (System.Reflection.PropertyInfo)m;
                                p.SetValue(o, CompileTo(p.PropertyType, true), null);
                            }
                            else
                            {
                                int i = (int)m;
                                arg[i] = CompileTo(cta[i].ParameterType, true);
                            }
                        }
                        else
                        {
                            var p = CompileTo(typeof(object), true);
                            if (obj)
                            {

                                if (d.ContainsKey(k))
                                    throw Error(String.Format("Duplicate key \"{0}\"", k));
                                d[k] = p;
                            }
                        }
                        while (data && (ch <= ' ')) // Spaces
                            read(NEXT);
                        if (ch == '}')
                        {
                            if (data) read('}');
                            return (isa ? Activator.CreateInstance(t, arg) : o);
                        }
                        if (data) read(',');
                        while (data && (ch <= ' ')) // Spaces
                            read(NEXT);
                    }
                }
                throw Error("Bad object");
            }

            private System.Collections.IEnumerable Arr(Type t)
            {
                var isa = t.IsArray;
                var ie = (isa || (t.GetInterfaces().Where(i => typeof(System.Collections.IEnumerable).IsAssignableFrom(i)).FirstOrDefault() != null));
                var et = (ie ? (isa ? t.GetElementType() : t.GetGenericArguments()[0]) : null);
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
                        l.Add(CompileTo(et, true));
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

            private object CompileTo(Type type, bool parse)
            {
                type = (type ?? typeof(object));
                while (data && (ch <= ' ')) // Spaces
                    read(NEXT);
                switch (ch)
                {
                    case '{':
                        return Obj(type);
                    case '[':
                        return Arr(type);
                    case '"':
                        return Str(false);
                    case '-':
                        return Num();
                    default:
                        return ((ch >= '0') && (ch <= '9') ? Num() : Word());
                }
            }

            internal object CompileTo(Type type)
            {
                var obj = CompileTo(type, true);
                while (data && (ch <= ' ')) // Spaces
                    read(NEXT);
                if (data)
                    throw Error("Unexpected content");
                return obj;
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

        private object Parse(object source, ParserSettings settings)
        {
            return Parse(source, settings, null);
        }

        private object Parse(object source, ParserSettings settings, Type type)
        {
            return new JSONPhrase((settings ?? Settings), source).CompileTo(type);
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
        public object Parse(System.IO.StreamReader reader, ParserSettings settings)
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
        public T Parse<T>(string text, ParserSettings settings)
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
        public T Parse<T>(string text, ParserSettings settings, T prototype)
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
        public T Parse<T>(System.IO.Stream stream, ParserSettings settings)
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
        public T Parse<T>(System.IO.Stream stream, ParserSettings settings, T prototype)
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
        public T Parse<T>(System.IO.StreamReader reader, ParserSettings settings)
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
        public T Parse<T>(System.IO.StreamReader reader, ParserSettings settings, T prototype)
        {
            return Parse<T>(reader, settings);
        }

        public ParserSettings Settings { get; set; }
    }
}