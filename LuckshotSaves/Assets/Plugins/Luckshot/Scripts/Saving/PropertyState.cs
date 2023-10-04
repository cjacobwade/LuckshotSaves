using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.IO;
using System.Linq;

[System.Serializable]
public class PropertyState
{
	[System.Serializable]
	public class Field
	{
		public string name;
		public string type;
		public string value;

		public object GetValue()
		{
			object objValue = StringToValue(value, GetFieldType());
			return objValue;
		}

		public Type GetFieldType()
		{
			Type fieldType = Type.GetType(type);
			return fieldType;
		}
	}

	[System.Serializable]
	public class ArrWrapper
	{
		public string type = string.Empty;
		public List<Field> fields = new List<Field>();

		public Type GetElementType()
		{
			Type arrType = Type.GetType(type);
			if(arrType.IsArray)
			{
				return arrType.GetElementType();
			}
			else if(typeof(IList).IsAssignableFrom(arrType))
			{
				return arrType.GetGenericArguments()[0];
			}

			return null;
		}
	}

	public static class ArrWrapperUtils
	{
		public static string ValueToString(object value)
		{
			Type arrType = value.GetType();

			Type elementType = arrType.GetElementType();
			if (!arrType.IsArray && typeof(IList).IsAssignableFrom(arrType))
				elementType = arrType.GetGenericArguments()[0];

			ArrWrapper wrapper = new ArrWrapper();
			wrapper.type = arrType.AssemblyQualifiedName;

			int arrLength = 0;
			IEnumerable enumerable = value as IEnumerable;
			foreach (var obj in enumerable)
			{
				Field field = new Field();
				field.type = elementType.AssemblyQualifiedName;
				field.name = $"Element{arrLength}";
				field.value = PropertyState.ValueToString(obj);

				wrapper.fields.Add(field);
				arrLength++;
			}

			return JsonUtility.ToJson(wrapper);
		}

		public static object StringToValue(string text)
		{
			ArrWrapper wrapper = JsonUtility.FromJson<ArrWrapper>(text);
			if (wrapper != null)
			{
				Type wrapperType = Type.GetType(wrapper.type);
				if (wrapperType.IsArray)
				{
					Type elementType = wrapperType.GetElementType();
					Array arr = Array.CreateInstance(elementType, wrapper.fields.Count);

					int elementIndex = 0;
					foreach (var field in wrapper.fields)
						arr.SetValue(PropertyState.StringToValue(field.value, elementType), elementIndex++);

					return arr;
				}
				else if(typeof(IList).IsAssignableFrom(wrapperType))
				{
					Type elementType = wrapperType.GetGenericArguments()[0];

					Type listType = typeof(List<>);
					Type constructedListType = listType.MakeGenericType(elementType);

					var list = (IList)Activator.CreateInstance(constructedListType);
					foreach(var field in wrapper.fields)
						list.Add(PropertyState.StringToValue(field.value, elementType));

					return list;
				}
			}

			return null;
		}
	}

	[System.Serializable]
	public class ClassWrapper
	{
		public string type = string.Empty;
		public List<Field> fields = new List<Field>();
	}

	public static class ClassWrapperUtils
	{
		public static string ValueToString(object value)
		{
			ClassWrapper wrapper = new ClassWrapper();

			Type valueType = value.GetType();
			wrapper.type = valueType.AssemblyQualifiedName;

			FieldInfo[] fieldInfos = value.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var fieldInfo in fieldInfos)
			{
				Type fieldType = fieldInfo.FieldType;

				Field field = new Field();
				field.name = fieldInfo.Name;
				field.type = fieldType.AssemblyQualifiedName;
				field.value = PropertyState.ValueToString(fieldInfo.GetValue(value));

				wrapper.fields.Add(field);
			}

			return JsonUtility.ToJson(wrapper);
		}

