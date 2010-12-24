/* Copyright (c) 2009, 2010, 2011 Cyril Jandia
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

namespace Samples
{
    public sealed class StandardEnglish : Language<English>
    {
        /// <summary>
        /// This reifies StandardEnglish as a type conforming to Reified&lt;English&gt; (TLegacy)
        /// and leverages English's definition (TDerived) with specifics
        /// </summary>
        public static readonly StandardEnglish Language = Reify<StandardEnglish>();

        public StandardEnglish() : this(null) { }
        public StandardEnglish(Select chain) : base(chain) { Semantic = Evaluate; }

        protected override Select Define()
        {
            /*
             *  A possible equivalent syntax for the fluent interface:
             *  (special characters: "=", "<", ">", "[", "]", "@", "#", "(", ")", "|", "$", "!", "*", "+", "?", '"')
             *  
             *  // Identifier on the left of "=": the name of the pattern being defined
             *  // Identifiers on the right of "=" (outside tags): the names of the composing patterns
             *  // A <...> tag gives the pattern-matched Value's brand (as a parsing result information item)
             *  // Square brackets ([ ]): equivalent construct token for sequence, Let.Seq(...)
             *  // At sign (@) at beginning of sequence content: equivalent construct token for bool asToken = true
             *  // Pipe symbol (|): equivalent construct token for choice, Let.Or(...)
             *  HOTDOG = <HOTDOG>[@ HOT ( HYPHEN | OPTSPACE ) DOG ]
             *  
             *  // Dollar sign ($) after closing parenthesis: equivalent construct token for expected, Let.Expect(...)
             *  Det = ( <Det>[ ( A | THE ) ] )$
             *  ...
             *  // Exclamation mark (!) in tag: equivalent construct token for error, Let.Error(...)
             *  // Pound signs (#) in tag: equivalent construct token for regular expression, Let.Regex(...)
             *  // (double quotes (") for string literal, Let.Token(...))
             *  UnknownWord = <unknown!>( <WORD #[a-z]+#> )
             *  ...
             *  // Star symbol (*) after closing parenthesis: equivalent construct token for zero or more, Let.Any(...)
             *  // (plus sign (+) for one or more, Let.Some(...))
             *  OptAdjsNoun = <OptAdjsNoun>[ <OptAdjectives>( <Adjective>[ Adjective SPACE ] )* Noun ]
             *  
             *  // Question mark (?) after closing parenthesis: equivalent construct token for optional, Let.Opt(...)
             *  OptAdverb = <OptAdverb>[ ( <Adverb>[ Adverb SPACE ] )? Verb ]
             *  ...
             */
            var SPACE = Let.Regex("SPACE", @"\s+");
            var OPTSPACE = Let.Opt(SPACE);
            var HYPHEN = Let.Token("HYPHEN", "-");
            var PERIOD = Let.Token("PERIOD", ".");
            var A = Let.Token("A", "a");
            var THE = Let.Token("THE", "the");
            var BOY = Let.Token("BOY", "boy");
            var BOYFRIEND = Let.Token("BOYFRIEND", "boyfriend");
            var DOG = Let.Token("DOG", "dog");
            var CAKE = Let.Token("CAKE", "cake");
            var CAT = Let.Token("CAT", "cat");
            var COFFEE = Let.Token("COFFEE", "coffee");
            var GIRL = Let.Token("GIRL", "girl");
            var GIRLFRIEND = Let.Token("GIRLFRIEND", "girlfriend");
            var MAN = Let.Token("MAN", "man");
            var WOMAN = Let.Token("WOMAN", "woman");
            var EATS = Let.Token("EATS", "eats");
            var DRINKS = Let.Token("DRINKS", "drinks");
            var HAS = Let.Token("HAS", "has");
            var HATES = Let.Token("HATES", "hates");
            var KICKS = Let.Token("KICKS", "kicks");
            var LOVES = Let.Token("LOVES", "loves");
            var PETS = Let.Token("PETS", "pets");
            var BIG = Let.Token("BIG", "big");
            var SMALL = Let.Token("SMALL", "small");
            var BLACK = Let.Token("BLACK", "black");
            var WHITE = Let.Token("WHITE", "white");
            var HOT = Let.Token("HOT", "hot");
            var COLD = Let.Token("COLD", "cold");
            var SWEET = Let.Token("SWEET", "sweet");
            var CRUDELY = Let.Token("CRUDELY", "crudely");
            var GENTLY = Let.Token("GENTLY", "gently");
            var QUICKLY = Let.Token("QUICKLY", "quickly");
            var SLOWLY = Let.Token("SLOWLY", "slowly");
            var SO = Let.Token("SO", "so");
            var CALLED = Let.Token("CALLED", "called");
            var SO_CALLED = Let.Seq("SO_CALLED", true, SO, Let.Or(HYPHEN, SPACE), CALLED);
            var HOTDOG = Let.Seq("HOTDOG", true, HOT, Let.Or(HYPHEN, OPTSPACE), DOG);

            var Det = Let.Expect(Let.Seq("Det", Let.Or(A, THE)));
            var EnglishNoun = Let.Or(HOTDOG, BOYFRIEND, BOY, CAT, CAKE, COFFEE, DOG, GIRLFRIEND, GIRL, MAN, WOMAN);
            var UnknownWord = Let.Error("unknown", Let.Regex("WORD", "[a-z]+"));
            var Noun = Let.Seq("Noun", Let.Or(EnglishNoun, Let["ForeignNoun"], UnknownWord));
            var Verb = Let.Expect(Let.Seq("Verb", Let.Or(EATS, DRINKS, HAS, HATES, KICKS, LOVES, PETS, UnknownWord)));
            var Adjective = Let.Or(SO_CALLED, BIG, SMALL, BLACK, WHITE, Let.Seq(HOT, Let.Not(Let.Seq(OPTSPACE, DOG))), COLD, SWEET, Let["ForeignAdjective"]);
            var Adverb = Let.Or(CRUDELY, GENTLY, QUICKLY, SLOWLY);

            var OptAdjsNoun = Let.Seq("OptAdjsNoun", Let.Any("OptAdjectives", Let.Seq("Adjective", Adjective, SPACE)), Noun);
            var OptAdverb = Let.Seq("OptAdverb", Let.Opt(Let.Seq("Adverb", Adverb, SPACE)), Verb);
            var NounPhrase = Let.Or("NounPhrase", Let.Seq("NounGroup", Det, SPACE, OptAdjsNoun), OptAdjsNoun);
            var VerbGroup = Let.Or(Let.Seq("VerbGroup", Let.Expect(OptAdverb), SPACE, Let.Expect(NounPhrase)), OptAdverb);
            var Phrase = Let.Seq("Phrase", Let.Expect(NounPhrase), Let.Expect(Let.Seq("VerbPhrase", SPACE, VerbGroup)));
            var Sentence = Let.Seq("Sentence", Let.Expect(Phrase), Let.Expect(Let.Seq("Period", OPTSPACE, PERIOD)), OPTSPACE);

            return Let.Seq("syntax", OPTSPACE, Let.Expect(Let.Some("Sentences", Sentence)));
        }

        protected override Value Evaluate(string select, Value value)
        {
            if (select == "NounPhrase")
            {
                string unknownWord = null;
                value.Visit(
                    delegate(Value current, ValueVisitorDelegate<Value> visitor)
                    {
                        if (String.IsNullOrEmpty(unknownWord))
                            unknownWord = (current.Ident.Contains("unknown") ? current.Literal : unknownWord);
                        return (String.IsNullOrEmpty(unknownWord) ? current : null);
                    }
                );
                Console.WriteLine("{0} : {1}...\t=>\tHas unknown noun/verb ? {2}{3}", GetType().FullName, value.Ident, !String.IsNullOrEmpty(unknownWord), !String.IsNullOrEmpty(unknownWord) ? String.Concat(" : ", unknownWord) : String.Empty);
                Console.WriteLine();
            }
            return value;
        }
    }
}