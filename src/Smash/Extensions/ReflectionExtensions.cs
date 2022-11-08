using System.Reflection;
using System.Reflection.Emit;

namespace Smash.Extensions;

/// <summary>Extension methods for manage reflection.</summary>
public static class ReflectionExtensions
{
	/// <summary>Creates a delegate for the specified method.</summary>
	/// <param name="method">The method.</param>
	/// <typeparam name="TFirst">The type of the first parameter.</typeparam>
	/// <typeparam name="TSecond">The type of the second parameter.</typeparam>
	/// <typeparam name="TResult">The type of the result.</typeparam>
	/// <returns>A delegate that represents the method.</returns>
	/// <exception cref="InvalidCastException">Whether a parameter is not castable.</exception>
	public static Func<object, TFirst, TSecond, TResult> CreateDelegate<TFirst, TSecond, TResult>(this MethodInfo method)
	{
		var returnType = typeof(TResult);

		var methodParameters = method.GetParameters().Select(p => p.ParameterType).ToArray();

		var delegateParameters = new[] { typeof(TFirst), typeof(TSecond) };

		var finalParameters = new[] { typeof(object) }.Concat(delegateParameters).ToArray();

		var dynamicMethod = new DynamicMethod(string.Empty, returnType, finalParameters, true);
		var generator = dynamicMethod.GetILGenerator();

		if (!method.IsStatic)
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(method.DeclaringType!.IsClass ? OpCodes.Castclass : OpCodes.Unbox, method.DeclaringType);
		}

		for (var i = 0; i < delegateParameters.Length; i++)
		{
			generator.Emit(OpCodes.Ldarg, i + 1);

			var methodParameter = methodParameters[i];
			var delegateParameter = delegateParameters[i];

			if (delegateParameter == methodParameter)
				continue;

			if (!methodParameter.IsSubclassOf(delegateParameter) && methodParameter.IsAssignableTo(delegateParameter))
				throw new InvalidCastException($"Cannot cast {delegateParameter.Name} to {methodParameter.Name}.");

			generator.Emit(methodParameter.IsClass ? OpCodes.Castclass : OpCodes.Unbox, methodParameter);
		}

		generator.Emit(OpCodes.Call, method);

		if (returnType != method.ReturnType)
		{
			if (!method.ReturnType.IsSubclassOf(returnType) && !method.ReturnType.IsAssignableTo(returnType))
				throw new InvalidCastException($"Cannot cast {method.ReturnType.Name} to {returnType.Name}.");
			
			if (method.ReturnType.IsClass && returnType.IsClass)
				generator.Emit(OpCodes.Castclass, returnType);
			
			else if (returnType == typeof(object))
				generator.Emit(OpCodes.Box, method.ReturnType);
			
			else if (method.ReturnType.IsClass)
				generator.Emit(OpCodes.Unbox, returnType);
		}

		generator.Emit(OpCodes.Ret);

		return dynamicMethod.CreateDelegate<Func<object, TFirst, TSecond, TResult>>();
	}
}