		public static object StringToValue(string text)
		{
			ClassWrapper wrapper = JsonUtility.FromJson<ClassWrapper>(text);
			if (wrapper != null)
			{
				Type classType = Type.GetType(wrapper.type);
				var classObj = Activator.CreateInstance(classType); 

				foreach (var field in wrapper.fields)
				{
					FieldInfo fieldInfo = classType.GetField(field.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					if (fieldInfo != null)
					{
						object value = PropertyState.StringToValue(field.value, fieldInfo.FieldType);
						fieldInfo.SetValue(classObj, value);
					}
				}
				return classObj;
			}

			return null;
		}
	}

	public string type = string.Empty;
	public List<Field> properties = new List<Field>();
	public List<Field> fields = new List<Field>();

	private static BindingFlags allInstanceBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
	private static Type unityObjType = typeof(UnityEngine.Object);

	public void ApplyStateToPropertyItem(PropertyItem propertyItem)
	{
		Type propertyItemType = propertyItem.GetType();
		for (int i = 0; i < properties.Count; i++)
		{
			Field propertyProperty = properties[i];
			PropertyInfo property = propertyItemType.GetProperty(propertyProperty.name, allInstanceBindingFlags);

			if (property == null ||
				property.SetMethod == null)
			{
				continue;
			}

			if (string.IsNullOrEmpty(propertyProperty.value))
			{
				property.SetValue(propertyItem, GetDefaultValue(propertyProperty.GetFieldType()));
			}
			else
			{
				object value = propertyProperty.GetValue();
				property.SetValue(propertyItem, value);
			}
		}

		for (int i = 0; i < fields.Count; i++)
		{
			Field propertyField = fields[i];
			FieldInfo field = propertyItemType.GetField(propertyField.name, allInstanceBindingFlags);

			if (string.IsNullOrEmpty(propertyField.value))
			{
				field.SetValue(propertyItem, GetDefaultValue(propertyField.GetFieldType()));
			}
			else
			{
				object value = propertyField.GetValue();
				field.SetValue(propertyItem, value);
			}
		}

		propertyItem.OnLoaded();
	}

	public static string ValueToString(object value)
	{
		if (value == null)
			return string.Empty;

		Type type = value.GetType();

		if (type.IsArray || typeof(IList).IsAssignableFrom(type))
		{
			return ArrWrapperUtils.ValueToString(value);
		}
		else if (type == typeof(string))
		{
			return value.ToString();
		}
		else if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
		{
			if (unityObjType.IsAssignableFrom(type))
				return ((UnityEngine.Object)value).name;

			return ClassWrapperUtils.ValueToString(value);
		}
		else
		{
			return value.ToString();
		}
	}

	public static object StringToValue(string text, Type type)
	{
		if (type.IsArray || typeof(IList).IsAssignableFrom(type))
		{
			return ArrWrapperUtils.StringToValue(text);
		}
		else if (type == typeof(string))
		{
			return text;
		}
		else if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
		{
			if (unityObjType.IsAssignableFrom(type))
			{
				var attrib = type.GetCustomAttribute(typeof(SaveLoadAssetAttribute)) as SaveLoadAssetAttribute;
				if (attrib != null)
				{
					// so cursed lols
					var allObjs = Resources.LoadAll(attrib.resourcePath, type);
					foreach (var obj in allObjs)
					{
						if (obj.name == text)
							return obj;
					}

					return null;
				}
			}

			return ClassWrapperUtils.StringToValue(text);
		}
		else
		{
			return Convert.ChangeType(text, type);
		}
	}

	object GetDefaultValue(Type t)
	{
		if (t.IsValueType)
			return Activator.CreateInstance(t);

		return null;
	}

	public object this[string fieldName]
	{
		get
		{
			for(int i = 0; i < fields.Count; i++)
			{
				var field = fields[i];
				if(field.name == fieldName)
				{
					object value = StringToValue(field.value, field.GetFieldType());
					return value;
				}
			}

			for(int i = 0;i < properties.Count; i++)
			{
				var property = properties[i];
				if (property.name == fieldName)
				{
					object value = StringToValue(property.value, property.GetFieldType());
					return value;
				}
			}

			return null;
		}
	}
}

