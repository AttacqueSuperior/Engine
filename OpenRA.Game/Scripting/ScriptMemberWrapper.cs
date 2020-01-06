#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Eluant;

namespace OpenRA.Scripting
{
	public class ScriptMemberWrapper
	{
		readonly ScriptContext context;
		public readonly object Target;
		public readonly MemberInfo Member;

		public readonly bool IsMethod;
		public readonly bool IsGetProperty;
		public readonly bool IsSetProperty;

		public ScriptMemberWrapper(ScriptContext context, object target, MemberInfo mi)
		{
			this.context = context;
			Target = target;
			Member = mi;

			var property = mi as PropertyInfo;
			if (property != null)
			{
				IsGetProperty = property.GetGetMethod() != null;
				IsSetProperty = property.GetSetMethod() != null;
			}
			else
				IsMethod = true;
		}

		LuaValue Invoke(LuaVararg args)
		{
			object[] clrArgs = null;
			try
			{
				if (!IsMethod)
					throw new LuaException("Trying to invoke a ScriptMemberWrapper that isn't a method!");

				var mi = (MethodInfo)Member;
				var pi = mi.GetParameters();

				clrArgs = new object[pi.Length];

				var argCount = args.Count;
				for (var i = 0; i < pi.Length; i++)
				{
					if (i >= argCount)
					{
						if (!pi[i].IsOptional)
							throw new LuaException("Argument '{0}' of '{1}' is not optional.".F(pi[i].LuaDocString(), Member.LuaDocString()));

						clrArgs[i] = pi[i].DefaultValue;
						continue;
					}

					if (!args[i].TryGetClrValue(pi[i].ParameterType, out clrArgs[i]))
						throw new LuaException("Unable to convert parameter {0} to {1}".F(i, pi[i].ParameterType.Name));
				}

				return mi.Invoke(Target, clrArgs).ToLuaValue(context);
			}
			finally
			{
				// Clean up all the Lua arguments that were given to us.
				foreach (var arg in args)
					arg.Dispose();
				args.Dispose();

				// If we created any arrays of LuaValues to pass around, we need to dispose those too.
				if (clrArgs != null)
				{
					foreach (var arg in clrArgs)
					{
						if (!(arg is LuaValue[]))
							continue;
						foreach (var value in (LuaValue[])arg)
							value.Dispose();
					}
				}
			}
		}

		public LuaValue Get(LuaRuntime runtime)
		{
			if (IsMethod)
				return runtime.CreateFunctionFromDelegate((Func<LuaVararg, LuaValue>)Invoke);

			if (IsGetProperty)
				return ((PropertyInfo)Member).GetValue(Target, null).ToLuaValue(context);

			throw new LuaException("The property '{0}' is write-only".F(Member.Name));
		}

		public void Set(LuaRuntime runtime, LuaValue value)
		{
			if (IsSetProperty)
			{
				var pi = (PropertyInfo)Member;
				object clrValue;
				if (!value.TryGetClrValue(pi.PropertyType, out clrValue))
					throw new LuaException("Unable to convert '{0}' to Clr type '{1}'".F(value.WrappedClrType().Name, pi.PropertyType));

				pi.SetValue(Target, clrValue, null);
			}
			else
				throw new LuaException("The property '{0}' is read-only".F(Member.Name));
		}

		public static IEnumerable<MemberInfo> WrappableMembers(Type t)
		{
			// Only expose defined public non-static methods that were explicitly declared by the author
			var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			return t.GetMembers(flags).Where(mi =>
			{
				// Properties are always wrappable
				if (mi is PropertyInfo)
					return true;

				// Methods are allowed if they aren't generic, and aren't generated by the compiler
				var method = mi as MethodInfo;
				if (method != null && !method.IsGenericMethodDefinition && !method.IsSpecialName)
					return true;

				// Fields aren't allowed
				return false;
			});
		}
	}
}
