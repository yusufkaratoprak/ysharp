/* Copyright (c) 2009, 2010 Cyril Jandia
 * 
 * http://www.ysharp.net/the.language/
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
from Cyril Jandia. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Microsoft.CSharp;

namespace System.Languages
{
    public delegate Select Construct();
    public delegate Select Amendment(Select let);
    public delegate TValue Parse<TValue>(ParseContext context, int offset);
    public delegate TValue Select<TValue>(string select, Value value)
        where TValue : Value, new();

    public sealed class Select : Dictionary<int, Value>
    {
        internal Language language;
        private bool empty;
        private string name;
        private object definition;
        private bool legacy;

        internal Select(Language language)
            : this(language, "legacy", null as object, true)
        {
        }

        private Select(Language language, string name)
            : this(language, name, null as object)
        {
        }

        private Select(Language language, string name, string literal)
            : this(language, name, literal as object)
        {
        }

        private Select(Language language, string name, Regex regex)
            : this(language, name, regex as object)
        {
        }

        private Select(Language language, string name, Parse<Value> syntax)
            : this(language, name, syntax as object)
        {
        }

        internal Select(Language language, string name, object definition)
            : this(language, name, definition, false)
        {
        }

        private Select(Language language, string name, object definition, bool legacy)
            : this(false, language, name, definition, legacy)
        {
        }

        internal Select(bool empty, Language language, string name, object definition)
            : this(empty, language, name, definition, false)
        {
        }

        private Select(bool empty, Language language, string name, object definition, bool legacy)
            : base()
        {
            if (language == null)
                throw new ArgumentNullException("language", "cannot be null");
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException("name", "cannot be null or empty");
            this.empty = empty;
            this.language = language;
            this.name = name;
            this.legacy = legacy;
            if (!legacy)
            {
                if ((language.Syntax == null) || !IsEmpty)
                {
                    DefineWith(definition);
                    language.Definition[name] = this;
                }
            }
        }

        private void Validate(object definition)
        {
            if (!typeof(Parse<Value>).IsAssignableFrom((definition != null) ? definition.GetType() : typeof(Parse<Value>)) && String.IsNullOrEmpty(definition as string) && !(definition is Regex))
                throw new ArgumentOutOfRangeException("definition", "must be a non-empty string literal, a regular expression, or a syntax rule");
        }

        private Select Literal(string name, string expression, int regexOptions)
        {
            if (regexOptions >= 0)
                return new Select(language, !String.IsNullOrEmpty(name) ? String.Concat("@", name) : String.Concat("Regex@", language.Sid), new Regex(expression, (RegexOptions)(regexOptions | (int)RegexOptions.Compiled)));
            else
                return new Select(language, !String.IsNullOrEmpty(name) ? String.Concat("@", name) : String.Concat("Token@", language.Sid), expression as string);
        }

        internal bool Knows(int offset, out Value cached)
        {
            cached = null;
            if (ContainsKey(offset))
            {
                cached = this[offset];
                return (cached != null);
            }
            return false;
        }

        private void DefineWith(object definition)
        {
            Validate(definition);
            this.definition = definition;
        }

        public Select Token(string value)
        {
            return Token(null, value);
        }

        public Select Token(string name, string value)
        {
            return Literal(name, value, -1);
        }

        public Select Regex(string pattern)
        {
            return Regex(pattern, RegexOptions.None);
        }

        public Select Regex(string pattern, RegexOptions options)
        {
            return Regex(null, pattern, options);
        }

        public Select Regex(string name, string pattern)
        {
            return Regex(name, pattern, RegexOptions.None);
        }

        public Select Regex(string name, string pattern, RegexOptions options)
        {
            return Literal(name, pattern, (int)options);
        }

        public Select End()
        {
            return End(null as string);
        }

        public Select End(Select<Value> action)
        {
            return End(null, action);
        }

        public Select End(string name)
        {
            return End(name, null);
        }

        public Select End(string name, Select<Value> action)
        {
            Parse<Value> syntax = delegate(ParseContext context, int offset)
            {
                action = (action ?? context.Language.semantic);
                if (offset >= context.Source.Length)
                {
                    Value match = new Value(true, context.Source.Length, 0, !String.IsNullOrEmpty(name) ? String.Concat("@", name) : name);
                    return ((action != null) ? (action(name, match) ?? match) : match);
                }
                else
                    return null;
            };
            return new Select(language, !String.IsNullOrEmpty(name) ? name : String.Concat("R", language.Sid), syntax);
        }

        public Select Start()
        {
            return Start(null as string);
        }

        public Select Start(Select<Value> action)
        {
            return Start(null, action);
        }

        public Select Start(string name)
        {
            return Start(name, null);
        }

        public Select Start(string name, Select<Value> action)
        {
            Parse<Value> syntax = delegate(ParseContext context, int offset)
            {
                action = (action ?? context.Language.semantic);
                if (offset == 0)
                {
                    Value match = new Value(0, (context.Source.Length > 0) ? 1 : 0, !String.IsNullOrEmpty(name) ? String.Concat("@", name) : name, (context.Source.Length > 0) ? context.Source.Substring(0, 1) : null);
                    return ((action != null) ? (action(name, match) ?? match) : match);
                }
                else
                    return null;
            };
            return new Select(language, !String.IsNullOrEmpty(name) ? name : String.Concat("R", language.Sid), syntax);
        }

        public Select Seq(params Select[] patterns)
        {
            return Seq(false, patterns);
        }

        public Select Seq(bool asToken, params Select[] patterns)
        {
            return Seq(null, asToken, null, patterns);
        }

        public Select Seq(Select<Value> action, params Select[] patterns)
        {
            return Seq(null, action, patterns);
        }

        public Select Seq(string name, params Select[] patterns)
        {
            return Seq(name, false, null, patterns);
        }

        public Select Seq(string name, bool asToken, params Select[] patterns)
        {
            return Seq(name, asToken, null, patterns);
        }

        public Select Seq(string name, Select<Value> action, params Select[] patterns)
        {
            return Seq(name, false, action, patterns);
        }

        public Select Seq(string name, bool asToken, Select<Value> action, params Select[] patterns)
        {
            return Seq(name, asToken, action, false, patterns);
        }

        private Select Seq(string name, bool asToken, Select<Value> action, bool empty, params Select[] patterns)
        {
            if (patterns.Length < 1)
                throw new ArgumentOutOfRangeException("patterns", "length must be greater than zero");
            Parse<Value> syntax = delegate(ParseContext context, int offset)
            {
                action = (action ?? context.Language.semantic);
                int length = 0;
                IList<Value> matches = new List<Value>();
                foreach (var pattern in patterns)
                {
                    Value match = context.Language.Parse(context, pattern.Name, offset + length);
                    if (match == null)
                        return null;
                    matches.Add(match);
                    length += match.Length;
                }
                Value value = new Value(offset, length, (!String.IsNullOrEmpty(name) ? String.Concat(asToken ? "@" : String.Empty, name) : String.Concat(asToken ? "@" : String.Empty, patterns[0].Ident)), null, matches.ToArray());
                return ((action != null) ? (action(name, value) ?? value) : value);
            };
            return new Select(empty, language, !String.IsNullOrEmpty(name) ? name : String.Concat("R", language.Sid), syntax);
        }

        public Select Any(Select pattern)
        {
            return Any(pattern, null);
        }

        public Select Any(Select pattern, Select<Value> action)
        {
            return Any(null, pattern, action);
        }

        public Select Any(string name, Select pattern)
        {
            return Any(name, pattern, null);
        }

        public Select Any(string name, Select pattern, Select<Value> action)
        {
            return Any(name, pattern, action, false);
        }

        private Select Any(string name, Select pattern, Select<Value> action, bool oneOrMore)
        {
            Parse<Value> syntax = delegate(ParseContext context, int offset)
            {
                action = (action ?? context.Language.semantic);
                int length = 0;
                IList<Value> matches = new List<Value>();
                Value match = context.Language.Parse(context, pattern.Name, offset);
                if ((match == null) && oneOrMore)
                    return null;
                while (match != null)
                {
                    matches.Add(match);
                    if ((offset + length + match.Length) < (context.Source.Length - 1))
                        length += ((match.Length > 0) ? match.Length : 1);
                    else
                        break;
                    match = context.Language.Parse(context, pattern.Name, offset + length);
                }
                match = new Value(offset, length, (!String.IsNullOrEmpty(name) ? String.Concat("@", name) : String.Concat("@", pattern.Ident)), null, matches.ToArray());
                return ((action != null) ? (action(name, match) ?? match) : match);
            };
            return new Select(language, !String.IsNullOrEmpty(name) ? name : String.Concat("R", language.Sid), syntax);
        }

        public Select Some(Select pattern)
        {
            return Some(pattern, null);
        }

        public Select Some(Select pattern, Select<Value> action)
        {
            return Some(null, pattern, action);
        }

        public Select Some(string name, Select pattern)
        {
            return Some(name, pattern, null);
        }

        public Select Some(string name, Select pattern, Select<Value> action)
        {
            return Any(name, pattern, action, true);
        }

        public Select Opt(Select pattern)
        {
            return Opt(pattern, null);
        }

        public Select Opt(Select pattern, Select<Value> action)
        {
            return Opt(null, pattern, action);
        }

        public Select Opt(string name, Select pattern)
        {
            return Opt(name, pattern, null);
        }

        public Select Opt(string name, Select pattern, Select<Value> action)
        {
            Parse<Value> syntax = delegate(ParseContext context, int offset)
            {
                action = (action ?? context.Language.semantic);
                Value match = context.Language.Parse(context, pattern.Name, offset);
                if (match == null)
                    match = new Value(offset, 0, !String.IsNullOrEmpty(name) ? String.Concat("@", name) : String.Concat("@", pattern.Ident));
                return ((action != null) ? (action(name, match) ?? match) : match);
            };
            return new Select(language, !String.IsNullOrEmpty(name) ? name : String.Concat("R", language.Sid), syntax);
        }

        public Select Or(params Select[] patterns)
        {
            return Or(null as string, patterns);
        }

        public Select Or(Select<Value> action, params Select[] patterns)
        {
            return Or(null, action, patterns);
        }

        public Select Or(string name, params Select[] patterns)
        {
            return Or(name, null, patterns);
        }

        public Select Or(string name, Select<Value> action, params Select[] patterns)
        {
            if (patterns.Length < 1)
                throw new ArgumentOutOfRangeException("patterns", "length must be greater than zero");
            Parse<Value> syntax = delegate(ParseContext context, int offset)
            {
                action = (action ?? context.Language.semantic);
                foreach (var pattern in patterns)
                {
                    Value match = context.Language.Parse(context, pattern.Name, offset);
                    if (match != null)
                        return ((action != null) ? (action(name, match) ?? match) : match);
                }
                return null;
            };
            return new Select(language, !String.IsNullOrEmpty(name) ? name : String.Concat("R", language.Sid), syntax);
        }

        public Select And(Select pattern)
        {
            return And(pattern, null);
        }

        public Select And(Select pattern, Select<Value> action)
        {
            return And(null, pattern, action);
        }

        public Select And(string name, Select pattern)
        {
            return And(name, pattern, null);
        }

        public Select And(string name, Select pattern, Select<Value> action)
        {
            Parse<Value> syntax = delegate(ParseContext context, int offset)
            {
                action = (action ?? context.Language.semantic);
                Value match = context.Language.Parse(context, pattern.Name, offset);
                if (match != null)
                {
                    match = new Value(offset, match.Length, !String.IsNullOrEmpty(name) ? String.Concat("@", name) : String.Concat("@", pattern.Ident));
                    return ((action != null) ? (action(name, match) ?? match) : match);
                }
                else
                    return null;
            };
            return new Select(language, !String.IsNullOrEmpty(name) ? name : String.Concat("R", language.Sid), syntax);
        }

        public Select Not(Select pattern)
        {
            return Not(pattern, null);
        }

        public Select Not(Select pattern, Select<Value> action)
        {
            return Not(null, pattern, action);
        }

        public Select Not(string name, Select pattern)
        {
            return Not(name, pattern, null);
        }

        public Select Not(string name, Select pattern, Select<Value> action)
        {
            Parse<Value> syntax = delegate(ParseContext context, int offset)
            {
                action = (action ?? context.Language.semantic);
                Value match = context.Language.Parse(context, pattern.Name, offset);
                if (match == null)
                {
                    match = new Value(offset, 0, !String.IsNullOrEmpty(name) ? String.Concat("@", name) : String.Concat("@", pattern.Ident));
                    return ((action != null) ? (action(name, match) ?? match) : match);
                }
                else
                    return null;
            };
            return new Select(language, !String.IsNullOrEmpty(name) ? name : String.Concat("R", language.Sid), syntax);
        }

        public Select Expect(Select pattern)
        {
            return Expect(pattern, null);
        }

        public Select Expect(Select pattern, Select<Value> action)
        {
            return Expect(null, pattern, action);
        }

        public Select Expect(string name, Select pattern)
        {
            return Expect(name, pattern, null);
        }

        public Select Expect(string name, Select pattern, Select<Value> action)
        {
            Parse<Value> syntax = delegate(ParseContext context, int offset)
            {
                action = (action ?? context.Language.semantic);
                Value match = context.Language.Parse(context, pattern.Name, offset);
                if (match == null)
                    match = new Value(offset >= context.Source.Length, offset, (offset < context.Source.Length) ? 1 : 0, String.Concat("expected:", !String.IsNullOrEmpty(name) ? name : pattern.Ident));
                return ((action != null) ? (action(name, match) ?? match) : match);
            };
            return new Select(language, !String.IsNullOrEmpty(name) ? name : String.Concat("R", language.Sid), syntax);
        }

        public Select Error(Select pattern)
        {
            return Error(pattern, null);
        }

        public Select Error(Select pattern, Select<Value> action)
        {
            return Error(null, pattern, action);
        }

        public Select Error(string tag, Select pattern)
        {
            return Error(tag, pattern, null);
        }

        public Select Error(string tag, Select pattern, Select<Value> action)
        {
            Parse<Value> syntax = delegate(ParseContext context, int offset)
            {
                action = (action ?? context.Language.semantic);
                Value match = context.Language.Parse(context, pattern.Name, offset);
                if (match != null)
                    match = new Value(offset, match.Length, String.Concat(tag, ":", pattern.Ident), match.Literal);
                else
                    return null;
                return ((action != null) ? (action(name, match) ?? match) : match);
            };
            return new Select(language, !String.IsNullOrEmpty(name) ? name : String.Concat("R", language.Sid), syntax);
        }

        internal Select Accept(Select syntax)
        {
            if (language.Syntax == null)
                language.Syntax = syntax;
            return ((language.Accept == null) ? Expect("language", syntax) : language.Accept);
        }

        public Select Empty()
        {
            if (this.language.Syntax == null)
                return Seq("syntax", false, null, true, End());
            else
                return null;
        }

        public Language Language { get { return language; } }

        public Select this[string name]
        {
            get
            {
                return (language.Definition.ContainsKey(name) ? language.Definition[name] : new Select(language, name, null as Parse<Value>));
            }
        }

        public bool IsEmpty { get { return empty; } }

        public bool IsLegacy { get { return legacy; } }

        public Select Legacy { get { return (legacy ? this : language.Chain); } }

        public string Ident { get { return Name.TrimStart('@'); } }

        public string Name { get { return name; } }

        public object Definition { get { return definition; } }

        public bool IsLiteral { get { return (Definition is string); } }

        public bool IsRegex { get { return (Definition is Regex); } }

        public string AsLiteral { get { return (Definition as string); } }

        public Regex AsRegex { get { return (Definition as Regex); } }

        public Parse<Value> Syntax { get { return (Definition as Parse<Value>); } }
    }

    public abstract class Reified<TLegacy> : Language
        where TLegacy : Language, new()
    {
        internal Reified(Select chain)
            : base(chain)
        {
        }

        internal override Value Parse(ParseContext context, string pattern, int offset)
        {
            ParseContext current = context;
            string source;
            Select select;
            Value value;
            int col, nlc;
            if (current.Language == null)
            {
                Define(GetType());
                current = new ParseContext(this, current);
                lastParse = current;
            }
            source = current.Source;
            pattern = (!String.IsNullOrEmpty(pattern) ? pattern : "language");
            select = (current.Language.Definition.ContainsKey(pattern) ? current.Language.Definition[pattern] : null);
            if (select == null)
                throw new ArgumentOutOfRangeException("pattern", String.Format("pattern '{0}' not found", pattern));
            if (offset >= current.maxOffset)
                current.maxOffset = offset;
            if (select.Knows(offset, out value))
                return value;
            col = (NewLineRegex.Matches(source.Substring(0, offset)).Count + 1);
            nlc = ((offset >= current.llo) ? NewLineRegex.Matches(source.Substring(current.llo, offset - current.llo)).Count : 0);
            if (offset >= current.llo)
                current.llo = offset;
            current.lol += nlc;
            if (select.IsLiteral)
            {
                string literal = select.AsLiteral;
                if (((offset + literal.Length) <= source.Length) && (source.Substring(offset, literal.Length) == literal))
                    select[offset] = new Value(offset, literal.Length, select.Name, literal);
                else
                    select[offset] = null;
            }
            else if (select.IsRegex)
            {
                Regex regex = select.AsRegex;
                string str = (offset < source.Length) ? source.Substring(offset) : null;
                Match match = regex.Match(str ?? String.Empty);
                if (match.Success && (match.Index == 0))
                    select[offset] = new Value(offset, match.Length, select.Name, match.Value);
                else
                    select[offset] = null;
            }
            else
                select[offset] = ((select.Syntax != null) ? select.Syntax(current, offset) : null);
            if (select[offset] != null)
                ((Value)select[offset]).Line = (!select[offset].IsEOF ? col : current.lol);
            return select[offset];
        }

        private void Define(Type self)
        {
            Derive(self, true, false);
            foreach (var item in Definition.Values)
                item.Clear();
        }

        protected void Amend(params Amendment[] amendments)
        {
            Derive(GetType(), false, true, amendments);
        }

        internal override Language Derive(Type reified, bool init, bool refine, params Amendment[] amendments)
        {
            if (typeof(Language<TLegacy>).IsAssignableFrom(reified) || init)
            {
                bool inSitu = ((GetType() == reified) || init || refine);
                IDictionary<string, Select> derived = new Dictionary<string, Select>();
                Language language = (!inSitu ? (Language)Activator.CreateInstance(reified, Let) : this);
                language.derived = (language.derived || !inSitu);
                language.refined = (language.refined || refine);
                if (!language.refined)
                {
                    if (init && !language.derived)
                    {
                        if (language.Accept != null)
                            return language;
                    }
                    if (language.derived || init)
                    {
                        if (language.derived)
                        {
                            IList<Select> current = language.Legacy.Definition.Values.ToList();
                            foreach (var item in current)
                            {
                                language.Definition[item.Name] = item;
                                if (item.Name == "syntax")
                                    language.Syntax = item;
                                if (item.Name == "language")
                                    language.Accept = item;
                            }
                        }
                        IList<MethodInfo> definition = new List<MethodInfo>();
                        var items =
                            from item in (!init ? language.GetType() : reified).GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                            let attribute = (Attribute.GetCustomAttribute(item, typeof(SyntaxAttribute)) as SyntaxAttribute)
                            where attribute != null
                            select item;
                        foreach (var item in items)
                            definition.Insert(0, item);
                        foreach (var construct in definition)
                        {
                            Construct grammar = (Construct)Delegate.CreateDelegate(typeof(Construct), language, construct);
                            Select defined = grammar();
                            if ((defined != null) && (defined.Name == "syntax"))
                            {
                                if (language.Syntax == null)
                                    language.Accept = language.Let.Accept(defined);
                            }
                        }
                    }
                }
                if ((language.sid == 0) && language.refined)
                    language.sid = language.Legacy.sid;
                if ((amendments != null) && (amendments.Length > 0))
                    foreach (var amendment in amendments)
                        amendment(language.Let);
                if (!language.refined)
                {
                    foreach (var item in language.Definition.Values)
                        derived[item.Name] = item;
                    if (language.derived)
                        foreach (var item in derived.Values)
                        {
                            var select = new Select(language, item.Name, item.Definition);
                            if (item == language.Syntax)
                                language.Syntax = select;
                            if (item == language.Accept)
                                language.Accept = select;
                        }
                }
                return language;
            }
            else
                throw new ArgumentOutOfRangeException("reified", String.Format("must conform to the legacy {0}", typeof(Language<TLegacy>).Name));
        }

        private Language DeriveFrom(Type reified, params Amendment[] amendments)
        {
            return Derive(reified, false, false, amendments);
        }

        private TReified DeriveFrom<TReified>(params Amendment[] amendments)
            where TReified : Language<TLegacy>
        {
            return (TReified)DeriveFrom(typeof(TReified), amendments);
        }

        public static TReified Coin<TReified>(params Amendment[] amendments)
            where TReified : Language<TLegacy>
        {
            return Coin<TReified>(null, amendments);
        }

        public static TReified Coin<TReified>(Language<TLegacy> legacy, params Amendment[] amendments)
            where TReified : Language<TLegacy>
        {
            legacy = (legacy ?? (Language<TLegacy>)Activator.CreateInstance(typeof(Language<TLegacy>)));
            return legacy.DeriveFrom<TReified>(amendments);
        }
    }

    public class Language<TLegacy> : Reified<TLegacy>
        where TLegacy : Language, new()
    {
        private bool reserved;

        public Language()
            : this(null)
        {
        }

        public Language(Select chain)
            : this(chain, false)
        {
        }

        private Language(Select chain, bool reserved)
            : base(chain)
        {
            this.reserved = reserved;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SyntaxAttribute : Attribute
    {
        public SyntaxAttribute()
            : base()
        {
        }
    }

    public delegate TResult ValueVisitorDelegate<TResult>(Value value, ValueVisitorDelegate<TResult> visitor)
        where TResult : Value, new();
    public delegate TResult ValueVisitorDelegate<TResult, TContext>(TContext context, Value value, ValueVisitorDelegate<TResult, TContext> visitor)
        where TResult : Value, new();

    public interface Let : IDisposable
    {
        bool HasErrors { get; }
        bool IsEOF { get; }
        bool IsBlank { get; }
        bool IsComment { get; }
        bool IsToken { get; }
        bool IsError { get; }
        int Line { get; }
        int Offset { get; }
        int Length { get; }
        string Ident { get; }
        string Brand { get; }
        string Literal { get; }
        Value[] Content { get; }
        Value[] Errors { get; }
        TValue Craft<TValue>()
            where TValue : Value, new();
        TValue Craft<TValue>(string newBrand)
            where TValue : Value, new();
        TValue Craft<TValue>(string newBrand, string newLiteral)
            where TValue : Value, new();
        TValue Craft<TValue>(string newBrand, string newLiteral, Value[] newChildren)
            where TValue : Value, new();
        Value Visit(ValueVisitorDelegate<Value> visitor);
        Value Visit(object context, ValueVisitorDelegate<Value, object> visitor);
        Value Visit<TContext>(TContext context, ValueVisitorDelegate<Value, TContext> visitor);
        TResult Visit<TResult>(ValueVisitorDelegate<TResult> visitor)
            where TResult : Value, new();
        TResult Visit<TResult, TContext>(TContext context, ValueVisitorDelegate<TResult, TContext> visitor)
            where TResult : Value, new();
    }

    public class Value : Let
    {
        public const string EndOfFile = "EOF";

        private bool isEOF;
        private int line;
        private int offset;
        private int length;
        private string brand;
        private string literal;
        private Value[] content;
        private Value[] errors;

        public Value()
            : this(null)
        {
        }

        public Value(string brand)
            : this(brand, null)
        {
        }

        public Value(int offset, int length, string brand)
            : this(false, offset, length, brand)
        {
        }

        internal Value(bool isEOF, int offset, int length, string brand)
            : this(offset, length, brand, null, null)
        {
            this.isEOF = isEOF;
        }

        public Value(string brand, string literal)
            : this(0, 0, brand, literal)
        {
        }

        public Value(int offset, int length, string brand, string literal)
            : this(offset, length, brand, literal, null)
        {
        }

        public Value(string brand, string literal, Value[] content)
            : this(0, 0, brand, literal, content)
        {
        }

        internal Value(int offset, int length, string brand, string literal, Value[] content)
        {
            Offset = offset;
            Length = length;
            Brand = brand;
            Literal = literal;
            Content = content;
        }

        public TValue Craft<TValue>()
            where TValue : Value, new()
        {
            return Craft<TValue>(null);
        }

        public TValue Craft<TValue>(string newBrand)
            where TValue : Value, new()
        {
            return Craft<TValue>(newBrand, null);
        }

        public TValue Craft<TValue>(string newBrand, string newLiteral)
            where TValue : Value, new()
        {
            return Craft<TValue>(newBrand, newLiteral, null);
        }

        public TValue Craft<TValue>(string newBrand, string newLiteral, Value[] newContent)
            where TValue : Value, new()
        {
            if ((typeof(TValue) == GetType()) && (newBrand == null) && (newLiteral == null) && (newContent == null))
                return (TValue)this;
            TValue baked = Activator.CreateInstance<TValue>();
            baked.IsEOF = IsEOF;
            baked.Line = Line;
            baked.Offset = Offset;
            baked.Length = Length;
            baked.Brand = ((newBrand != null) ? newBrand : Brand);
            baked.Literal = (!IsEOF ? (newLiteral ?? Literal) : null);
            baked.Content = (!IsEOF ? (newContent ?? Content) : null);
            return baked;
        }

        private string GetLiteral()
        {
            string literal = null;
            if (IsEOF)
                return EndOfFile;
            if (String.IsNullOrEmpty(this.literal))
            {
                foreach (Value item in Content)
                {
                    string theLiteral = item.Literal;
                    if (theLiteral != String.Empty)
                    {
                        literal = theLiteral;
                        break;
                    }
                }
                Literal = literal;
            }
            return (this.literal ?? String.Empty);
        }

        private Value[] GetErrors()
        {
            if (errors == null)
            {
                IList<Value> errorList = new List<Value>();
                if (!IsError)
                    foreach (Value item in Content)
                    {
                        if (item.HasErrors)
                            foreach (Value error in item.Errors)
                                errorList.Add(error);
                    }
                else
                    errorList.Add(this);
                errors = errorList.ToArray();
            }
            return errors;
        }

        private TResult DoVisit<TResult, TContext>(TContext context, Delegate visitor)
            where TResult : Value, new()
        {
            ValueVisitorDelegate<TResult> contextFreeVisit = (visitor as ValueVisitorDelegate<TResult>);
            ValueVisitorDelegate<TResult, TContext> contextBoundVisit = (visitor as ValueVisitorDelegate<TResult, TContext>);
            IList<Value> newContent = new List<Value>();
            if (visitor != null)
            {
                TResult changed = ((contextBoundVisit != null) ? contextBoundVisit(context, this, contextBoundVisit) : contextFreeVisit(this, contextFreeVisit));
                if (changed != this)
                    return changed;
                foreach (Value value in Content)
                {
                    TResult replaced = ((contextBoundVisit != null) ? value.Visit<TResult, TContext>(context, contextBoundVisit) : value.Visit<TResult>(contextFreeVisit));
                    if (replaced != null)
                        newContent.Add(replaced);
                }
            }
            if (newContent.Count > 0)
                content = newContent.ToArray();
            return (this as TResult);
        }

        private TResult ContextFreeVisit<TResult>(ValueVisitorDelegate<TResult> visitor)
            where TResult : Value, new()
        {
            return DoVisit<TResult, object>(null, visitor);
        }

        private TResult ContextBoundVisit<TResult, TContext>(TContext context, ValueVisitorDelegate<TResult, TContext> visitor)
            where TResult : Value, new()
        {
            return DoVisit<TResult, TContext>(context, visitor);
        }

        public Value Visit(ValueVisitorDelegate<Value> visitor)
        {
            return Visit<Value>(visitor);
        }

        public Value Visit(object context, ValueVisitorDelegate<Value, object> visitor)
        {
            return Visit<object>(context, visitor);
        }

        public Value Visit<TContext>(TContext context, ValueVisitorDelegate<Value, TContext> visitor)
        {
            return Visit<Value, TContext>(context, visitor);
        }

        public TResult Visit<TResult>(ValueVisitorDelegate<TResult> visitor)
            where TResult : Value, new()
        {
            return ContextFreeVisit<TResult>(visitor);
        }

        public TResult Visit<TResult, TContext>(TContext context, ValueVisitorDelegate<TResult, TContext> visitor)
            where TResult : Value, new()
        {
            return ContextBoundVisit<TResult, TContext>(context, visitor);
        }

        public virtual void Dispose()
        {
        }

        public virtual bool HasErrors { get { return (Errors.Length > 0); } }

        public virtual bool IsEOF { get { return isEOF; } private set { isEOF = value; } }

        public virtual bool IsBlank { get { return (IsToken && (Literal.Trim() == String.Empty)); } }

        public virtual bool IsComment { get { return (IsToken && Brand.ToLower().EndsWith("comment")); } }

        public virtual bool IsToken { get { return Brand.Contains('@'); } }

        public virtual bool IsError { get { return Brand.Contains(':'); } }

        public virtual int Line { get { return line; } internal set { line = value; } }

        public virtual int Offset { get { return offset; } private set { offset = value; } }

        public virtual int Length { get { return length; } private set { length = value; } }

        public virtual string Ident { get { return Brand.TrimStart('@').Replace(':', ' '); } }

        public virtual string Brand { get { return (brand ?? String.Empty); } private set { brand = value; } }

        public virtual string Literal { get { return GetLiteral(); } private set { literal = value; } }

        public virtual Value[] Content { get { return (content ?? new Value[] { }); } private set { content = ((value != null) ? (Value[])value.Clone() : null); } }

        public virtual Value[] Errors { get { return GetErrors(); } }
    }

    public sealed class ParseContext
    {
        private Language language;
        internal string source;
        internal string[] lines;
        internal int llo;
        internal int lol = 1;
        internal int maxOffset;

        internal ParseContext(string source)
        {
            this.source = (source ?? String.Empty);
            this.lines = this.source.Split('\n');
        }

        internal ParseContext(Language language, ParseContext current)
        {
            this.language = language;
            this.source = current.source;
            this.lines = current.lines;
            this.llo = current.llo;
            this.lol = current.lol;
            this.maxOffset = current.maxOffset;
        }

        public Language Language { get { return language; } }

        public string Source { get { return source; } }

        public string[] Lines { get { return lines; } }
    }

    public class Language
    {
        public sealed class Void : Language<Void>
        {
            public static readonly Void Language = Void.Empty;

            public Void()
                : base()
            {
            }
        }

        private object syncRoot;
        private Select let;
        private Dictionary<string, Select> definition;
        internal int sid;
        internal bool parsing;
        internal ParseContext lastParse;
        internal Select Syntax;
        internal Select Accept;
        internal bool derived;
        internal bool refined;
        internal Select<Value> semantic;
        protected readonly Regex NewLineRegex;
        public readonly Select Chain;

        internal static Void empty;
        public static Void Empty { get { return ((empty == null) ? new Void() : empty); } }

        internal Language()
            : this(null)
        {
        }

        protected Language(Select chain)
            : base()
        {
            if ((empty == null) && (GetType() == typeof(Void)))
                empty = (Void)this;
            let = new Select(this);
            NewLineRegex = new Regex(@"\n", RegexOptions.Compiled);
            Chain = (chain ?? ((GetType() != typeof(Language.Void)) ? (new Language.Void()).Let : Language.Empty.Let));
        }

        internal virtual Language Derive(Type reified, bool init, bool redefine, params Amendment[] amendments)
        {
            return this;
        }

        internal virtual Value Parse(ParseContext context, string pattern, int offset)
        {
            return null;
        }

        [Syntax]
        protected virtual Select Define()
        {
            // The Void language only accepts (and expects) EOF, immediately.
            return Let.Empty();
        }

        protected virtual Value Evaluate(string select, Value value)
        {
            return value;
        }

        public Value Parse(string source)
        {
            source = (source ?? String.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            lock (SyncRoot)
            {
                try
                {
                    if (parsing)
                        throw new Exception(String.Format("{0} does not support reentrant parsing", GetType().FullName));
                    Value value = Parse(new ParseContext(source), null, 0);
                    parsing = false;
                    return value;
                }
                finally
                {
                    if (parsing)
                        lastParse = null;
                    parsing = false;
                }
            }
        }

        public int GetColumn(int line, int offset)
        {
            if (LastParse.Lines == null)
                return 0;
            int a = 0;
            for (int i = 0; i < line - 1; i++)
                a += LastParse.Lines[i].Length;
            a += line - 1;
            return ((offset >= a) ? (offset - a + 1) : 0);
        }

        public string GetToken(int offset)
        {
            if (!String.IsNullOrEmpty(LastParse.Source) && (offset < LastParse.Source.Length))
                return LastParse.Source.Substring(offset, 1);
            else
                return Value.EndOfFile;
        }

        internal string Sid
        {
            get
            {
                return (++sid).ToString();
            }
        }

        internal Dictionary<string, Select> Definition
        {
            get
            {
                if (definition == null)
                    definition = new Dictionary<string, Select>();
                return definition;
            }
        }

        protected object SyncRoot
        {
            get
            {
                if (syncRoot == null)
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);
                return syncRoot;
            }
        }

        protected Select<Value> Semantic { get { return semantic; } set { semantic = value; } }

        public Language Legacy { get { return ((Chain.Language != this) ? Chain.Language : this); } }

        public Select Let { get { return let; } internal set { let = value; } }

        public bool Derived { get { return derived; } }

        public bool Refined { get { return refined; } }

        public ParseContext LastParse { get { return lastParse; } }
    }
}
