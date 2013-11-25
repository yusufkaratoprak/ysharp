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
		public abstract class Callee
		{
			public abstract object Invoke();
			public abstract object Invoke<A>(A a);
			public abstract object Invoke<A, B>(A a, B b);
			public abstract object Invoke<A, B, C>(A a, B b, C c);
			public abstract object Invoke<A, B, C, D>(A a, B b, C c, D d);
		}

		internal class Callee<TResult> : Callee
		{
			private readonly Func<object> callee;
			internal Callee(Func<object> callee) { System.Linq.Expressions.Expression<Func<object>> expr = () => callee(); this.callee = expr.Compile(); }
			public override object Invoke() { return (TResult)callee(); }
			public override object Invoke<A>(A a) { throw new NotImplementedException(); }
			public override object Invoke<A, B>(A a, B b) { throw new NotImplementedException(); }
			public override object Invoke<A, B, C>(A a, B b, C c) { throw new NotImplementedException(); }
			public override object Invoke<A, B, C, D>(A a, B b, C c, D d) { throw new NotImplementedException(); }
		}

		internal class Callee<T, TResult> : Callee
		{
			private readonly Func<T, object> callee;
			internal Callee(Func<T, object> callee) { System.Linq.Expressions.Expression<Func<T, object>> expr = (arg) => callee(arg); this.callee = expr.Compile(); }
			public override object Invoke() { throw new NotImplementedException(); }
			public override object Invoke<A>(A a) { return (TResult)callee((T)(object)a); }
			public override object Invoke<A, B>(A a, B b) { throw new NotImplementedException(); }
			public override object Invoke<A, B, C>(A a, B b, C c) { throw new NotImplementedException(); }
			public override object Invoke<A, B, C, D>(A a, B b, C c, D d) { throw new NotImplementedException(); }
		}

		internal class Callee<T1, T2, TResult> : Callee
		{
			private readonly Func<T1, T2, object> callee;
			internal Callee(Func<T1, T2, object> callee) { System.Linq.Expressions.Expression<Func<T1, T2, object>> expr = (arg1, arg2) => callee(arg1, arg2); this.callee = expr.Compile(); }
			public override object Invoke() { throw new NotImplementedException(); }
			public override object Invoke<A>(A a) { throw new NotImplementedException(); }
			public override object Invoke<A, B>(A a, B b) { return (TResult)callee((T1)(object)a, (T2)(object)b); }
			public override object Invoke<A, B, C>(A a, B b, C c) { throw new NotImplementedException(); }
			public override object Invoke<A, B, C, D>(A a, B b, C c, D d) { throw new NotImplementedException(); }
		}

		internal class Callee<T1, T2, T3, TResult> : Callee
		{
			private readonly Func<T1, T2, T3, object> callee;
			internal Callee(Func<T1, T2, T3, object> callee) { System.Linq.Expressions.Expression<Func<T1, T2, T3, object>> expr = (arg1, arg2, arg3) => callee(arg1, arg2, arg3); this.callee = expr.Compile(); }
			public override object Invoke() { throw new NotImplementedException(); }
			public override object Invoke<A>(A a) { throw new NotImplementedException(); }
			public override object Invoke<A, B>(A a, B b) { throw new NotImplementedException(); }
			public override object Invoke<A, B, C>(A a, B b, C c) { return (TResult)callee((T1)(object)a, (T2)(object)b, (T3)(object)c); }
			public override object Invoke<A, B, C, D>(A a, B b, C c, D d) { throw new NotImplementedException(); }
		}

		internal class Callee<T1, T2, T3, T4, TResult> : Callee
		{
			private readonly Func<T1, T2, T3, T4, object> callee;
			internal Callee(Func<T1, T2, T3, T4, object> callee) { System.Linq.Expressions.Expression<Func<T1, T2, T3, T4, object>> expr = (arg1, arg2, arg3, arg4) => callee(arg1, arg2, arg3, arg4); this.callee = expr.Compile(); }
			public override object Invoke() { throw new NotImplementedException(); }
			public override object Invoke<A>(A a) { throw new NotImplementedException(); }
			public override object Invoke<A, B>(A a, B b) { throw new NotImplementedException(); }
			public override object Invoke<A, B, C>(A a, B b, C c) { throw new NotImplementedException(); }
			public override object Invoke<A, B, C, D>(A a, B b, C c, D d) { return (TResult)callee((T1)(object)a, (T2)(object)b, (T3)(object)c, (T4)(object)d); }
		}

		private static Callee CreateCallee(Type callableType, Delegate func)
		{
			var culture = System.Globalization.CultureInfo.InvariantCulture;
			var flags =
				System.Reflection.BindingFlags.CreateInstance |
				System.Reflection.BindingFlags.Instance |
				System.Reflection.BindingFlags.NonPublic;
			var args = new object[] { func };
			return (Callee)Activator.CreateInstance(callableType, flags, null, args, culture);
		}

		private static Func<object> DoUpCast<TResult>(Func<TResult> func)
		{
			System.Linq.Expressions.Expression<Func<object>> expr = () => func();
			return expr.Compile();
		}

		private static Func<T, object> DoUpCast<T, TResult>(Func<T, TResult> func)
		{
			System.Linq.Expressions.Expression<Func<T, object>> expr = (arg) => func(arg);
			return expr.Compile();
		}

		private static Func<T1, T2, object> DoUpCast<T1, T2, TResult>(Func<T1, T2, TResult> func)
		{
			System.Linq.Expressions.Expression<Func<T1, T2, object>> expr = (arg1, arg2) => func(arg1, arg2);
			return expr.Compile();
		}

		private static Func<T1, T2, T3, object> DoUpCast<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func)
		{
			System.Linq.Expressions.Expression<Func<T1, T2, T3, object>> expr = (arg1, arg2, arg3) => func(arg1, arg2, arg3);
			return expr.Compile();
		}

		private static Func<T1, T2, T3, T4, object> DoUpCast<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func)
		{
			System.Linq.Expressions.Expression<Func<T1, T2, T3, T4, object>> expr = (arg1, arg2, arg3, arg4) => func(arg1, arg2, arg3, arg4);
			return expr.Compile();
		}

		public static Func<object> UpCast<TResult>(Delegate func, TResult protoResult)
		{
			return DoUpCast((Func<TResult>)func);
		}

		public static Func<object> UpCast<TResult>(Func<TResult> func)
		{
			return DoUpCast(func);
		}

		public static Func<T, object> UpCast<T, TResult>(Delegate func, T protoArg, TResult protoResult)
		{
			return DoUpCast((Func<T, TResult>)func);
		}

		public static Func<T, object> UpCast<T, TResult>(Func<T, TResult> func)
		{
			return DoUpCast(func);
		}

		public static Func<T1, T2, object> UpCast<T1, T2, TResult>(Delegate func, T1 protoArg1, T2 protoArg2, TResult protoResult)
		{
			return DoUpCast((Func<T1, T2, TResult>)func);
		}

		public static Func<T1, T2, object> UpCast<T1, T2, TResult>(Func<T1, T2, TResult> func)
		{
			return DoUpCast(func);
		}

		public static Func<T1, T2, T3, object> UpCast<T1, T2, T3, TResult>(Delegate func, T1 protoArg1, T2 protoArg2, T3 protoArg3, TResult protoResult)
		{
			return DoUpCast((Func<T1, T2, T3, TResult>)func);
		}

		public static Func<T1, T2, T3, object> UpCast<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func)
		{
			return DoUpCast(func);
		}

		public static Func<T1, T2, T3, T4, object> UpCast<T1, T2, T3, T4, TResult>(Delegate func, T1 protoArg1, T2 protoArg2, T3 protoArg3, T4 protoArg4, TResult protoResult)
		{
			return DoUpCast((Func<T1, T2, T3, T4, TResult>)func);
		}

		public static Func<T1, T2, T3, T4, object> UpCast<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func)
		{
			return DoUpCast(func);
		}

		public static object Call(Callee callee)
		{
			return callee.Invoke();
		}

		public static object Call(Type resultType, Func<object> upCast)
		{
			return Callable(resultType, upCast).Invoke();
		}

		public static Callee Callable<TResult>(Func<TResult> func)
		{
			return Callable(typeof(TResult), func);
		}

		public static Callee Callable(Type resultType, Func<object> upCast)
		{
			return Callable(resultType, (Delegate)upCast);
		}

		public static Callee Callable(Type resultType, Delegate func)
		{
			return CreateCallee(typeof(Callee<>).MakeGenericType(resultType), func);
		}

		public static object Call<T>(Callee callee, T arg)
		{
			return callee.Invoke(arg);
		}

		public static object Call<T>(Type resultType, Func<T, object> upCast, T arg)
		{
			return Callable(resultType, upCast).Invoke(arg);
		}

		public static Callee Callable<T, TResult>(Func<T, TResult> func)
		{
			return Callable(typeof(TResult), typeof(T), func);
		}

		public static Callee Callable<T>(Type resultType, Func<T, object> upCast)
		{
			return Callable(resultType, typeof(T), upCast);
		}

		public static Callee Callable(Type resultType, Type arg, Delegate func)
		{
			return CreateCallee(typeof(Callee<,>).MakeGenericType(arg, resultType), func);
		}

		public static object Call<T1, T2>(Callee callee, T1 arg1, T2 arg2)
		{
			return callee.Invoke(arg1, arg2);
		}

		public static object Call<T1, T2>(Type resultType, Func<T1, T2, object> upCast, T1 arg1, T2 arg2)
		{
			return Callable(resultType, upCast).Invoke(arg1, arg2);
		}

		public static Callee Callable<T1, T2, TResult>(Func<T1, T2, TResult> func)
		{
			return Callable(typeof(TResult), typeof(T1), typeof(T2), func);
		}

		public static Callee Callable<T1, T2>(Type resultType, Func<T1, T2, object> upCast)
		{
			return Callable(resultType, typeof(T1), typeof(T2), upCast);
		}

		public static Callee Callable(Type resultType, Type arg1, Type arg2, Delegate func)
		{
			return CreateCallee(typeof(Callee<,,>).MakeGenericType(arg1, arg2, resultType), func);
		}

		public static object Call<T1, T2, T3>(Callee callee, T1 arg1, T2 arg2, T3 arg3)
		{
			return callee.Invoke(arg1, arg2, arg3);
		}

		public static object Call<T1, T2, T3>(Type resultType, Func<T1, T2, T3, object> upCast, T1 arg1, T2 arg2, T3 arg3)
		{
			return Callable(resultType, upCast).Invoke(arg1, arg2, arg3);
		}

		public static Callee Callable<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func)
		{
			return Callable(typeof(TResult), typeof(T1), typeof(T2), typeof(T3), func);
		}

		public static Callee Callable<T1, T2, T3>(Type resultType, Func<T1, T2, T3, object> upCast)
		{
			return Callable(resultType, typeof(T1), typeof(T2), typeof(T3), upCast);
		}

		public static Callee Callable(Type resultType, Type arg1, Type arg2, Type arg3, Delegate func)
		{
			return CreateCallee(typeof(Callee<,,,>).MakeGenericType(arg1, arg2, arg3, resultType), func);
		}

		public static object Call<T1, T2, T3, T4>(Callee callee, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			return callee.Invoke(arg1, arg2, arg3, arg4);
		}

		public static object Call<T1, T2, T3, T4>(Type resultType, Func<T1, T2, T3, T4, object> upCast, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			return Callable(resultType, upCast).Invoke(arg1, arg2, arg3, arg4);
		}

		public static Callee Callable<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func)
		{
			return Callable(typeof(TResult), typeof(T1), typeof(T2), typeof(T3), typeof(T4), func);
		}

		public static Callee Callable<T1, T2, T3, T4>(Type resultType, Func<T1, T2, T3, T4, object> upCast)
		{
			return Callable(resultType, typeof(T1), typeof(T2), typeof(T3), typeof(T4), upCast);
		}

		public static Callee Callable(Type resultType, Type arg1, Type arg2, Type arg3, Type arg4, Delegate func)
		{
			return CreateCallee(typeof(Callee<,,,,>).MakeGenericType(arg1, arg2, arg3, arg4, resultType), func);
		}
	}
}