public static class PropertyStateUtils
{
	public static PropertyState BuildPropertyState(this PropertyItem propertyItem)
	{
		PropertyState propertyState = new PropertyState();

		Type propertyItemType = propertyItem.GetType();
		if (propertyItemType.IsGenericType)
			propertyItemType = propertyItemType.GetGenericArguments()[0];

		propertyState.type = propertyItemType.Name;

		BindingFlags allInstanceBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

		while (propertyItemType != typeof(MonoBehaviour))
		{
			// PropertyItem members

			if (FlowTypeCache.TryGetPropertyItemMembers(propertyItemType, out Dictionary<string, MemberInfo> nameToMemberMap))
			{
				foreach (var kvp in nameToMemberMap)
				{
					string memberName = kvp.Key;
					MemberInfo memberInfo = kvp.Value;

					if (memberInfo.GetCustomAttribute(typeof(SaveLoadAttribute), true) == null)
						continue;

					PropertyState.Field property = new PropertyState.Field();
					property.name = memberName;

					MethodInfo methodInfo = memberInfo as MethodInfo;
					if (methodInfo != null)
					{
						bool invalidArgs = false;

						ParameterInfo[] parameterInfos = methodInfo.GetParameters();
						object[] parameters = new object[parameterInfos.Length];
						for (int i = 0; i < parameterInfos.Length; i++)
						{
							if (parameterInfos[i].ParameterType == typeof(Item))
								parameters[i] = propertyItem.Item;
							else
								invalidArgs = true;
						}

						if (invalidArgs)
							continue;

						property.type = methodInfo.ReturnType.AssemblyQualifiedName;
						property.value = PropertyState.ValueToString(methodInfo.Invoke(propertyItem, parameters));
					}
					else
					{
						FieldInfo fieldInfo = memberInfo as FieldInfo;
						if (fieldInfo != null)
						{
							property.type = fieldInfo.FieldType.AssemblyQualifiedName;
							property.value = PropertyState.ValueToString(fieldInfo.GetValue(propertyItem));
						}
						else
						{
							PropertyInfo propertyInfo = memberInfo as PropertyInfo;
							if (propertyInfo != null)
							{
								property.type = propertyInfo.PropertyType.AssemblyQualifiedName;
								property.value = PropertyState.ValueToString(propertyInfo.GetValue(propertyItem));
							}
						}
					}

					propertyState.properties.Add(property);
				}
			}

			// Explicit [SaveLoad] Fields

			PropertyInfo[] properties = propertyItemType.GetProperties(allInstanceBindingFlags);
			for (int i = 0; i < properties.Length; i++)
			{
				if (properties[i].GetMethod == null)
					continue;

				if (Attribute.IsDefined(properties[i], typeof(SaveLoadAttribute)))
				{
					PropertyState.Field property = new PropertyState.Field();
					property.name = properties[i].Name;
					property.type = properties[i].PropertyType.AssemblyQualifiedName;
					property.value = PropertyState.ValueToString(properties[i].GetValue(propertyItem));

					propertyState.properties.Add(property);
				}
			}

			FieldInfo[] fields = propertyItemType.GetFields(allInstanceBindingFlags);
			for (int i = 0; i < fields.Length; i++)
			{
				if (Attribute.IsDefined(fields[i], typeof(SaveLoadAttribute)))
				{
					PropertyState.Field field = new PropertyState.Field();
					field.name = fields[i].Name;
					field.type = fields[i].FieldType.AssemblyQualifiedName;
					field.value = PropertyState.ValueToString(fields[i].GetValue(propertyItem));

					propertyState.fields.Add(field);
				}
			}

			propertyItemType = propertyItemType.BaseType;

			while (propertyItemType.IsGenericType)
				propertyItemType = propertyItemType.BaseType;
		}

		return propertyState;
	}
}