using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Text;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif


#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class FlowTypeCache
{
	static FlowTypeCache()
	{
#if UNITY_EDITOR
		InitializeIfNeeded();
#endif
	}

	// Hack to get this to load in editor without error spew
	private static void LoadFlowGraphSettings()
	{
		/*
		flowGraphSettings = Resources.Load<FlowGraphSettings>("FlowGraphSettings");
#if UNITY_EDITOR
		EditorApplication.update -= LoadFlowGraphSettings;
#endif
		*/
	}

	public class ModuleInfo
	{
		public TypeInfo TypeInfo = null;
		public List<MethodInfo> MethodInfos = new List<MethodInfo>();
	}

	private static List<Assembly> assemblies = new List<Assembly>();

	private static Dictionary<Type, ModuleInfo> moduleTypeToInfoMap = new Dictionary<Type, ModuleInfo>();
	private static Dictionary<string, Type> moduleNameToTypeMap = new Dictionary<string, Type>();
	private static Dictionary<string, Dictionary<string, MethodInfo>> moduleFunctionToMethodInfoLookup = new Dictionary<string, Dictionary<string, MethodInfo>>();
	private static Dictionary<MethodInfo, ParameterInfo[]> methodInfoToParametersLookup = new Dictionary<MethodInfo, ParameterInfo[]>();
	private static Dictionary<MethodInfo, object[]> methodInfoToRecycledParamsLookup = new Dictionary<MethodInfo, object[]>();
	
	private static List<string> propertyItemNames = new List<string>();
	private static List<Type> propertyItemTypes = new List<Type>();

	private static Dictionary<Type, List<string>> propertyItemMemberNamesMap = new Dictionary<Type, List<string>>();
	private static Dictionary<Type, Dictionary<string, MemberInfo>> propertyItemMemberInfoMap = new Dictionary<Type, Dictionary<string, MemberInfo>>();

	//private static FlowGraphSettings flowGraphSettings = null;
	//public static FlowGraphSettings FlowGraphSettings => flowGraphSettings;

	private static bool initialized = false;

	public static void CacheTypeInfo() 
	{
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (!ReflectionUtils.ProjectAssemblies.Contains(assembly.GetName().Name))
				continue;

			assemblies.Add(assembly);
		}

		CollectFlowModuleInfo();
		CollectPropertyItemMembers();
	}


	public static void InitializeIfNeeded()
	{
		if (!initialized)
		{
			CacheTypeInfo();

			/*
#if UNITY_EDITOR
			EditorApplication.delayCall += LoadFlowGraphSettings;
#else
			LoadFlowGraphSettings();
#endif
			*/

			initialized = true;
		}
	}

	private static void CollectPropertyItemMembers()
	{
		propertyItemMemberInfoMap.Clear(); 
		 
		foreach (var assembly in assemblies)     
		{
			var assemblyTypes = assembly.GetTypes();
			foreach (var type in assemblyTypes)
			{
				if (type.IsSubclassOf(typeof(PropertyItem)) || type == typeof(PropertyItem))
				{
					var nameToMemberMap = new Dictionary<string, MemberInfo>();

					var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
					foreach (var field in fields)
					{
						if (field.FieldType == typeof(bool) ||
							field.FieldType == typeof(LensManagerBool))
						{
							nameToMemberMap.Add(field.Name, field);
						}
					}

					var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
					foreach (var property in properties)
					{
						if (property.PropertyType == typeof(bool) ||
							property.PropertyType == typeof(float))
						{
							nameToMemberMap.Add(property.Name, property);
						}
					}

					var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
					foreach (var method in methods)
					{
						if (method.IsSpecialName || method.IsGenericMethod) // no properties
							continue;

						bool invalidArgs = false;
						var parameters = method.GetParameters();
						for (int i = 0; i < parameters.Length; i++)
                        {
							if(	parameters[i].ParameterType.IsSubclassOf(typeof(MonoBehaviour)) &&
								parameters[i].ParameterType != typeof(Item))
                            {
								invalidArgs = true;
                            }
                        }

						if (invalidArgs)
							continue;

						if (!nameToMemberMap.ContainsKey(method.Name) && !method.Name.Contains("Try") && // cursed way of blocking state changing methods
							(method.ReturnType == typeof(float) ||
							method.ReturnType == typeof(bool)))
						{
							nameToMemberMap.Add(method.Name, method);
						}
					}

					if (type != typeof(PropertyItem))
					{
						// Hack to let us easily check if a property exists on a thing
						var hasPropertyMethod = typeof(PropertyItem).GetMethod("HasProperty", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
						nameToMemberMap.Add(hasPropertyMethod.Name, hasPropertyMethod);
					}

					propertyItemNames.Add(type.Name);
					propertyItemTypes.Add(type);

					var memberNames = new List<string>();
					foreach (var kvp in nameToMemberMap)
						memberNames.Add(kvp.Key);

					propertyItemMemberNamesMap.Add(type, memberNames);
					propertyItemMemberInfoMap.Add(type, nameToMemberMap);
				}
			}
		}


		propertyItemNames.Sort();
		propertyItemTypes.Sort((x, y) => x.Name.CompareTo(y.Name)); 

		foreach (var kvp in propertyItemMemberInfoMap)
		{
			/*
			moduleNameToTypeMap.Add(kvp.Key.Name, kvp.Key);

			Dictionary<string, MethodInfo> methodNameToInfoMap = new Dictionary<string, MethodInfo>();

			foreach (var method in kvp.Value.MethodInfos)
				methodNameToInfoMap.Add(method.Name, method);

			moduleFunctionToMethodInfoLookup.Add(kvp.Key.Name, methodNameToInfoMap);
			*/
		}
	}

	public static List<string> GetPropertyItemNames()
	{
		InitializeIfNeeded();
		return propertyItemNames; 
	}

	public static List<Type> GetPropertyItemTypes()
	{
		InitializeIfNeeded();
		return propertyItemTypes; 
	}

	public static Type GetPropertyType(string name)
    {
		InitializeIfNeeded();

		int propertyIndex = propertyItemNames.IndexOf(name);
		if (propertyIndex == -1)
			return null;

		return propertyItemTypes[propertyIndex];
    }

	public static bool TryGetPropertyItemMembers(Type type, out Dictionary<string, MemberInfo> typeToMemberMap)
    {
		InitializeIfNeeded();

		if (propertyItemMemberInfoMap.TryGetValue(type, out typeToMemberMap))
			return true;

		return false;
    }

	public static List<string> GetPropertyItemMemberNames(Type type)
	{
		InitializeIfNeeded();

		if (propertyItemMemberNamesMap.TryGetValue(type, out List<string> typeToMemberMap))
			return typeToMemberMap;

		return null;
	}

	public static MemberInfo GetPropertyItemMember(Type type, string memberName)
    {
		Type typeIter = type;
		while (typeIter != null)
		{
			if (TryGetPropertyItemMembers(typeIter, out Dictionary<string, MemberInfo> propertyItemToMemberMap))
			{
				if(propertyItemToMemberMap.TryGetValue(memberName, out MemberInfo member))
					return member;
			}

			typeIter = typeIter.BaseType;
		}

		return null;
    }

	public static List<ModuleInfo> GetModuleInfos()
	{
		InitializeIfNeeded();
		List<ModuleInfo> modules = moduleTypeToInfoMap.Select(kvp => kvp.Value).ToList();
		modules.Sort((a,b) => String.Compare(a.TypeInfo.Name, b.TypeInfo.Name, StringComparison.Ordinal));
		return modules;
	}

	public static ModuleInfo GetModuleInfo(string module)
	{
		InitializeIfNeeded();

		Type type = GetModuleType(module);
		if (type != null)
			return GetModuleInfo(type);

		return null;
	}

	public static ModuleInfo GetModuleInfo(Type type)
	{
		InitializeIfNeeded();
		moduleTypeToInfoMap.TryGetValue(type, out ModuleInfo info);
		return info;
	}

	public static Type GetModuleType(string module)
	{
		InitializeIfNeeded();
		moduleNameToTypeMap.TryGetValue(module, out Type type);
		return type;
	}

	public static MethodInfo GetModuleFunction(string module, string function)
	{
		InitializeIfNeeded();

		if(	moduleFunctionToMethodInfoLookup.TryGetValue(module, out Dictionary<string, MethodInfo> moduleFunctionsMap) &&
			moduleFunctionsMap.TryGetValue(function, out MethodInfo methodInfo))
		{
			return methodInfo;
		}

		return null;
	}
	
	public static ParameterInfo[] GetMethodParameters(MethodInfo method, out object[] recycledParamsArray)
	{
		InitializeIfNeeded();
		
		if (methodInfoToParametersLookup.TryGetValue(method, out ParameterInfo[] parameters))
		{
			recycledParamsArray = methodInfoToRecycledParamsLookup[method];
			return parameters;
		}
		
		recycledParamsArray = null;
		return null;
	}

	private static void CollectFlowModuleInfo()
	{
		moduleTypeToInfoMap.Clear(); 
		moduleNameToTypeMap.Clear();
		moduleFunctionToMethodInfoLookup.Clear();

		// Note: pulling from just FlowModule's assembly isn't going to get all the right types
		foreach (var assembly in assemblies) 
		{
			var assemblyTypes = assembly.GetTypes();
			foreach (var type in assemblyTypes)
			{
				// NOTE: This is for flowgraph which is a tool not included in this project
				/*
				if (type.IsSubclassOf(typeof(FlowModule)))
				{
					ModuleInfo moduleInfo = null;

					var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
					foreach (var method in methods)
					{
						if (method.IsSpecialName) // dont want properties
							continue;

						var parameters = method.GetParameters();
						if (parameters != null && parameters.Length >= 1 &&
							parameters[0].ParameterType == typeof(FlowEffectInstance))
						{
							if (moduleInfo == null)
							{
								moduleInfo = new ModuleInfo();
								moduleInfo.TypeInfo = type.GetTypeInfo();
								moduleTypeToInfoMap.Add(type, moduleInfo);
							}

							moduleInfo.MethodInfos.Add(method);
						}
					}
					moduleInfo.MethodInfos.Sort((a,b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));
				}
				*/
			}
		}

		foreach(var kvp in moduleTypeToInfoMap)
		{
			moduleNameToTypeMap.Add(kvp.Key.Name, kvp.Key);

			Dictionary<string, MethodInfo> methodNameToInfoMap = new Dictionary<string, MethodInfo>();
			 
			foreach(var method in kvp.Value.MethodInfos)
			{
				methodNameToInfoMap.Add(method.Name, method);
				ParameterInfo[] parameters = method.GetParameters();
				methodInfoToParametersLookup.Add(method, parameters);
				methodInfoToRecycledParamsLookup.Add(method, new object[parameters.Length]);
			}

			moduleFunctionToMethodInfoLookup.Add(kvp.Key.Name, methodNameToInfoMap);
		}
	}
}