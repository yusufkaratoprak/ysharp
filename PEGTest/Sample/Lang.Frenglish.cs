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
    /// Frenglish is just StandardEnglish with the same amendments as ExtendedEnglish's,
    /// but instead built-in via [Syntax]-marked methods.
    /// 
    /// (Amendments implemented in Frenglish.FrenchNoun and Frenglish.FrenchAdj methods.)
    /// </summary>
    public sealed class Frenglish : Language<StandardEnglish, English>
    {
        /// <summary>
        /// This reifies Frenglish as a type conforming to Reified&lt;English&gt; (TLegacy)
        /// and leverages StandardEnglish's definition (TDerived) with specifics
        /// </summary>
        public static readonly Frenglish Language = Reify<Frenglish>();

        public Frenglish() : this(null) { }
        public Frenglish(Select chain) : base(chain) { }

        [Syntax]
        private Select FrenchNoun()
        {
            var CONNOISSEUR = Let.Token("CONNOISSEUR", "connoisseur");
            var RENDEZ = Let.Token("RENDEZ", "rendez");
            var VOUS = Let.Token("VOUS", "vous");
            var RENDEZ_VOUS = Let.Seq("RENDEZ_VOUS", RENDEZ, Let.Or(Let["@HYPHEN"], Let["@SPACE"]), VOUS);
            var ForeignNoun = Let.Or("ForeignNoun", CONNOISSEUR, RENDEZ_VOUS);
            return null;
        }

        [Syntax]
        private Select FrenchAdj()
        {
            var CHIC = Let.Token("CHIC", "chic");
            var PETITE = Let.Token("PETITE", "petite");
            var ForeignAdj = Let.Or("ForeignAdj", CHIC, PETITE);
            return null;
        }
    }
}