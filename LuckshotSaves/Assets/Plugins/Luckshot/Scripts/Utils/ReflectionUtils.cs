using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

public static class ReflectionUtils
{
	// TODO: Do this betterly
	// hardcoded list of assemblies for core and plugin stuff
	// note that asmdef things won't be included here
	private static List<string> projectAssemblies = new List<string>()
	{
		"Assembly-CSharp-firstpass",
		"Assembly-CSharp" ,
		"Assembly-CSharp-Editor-firstpass",
		"Assembly-CSharp-Editor"
	};

	public static List<string> ProjectAssemblies => projectAssemblies;

	public static string SimplifyTypeName(string name)
    {
		int index = name.LastIndexOf(',');
		return (index > 0) ? name.Substring(0, index).Trim() : name;
	}

	public static System.Object DoInvoke(Type type, string methodName, System.Object[] parameters)
	{
		Type[] types = new Type[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
			types[i] = parameters[i].GetType();

		MethodInfo method = type.GetMethod(methodName, (BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public), null, types, null);
		return DoInvoke2(type, method, parameters);
	}

	public static System.Object DoInvoke2(Type type, MethodInfo method, System.Object[] parameters)
	{
		if (method.IsStatic)
			return method.Invoke(null, parameters);

		System.Object obj = type.InvokeMember(null,
		BindingFlags.DeclaredOnly |
		BindingFlags.Public | BindingFlags.NonPublic |
		BindingFlags.Instance | BindingFlags.CreateInstance, null, null, new System.Object[0]);

		return method.Invoke(obj, parameters);
	}

    // Deep Copy
    public static List<FieldInfo> GetAllFields(this Type type, BindingFlags flags)
    {
        if (type == typeof(System.Object))
            return new List<FieldInfo>();

        var fields = type.BaseType.GetAllFields(flags);
        fields.AddRange(type.GetFields(flags | BindingFlags.DeclaredOnly));
        return fields;
    }

    public static T DeepCopy<T>(T obj)
    {
        if (obj == null)
            throw new ArgumentNullException("Object cannot be null");

        return (T)DoCopy(obj);
    }

    private static object DoCopy(object obj)
    {
        if (obj == null)
            return null;

        var type = obj.GetType();
        if (type.IsValueType || type == typeof(string))
        {
            return obj;
        }
        else if (type.IsArray)
        {
            Type elementType = type.GetElementType();
            var array = obj as Array;
            Array copied = Array.CreateInstance(elementType, array.Length);
            for (int i = 0; i < array.Length; i++)
                copied.SetValue(DoCopy(array.GetValue(i)), i);

            return Convert.ChangeType(copied, obj.GetType());
        }
        else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
        {
            return obj;
        }
        else if (type.IsClass)
        {
            var copy = Activator.CreateInstance(obj.GetType());

            var fields = type.GetAllFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                var fieldValue = field.GetValue(obj);
                if (fieldValue != null)
                    field.SetValue(copy, DoCopy(fieldValue));
            }

            return copy;
        }
        else
        {
            throw new ArgumentException("Unknown type");
        }
    }
}