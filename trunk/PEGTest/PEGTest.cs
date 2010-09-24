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
using System.Text;

using System.Languages;

namespace PEGTest
{
    class Program
    {
        private static string quickNDirtyIndent = String.Empty;

        private static void PrettyPrint(Let phrase)
        {
            PrettyPrint(quickNDirtyIndent, phrase); 
        }

        private static void PrettyPrint(string indent, Let phrase)
        {
            PrettyPrint(indent, phrase, phrase.Line, phrase.Offset, phrase.Length, phrase.Ident, phrase.Literal);
        }

        private static void PrettyPrint(string indent, Let phrase, int lno, int ofs, int len, string title, object val)
        {
            Console.WriteLine(quickNDirtyIndent + String.Format("{0}\t\"{1}\" (line {2}) (ofs: {3}) (len: {4})", title, (val ?? String.Empty).ToString().Replace("\r\n", String.Empty), lno, ofs, len));
            quickNDirtyIndent += "  ";
            foreach (Let item in phrase.Content)
                PrettyPrint(indent, item, item.Line, item.Offset, item.Length, item.Ident, item.Literal);
            quickNDirtyIndent = quickNDirtyIndent.Substring(0, quickNDirtyIndent.Length - 2);
        }

        static void Main(string[] args)
        {
            Language language;
            bool en, en2, fg, fr, mt, ys;
            ConsoleKeyInfo keyInfo;
            Console.WriteLine();
            Console.WriteLine("Choose your language :");
            Console.WriteLine("\tpress 'E' for English, or");
            Console.WriteLine("\tpress 'N' for Extended English, or");
            Console.WriteLine("\tpress 'F' for Frenglish, or");
            Console.WriteLine("\tpress 'R' for French, or");
            Console.WriteLine("\tpress 'V' for the Void (empty) language, or");
            Console.WriteLine("any other key to exit.");
            Console.WriteLine();
            keyInfo = Console.ReadKey();
            en = ((keyInfo.KeyChar == 'E') || (keyInfo.KeyChar == 'e'));
            en2 = ((keyInfo.KeyChar == 'N') || (keyInfo.KeyChar == 'n'));
            fg = ((keyInfo.KeyChar == 'F') || (keyInfo.KeyChar == 'f'));
            fr = ((keyInfo.KeyChar == 'R') || (keyInfo.KeyChar == 'r'));
            mt = ((keyInfo.KeyChar == 'V') || (keyInfo.KeyChar == 'v'));
            ys = ((keyInfo.KeyChar == 'Y') || (keyInfo.KeyChar == 'y'));
            if (!(en || en2 || fg || fr || mt || ys))
                return;
            var empty = Language.Empty; // note this works, too: var empty = Language.Void.Language
            var english = StandardEnglish.Language;
            var extended = ExtendedEnglish.Language;
            var frenglish = Frenglish.Language;
            var french = French.Language;
            language = (mt ? empty : (ys ? new YSharp() : (!fr ? (fg ? (Language)frenglish : (en2 ? (Language)extended : (Language)english)) : (Language)french)));
            while ((keyInfo.KeyChar != 'X') && (keyInfo.KeyChar != 'x'))
            {
                string text = String.Empty;
                string line;
                Value parsed;
                Console.Clear();
                Console.WriteLine();
                if (language is Language<English>)
                {
                    Console.WriteLine("articles :\ta, the");
                    Console.WriteLine("nouns :\t\tboy, boyfriend, cake, cat, coffee, dog, girl, girlfriend, man, woman");
                    Console.WriteLine("verbs :\t\teats, drinks, has, hates, kicks, loves, pets");
                    Console.WriteLine("adjectives :\tbig, small, black, white, hot, cold, sweet");
                    Console.WriteLine("adverbs :\tcrudely, gently, quickly, slowly");
                    if (language is ExtendedEnglish)
                    {
                        Console.WriteLine();
                        Console.WriteLine("... and :\tthe (MAGIC!) 'so-called' to merge foreign vernaculars :)");
                    }
                    Console.WriteLine();
                    Console.WriteLine("Enter the {0} text in lower case (don't forget the sentence-ending period)", fr ? "french" : "english");
                    Console.WriteLine("and finish with an empty line :");
                    Console.WriteLine();
                }
                else
                    Console.Write("{0} > ", language.GetType().FullName);
                while (!String.IsNullOrEmpty(line = Console.ReadLine()))
                    text += String.Concat(line, "\r\n");
                text = text.TrimEnd();
                parsed = language.Parse(text);
                if (language is Language<English>)
                {
                    Console.WriteLine("Syntax analysis of {0} input :", language.GetType().FullName);
                    Console.WriteLine();
                }
                Console.WriteLine("Begin>");
                Console.WriteLine();
                if (parsed != null)
                {
                    parsed = parsed.Visit(
                            (value, visitor)
                                => (!value.IsBlank ? value : null)
                        );
                    Console.WriteLine("Parsed ... {0}", parsed.HasErrors ? "with errors" : "successfully");
                    Console.WriteLine();
                    if (parsed.HasErrors)
                    {
                        foreach (Value error in parsed.Errors)
                        {
                            string token = language.GetToken(error.Offset);
                            Console.WriteLine(String.Format("error: {0}: line {1}, column {2}, found '{3}'", error.Ident, error.Line, language.GetColumn(error.Line, error.Offset), (token.Trim() != String.Empty) ? token : " "));
                        }
                        Console.WriteLine();
                    }
                    PrettyPrint(parsed);
                }
                else
                    Console.WriteLine("unrecoverable syntax error");
                Console.WriteLine();
                Console.WriteLine("<End");
                Console.WriteLine();
                Console.WriteLine("Press 'X' to exit or any other key to continue...");
                keyInfo = Console.ReadKey();
            }
        }
    }
}
