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
    /// <summary>
    /// ExtendedEnglish gives an example of dynamic grammar refinement at...
    /// ... parse time, triggered by the parse context itself,
    /// when 'so-called' is used in noun phrases.
    /// 
    /// (Amendments implemented in ExtendedEnglish.Evaluate method.)
    /// </summary>
    public sealed class ExtendedEnglish : Language<StandardEnglish, English>
    {
        private bool amended;

        /// <summary>
        /// This reifies ExtendedEnglish as a type conforming to Reified&lt;English&gt; (TLegacy)
        /// and leverages StandardEnglish's definition (TDerived) with specifics
        /// </summary>
        public static readonly ExtendedEnglish Language = Reify<ExtendedEnglish>();

        public ExtendedEnglish() : this(null) { }
        public ExtendedEnglish(Select chain) : base(chain) { Semantic = Evaluate; }

        protected override Value Evaluate(string select, Value value)
        {
            if ((value.Ident == "SO_CALLED") && !amended)
            {
                Amend(
                    delegate(Select let)
                    {
                        var CONNOISSEUR = let.Token("CONNOISSEUR", "connoisseur");
                        var RENDEZ = let.Token("RENDEZ", "rendez");
                        var VOUS = let.Token("VOUS", "vous");
                        var RENDEZ_VOUS = let.Seq("RENDEZ_VOUS", RENDEZ, let.Or(let["@HYPHEN"], let["@SPACE"]), VOUS);
                        var ForeignNoun = let.Or("ForeignNoun", CONNOISSEUR, RENDEZ_VOUS);
                        return null;
                    },
                    delegate(Select let)
                    {
                        var CHIC = let.Token("CHIC", "chic");
                        var PETITE = let.Token("PETITE", "petite");
                        var ForeignAdj = let.Or("ForeignAdj", CHIC, PETITE);
                        return null;
                    }
                );
                amended = true;
            }
            return value;
        }
    }
}