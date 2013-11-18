using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text.Json
{
    public static class Extensions
    {
        public static T As<T>(this object obj)
        {
            return (T)obj;
        }

        public static T As<T>(this object obj, T prototype)
        {
            return (T)obj;
        }

        public static object FromJson(this object obj, string text)
        {
            return obj.FromJson<object>(text);
        }

        public static object FromJson(this object obj, string text, params Reviver[] revivers)
        {
            return obj.FromJson<object>(text, revivers);
        }

        public static object FromJson(this object obj, System.IO.Stream stream)
        {
            return obj.FromJson<object>(stream);
        }

        public static object FromJson(this object obj, System.IO.Stream stream, params Reviver[] revivers)
        {
            return obj.FromJson<object>(stream, revivers);
        }

        public static object FromJson(this object obj, System.IO.StreamReader reader)
        {
            return obj.FromJson<object>(reader);
        }

        public static object FromJson(this object obj, System.IO.StreamReader reader, params Reviver[] revivers)
        {
            return obj.FromJson<object>(reader, revivers);
        }

        public static object FromJson<T>(this T prototype, string text, Parse value)
        {
            return new Parser().Parse(text, prototype);
        }

        public static T As<T>(this Parse value, string text)
        {
            return default(T).FromJson(text);
        }

        public static T FromJson<T>(this T prototype, string text)
        {
            return (T)new Parser().Parse(text, prototype);
        }

        public static object FromJson<T>(this T prototype, string text, Parse value, params Reviver[] revivers)
        {
            return new Parser().Parse(text, prototype, revivers);
        }

        public static T As<T>(this Parse value, string text, params Reviver[] revivers)
        {
            return default(T).FromJson(text, revivers);
        }

        public static T FromJson<T>(this T prototype, string text, params Reviver[] revivers)
        {
            return (T)new Parser().Parse(text, prototype, revivers);
        }

        public static object FromJson<T>(this T prototype, System.IO.Stream stream, Parse value)
        {
            return new Parser().Parse(stream, prototype);
        }

        public static T As<T>(this Parse value, System.IO.Stream stream)
        {
            return default(T).FromJson(stream);
        }

        public static T FromJson<T>(this T prototype, System.IO.Stream stream)
        {
            return (T)new Parser().Parse(stream, prototype);
        }

        public static object FromJson<T>(this T prototype, System.IO.Stream stream, Parse value, params Reviver[] revivers)
        {
            return new Parser().Parse(stream, prototype, revivers);
        }

        public static T As<T>(this Parse value, System.IO.Stream stream, params Reviver[] revivers)
        {
            return default(T).FromJson(stream, revivers);
        }

        public static T FromJson<T>(this T prototype, System.IO.Stream stream, params Reviver[] revivers)
        {
            return (T)new Parser().Parse(stream, prototype, revivers);
        }

        public static object FromJson<T>(this T prototype, System.IO.StreamReader reader, Parse value)
        {
            return new Parser().Parse(reader, prototype);
        }

        public static T As<T>(this Parse value, System.IO.StreamReader reader)
        {
            return default(T).FromJson(reader);
        }

        public static T FromJson<T>(this T prototype, System.IO.StreamReader reader)
        {
            return (T)new Parser().Parse(reader, prototype);
        }

        public static object FromJson<T>(this T prototype, System.IO.StreamReader reader, Parse value, params Reviver[] revivers)
        {
            return new Parser().Parse(reader, prototype, revivers);
        }

        public static T As<T>(this Parse value, System.IO.StreamReader reader, params Reviver[] revivers)
        {
            return default(T).FromJson(reader, revivers);
        }

        public static T FromJson<T>(this T prototype, System.IO.StreamReader reader, params Reviver[] revivers)
        {
            return (T)new Parser().Parse(reader, prototype, revivers);
        }

        public static IDictionary<string, object> JsonObject(this object obj)
        {
            return (IDictionary<string, object>)obj;
        }

        public static IList<object> JsonArray(this object obj)
        {
            return (IList<object>)obj;
        }
    }

    public delegate Func<object> Reviver(Type target, Type type, object key, object value);

    public class ParserSettings
    {
        public bool AcceptIdentifiers { get; set; }
        public int LiteralsBuffer { get; set; }
    }

    public enum Parse
    {
        Value
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

            private static ParserSettings DefaultSettings
            {
                get
                {
                    return new ParserSettings
                    {
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
                return new Exception(String.Format("{0} at offset {1} ('{2}')", message, at, ch));
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

            private Func<object> Map(Reviver[] revivers, Type t, Type m, object k, object v)
            {
                Func<object> mapper = null;
                if (revivers != null)
                    for (int i = 0; i < revivers.Length; i++)
                        if ((mapper = revivers[i](t, m, k, v)) != null)
                            break;
                return mapper;
            }

            private object Word(Type target, params Reviver[] revivers)
            {
                Func<object> mapped;
                switch (ch)
                {
                    case 't':
                        if (data) read('t');
                        if (data) read('r');
                        if (data) read('u');
                        if (data) read('e');
                        mapped = Map(revivers, target, typeof(bool), null, true);
                        return ((mapped != null) ? mapped() : true);
                    case 'f':
                        if (data) read('f');
                        if (data) read('a');
                        if (data) read('l');
                        if (data) read('s');
                        if (data) read('e');
                        mapped = Map(revivers, target, typeof(bool), null, false);
                        return ((mapped != null) ? mapped() : false);
                    case 'n':
                        if (data) read('n');
                        if (data) read('u');
                        if (data) read('l');
                        if (data) read('l');
                        mapped = Map(revivers, target, typeof(object), null, null);
                        return ((mapped != null) ? mapped() : null);
                }
                throw Error(String.Format("Unexpected '{0}'", ch));
            }

            private object Number(Type target, params Reviver[] revivers)
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
                n = double.Parse((sb != null) ? sb.ToString() : new String(cs, 0, ci));
                var mapped = Map(revivers, target, typeof(double), null, n);
                return ((mapped != null) ? mapped() : n);
            }

            private object Literal(Type target, bool key, params Reviver[] revivers)
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
                            var mapped = Map(revivers, target, typeof(string), (key ? (object)key : null), s);
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
                            {
                                s = ((sb != null) ? sb.ToString() : new String(cs, 0, ci));
                                var mapped = Map(revivers, target, typeof(string), (key ? (object)key : null), s);
                                return ((mapped != null) ? mapped() : s);
                            }
                    }
                }
                throw Error("Bad string");
            }

            private bool Inherits(Type given, Type generic)
            {
                var itfs = given.GetInterfaces();
                foreach (var it in itfs)
                    if (it.IsGenericType && it.GetGenericTypeDefinition() == generic)
                        return true;
                if (given.IsGenericType && given.GetGenericTypeDefinition() == generic)
                    return true;
                if (given.BaseType == null)
                    return false;
                return Inherits(given.BaseType, generic);
            }

            private object Object(Type type, params Reviver[] revivers)
            {
                Func<Type, bool> dit = (t) => (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IDictionary<,>)));
                bool obj = ((type = (type ?? typeof(object))) == typeof(object));
                bool ish = (!obj && typeof(System.Collections.IDictionary).IsAssignableFrom(type));
                Type did = (!obj && !ish && type.IsGenericType && Inherits(type, typeof(IDictionary<,>)) ? type : null);
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
                string k;
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
                        object h = Literal(type, true, revivers), m;
                        if (!dyn && ((h as string) == null))
                            throw Error("Bad key");
                        k = (!dyn ? String.Intern((string)h) : null);
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
                                var v = CompileTo(t, true, revivers);
                                var mapped = Map(revivers, type, t, k, v);
                                p.SetValue(o, ((mapped != null) ? mapped() : v), null);
                            }
                            else
                            {
                                int i = (int)m;
                                var t = cta[i].ParameterType;
                                var v = CompileTo(t, true, revivers);
                                var mapped = Map(revivers, type, t, k, v);
                                arg[i] = ((mapped != null) ? mapped() : v);
                            }
                        }
                        else
                        {
                            var v = CompileTo(dvt, true, revivers);
                            if (dyn)
                            {
                                var mapped = Map(revivers, dkt, dvt, true, h);
                                h = ((mapped != null) ? mapped() : h);
                                if (d.Contains(h))
                                    throw Error(String.Format("Duplicate key \"{0}\"", h));
                                mapped = Map(revivers, dkt, dvt, h, v);
                                v = ((mapped != null) ? mapped() : v);
                                if (v != d)
                                    d[h] = v;
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

            private System.Collections.IEnumerable Array(Type type, params Reviver[] revivers)
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
                        l.Add(CompileTo(et, true, revivers));
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

            private object CompileTo(Type type, bool parse, params Reviver[] revivers)
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

            internal object CompileTo(Type type, params Reviver[] revivers)
            {
                var obj = CompileTo(type, true, revivers);
                while (data && (ch <= ' ')) // Spaces
                    read(NEXT);
                if (data)
                    throw Error("Unexpected content");
                var mapped = Map(revivers, type, null, null, obj);
                return ((mapped != null) ? mapped() : obj);
            }
        }

        private object Parse(object input, ParserSettings settings, Type type, params Reviver[] revivers)
        {
            revivers = (revivers ?? new Reviver[0]);
            return new Phrase((settings ?? Settings), input).CompileTo(type, revivers);
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
            return Parse(text as object, null, null, null);
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
            return Parse(text as object, settings, null, null);
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
                return Parse(reader as object, null, null, null);
            }
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
                return Parse(reader as object, settings, null, null);
            }
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/). This can be, either:
        /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse(System.IO.Stream stream, ParserSettings settings, params Reviver[] revivers)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                return Parse(reader as object, settings, null, revivers);
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
            return Parse(reader as object, null, null, null);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/). This can be, either:
        /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse(System.IO.StreamReader reader, params Reviver[] revivers)
        {
            return Parse(reader as object, null, null, revivers);
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
            return Parse(reader as object, settings, null, null);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/). This can be, either:
        /// null, or true/false, or a System.Double, or a System.String, or an IList(object), or an IDictionary(string, object).
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse(System.IO.StreamReader reader, ParserSettings settings, params Reviver[] revivers)
        {
            return Parse(reader as object, settings, null, revivers);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(string text)
        {
            return Parse(text as object, null, null, null);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(string text, params Reviver[] revivers)
        {
            return Parse(text as object, null, null, revivers);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(string text, ParserSettings settings)
        {
            return Parse(text as object, settings, null, null);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(string text, ParserSettings settings, params Reviver[] revivers)
        {
            return Parse(text as object, settings, typeof(T), revivers);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(string text, T prototype)
        {
            return Parse(text as object, null, typeof(T), null);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(string text, T prototype, params Reviver[] revivers)
        {
            return Parse(text as object, null, typeof(T), revivers);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(string text, ParserSettings settings, T prototype)
        {
            return Parse(text as object, settings, typeof(T), null);
        }

        /// <summary>
        /// Converts the specified JSON text string to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="text">The string of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(string text, ParserSettings settings, T prototype, params Reviver[] revivers)
        {
            return Parse(text as object, settings, typeof(T), revivers);
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.Stream stream)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                return Parse(reader as object, null, typeof(T), null);
            }
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.Stream stream, params Reviver[] revivers)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                return Parse(reader as object, null, typeof(T), revivers);
            }
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.Stream stream, ParserSettings settings)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                return Parse(reader as object, settings, typeof(T), null);
            }
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.Stream stream, ParserSettings settings, params Reviver[] revivers)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                return Parse(reader as object, settings, typeof(T), revivers);
            }
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.Stream stream, T prototype)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                return Parse(reader as object, null, typeof(T), null);
            }
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.Stream stream, T prototype, params Reviver[] revivers)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                return Parse(reader as object, null, typeof(T), revivers);
            }
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.Stream stream, ParserSettings settings, T prototype)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                return Parse(reader as object, settings, typeof(T), null);
            }
        }

        /// <summary>
        /// Converts the specified JSON text stream to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="stream">The stream of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.Stream stream, ParserSettings settings, T prototype, params Reviver[] revivers)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                return Parse(reader as object, settings, typeof(T), revivers);
            }
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.StreamReader reader)
        {
            return Parse(reader as object, null, typeof(T), null);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.StreamReader reader, params Reviver[] revivers)
        {
            return Parse(reader as object, null, typeof(T), revivers);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.StreamReader reader, ParserSettings settings)
        {
            return Parse(reader as object, settings, typeof(T), null);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the specified type.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.StreamReader reader, ParserSettings settings, params Reviver[] revivers)
        {
            return Parse(reader as object, settings, typeof(T), revivers);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <returns>The deserialized object.</returns>
        /// <summary>
        public object Parse<T>(System.IO.StreamReader reader, T prototype)
        {
            return Parse(reader as object, null, typeof(T), null);
        }

        /// <summary>
        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        /// <summary>
        public object Parse<T>(System.IO.StreamReader reader, T prototype, params Reviver[] revivers)
        {
            return Parse(reader as object, null, typeof(T), revivers);
        }

        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.StreamReader reader, ParserSettings settings, T prototype)
        {
            return Parse(reader as object, settings, typeof(T), null);
        }

        /// Converts the specified JSON text reader to its .NET equivalent of the JSON "value"
        /// (as defined by http://json.org/), assignment-compatible with the anonymous type of the specified prototype.
        /// </summary>
        /// <param name="reader">The reader of JSON text to parse.</param>
        /// <param name="settings">The parser settings to use.</param>
        /// <param name="prototype">The prototype for the target type.</param>
        /// <param name="revivers">The revivers to use.</param>
        /// <returns>The deserialized object.</returns>
        public object Parse<T>(System.IO.StreamReader reader, ParserSettings settings, T prototype, params Reviver[] revivers)
        {
            return Parse(reader as object, settings, typeof(T), revivers);
        }

        public ParserSettings Settings { get; set; }
    }
}