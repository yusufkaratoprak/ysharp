/*
 * Copyright (c) 2013 Cyril Jandia
 *
 * http://www.cjandia.com/
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
from Cyril Jandia.

Inquiries : ysharp {dot} design {at} gmail {dot} com
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text.Json
{
	public partial struct JSON
	{
		internal abstract class Callable
		{
			internal abstract object Invoke();
            internal abstract object Invoke<A>(A a);
        }

		internal class Callable<TResult> : Callable
		{
			internal readonly Func<object> func;
			internal Callable(Func<object> func) { System.Linq.Expressions.Expression<Func<object>> expr = () => func(); this.func = expr.Compile(); }
            internal override object Invoke() { return ((Func<TResult>)func())(); }
            internal override object Invoke<T1>(T1 arg1) { throw new NotImplementedException(); }
        }

        internal class Callable<T1, TResult> : Callable
        {
            internal readonly Func<T1, object> func;
            internal Callable(Func<T1, object> func) { System.Linq.Expressions.Expression<Func<T1, object>> expr = (a1) => func(a1); this.func = expr.Compile(); }
            internal override object Invoke() { throw new NotImplementedException(); }
            internal override object Invoke<A>(A a) { return ((Func<T1, TResult>)func((T1)(object)a))((T1)(object)a); }
        }

        private static Func<object> DoUpCast<TResult>(Func<TResult> func)
		{
			System.Linq.Expressions.Expression<Func<object>> expr = () => func;
			return expr.Compile();
		}

        private static Func<T1, object> DoUpCast<T1, TResult>(Func<T1, TResult> func)
        {
            System.Linq.Expressions.Expression<Func<T1, object>> expr = (a1) => func;
            return expr.Compile();
        }

        public static Func<object> UpCast<TResult>(Func<TResult> func)
		{
            Func<TResult> f = () => func();
			return DoUpCast(f);
		}

        public static Func<T1, object> UpCast<T1, TResult>(Func<T1, TResult> func)
        {
            Func<T1, TResult> f = (a1) => func(a1);
            return DoUpCast(f);
        }

        public static object Call(Type result, Func<object> func)
		{
			var culture = System.Globalization.CultureInfo.InvariantCulture;
			var flags =
				System.Reflection.BindingFlags.CreateInstance |
				System.Reflection.BindingFlags.Instance |
				System.Reflection.BindingFlags.NonPublic;
			var args = new object[] { func };
            var callable = (Callable)Activator.CreateInstance(typeof(Callable<>).MakeGenericType(result), flags, null, args, culture);
            return callable.Invoke();
		}

        public static object Call<T1>(Type result, Func<T1, object> func, T1 arg1)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var flags =
                System.Reflection.BindingFlags.CreateInstance |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic;
            var args = new object[] { func };
            var callable = (Callable)Activator.CreateInstance(typeof(Callable<,>).MakeGenericType(typeof(T1), result), flags, null, args, culture);
            return callable.Invoke(arg1);
        }
	}
}