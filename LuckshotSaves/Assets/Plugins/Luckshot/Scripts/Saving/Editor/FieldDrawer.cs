using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Field = PropertyState.Field;
using System;
using static PropertyState;
using System.Reflection;

[CustomPropertyDrawer(typeof(Field))]
public class FieldDrawer : PropertyDrawer
{
	private static Type unityObjType = typeof(UnityEngine.Object);

	private static Dictionary<string, bool> fieldToOpenMap = new Dictionary<string, bool>();

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// name
		// type
		// value

		// perhaps a checkbox to draw raw?

		EditorGUI.BeginProperty(position, label, property);

		string path = property.propertyPath;

		Field field = EditorUtils.GetTargetObjectOfProperty(property) as Field;
		if (field != null)
			DrawField(position, field, path);

		EditorGUI.EndProperty();
	}

	private void DrawIndentedBox(Rect position, Field field, string path)
	{
		Rect boxPosition = EditorGUI.IndentedRect(position);
		boxPosition.height = GetFieldHeight(field, path, false) - EditorGUIUtility.singleLineHeight;

		Color backgroundColor = EditorUtils.DefaultBackgroundColor;
		backgroundColor *= Mathf.Lerp(1f, 0f, (float)EditorGUI.indentLevel / 6f);

		EditorGUI.DrawRect(boxPosition, backgroundColor);
	}

	private void DrawField(Rect position, Field field, string path)
	{
		Type fieldType = Type.GetType(field.type);

		path = $"{path}.{field.name}";

		if (fieldType.IsArray || typeof(IList).IsAssignableFrom(fieldType))
		{
			ArrWrapper wrapper = JsonUtility.FromJson<ArrWrapper>(field.value);
			if (wrapper != null)
			{
				bool open = true;

				position.height = EditorGUIUtility.singleLineHeight;
				if (wrapper.fields.Count > 0)
				{
					if (!fieldToOpenMap.TryGetValue(path, out open))
						fieldToOpenMap[path] = open = true;

					open = EditorGUI.Foldout(position, open, $"{field.name} ({wrapper.GetElementType().Name})", true);
					fieldToOpenMap[path] = open;
				}
				else
				{
					EditorGUI.LabelField(position, $"{field.name} ({wrapper.GetElementType().Name})");
				}

				position.y += position.height;

				if (open)
				{
					EditorGUI.indentLevel++;

					DrawIndentedBox(position, field, path);

					foreach (var subField in wrapper.fields)
					{
						float subFieldHeight = GetFieldHeight(subField, path);
						position.height = subFieldHeight;
						DrawField(position, subField, path);
						position.y += position.height;
					}

					EditorGUI.indentLevel--;
				}
			}
		}
		else if (fieldType == typeof(string))
		{
			position.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.LabelField(position, $"{field.name}: {field.value}");
			position.y += position.height;
		}
		else if (fieldType.IsClass || (fieldType.IsValueType && !fieldType.IsPrimitive))
		{
			if (unityObjType.IsAssignableFrom(fieldType))
			{
				position.height = EditorGUIUtility.singleLineHeight;
				EditorGUI.LabelField(position, $"{field.name}: {field.value} ({fieldType.Name})");
				position.y += position.height;
			}
			else
			{
				ClassWrapper wrapper = JsonUtility.FromJson<ClassWrapper>(field.value);
				if (wrapper != null)
				{
					position.height = EditorGUIUtility.singleLineHeight;
					if (!fieldToOpenMap.TryGetValue(path, out bool open))
						fieldToOpenMap[path] = open = true;

					open = EditorGUI.Foldout(position, open, $"{field.name} ({fieldType.Name})", true);
					fieldToOpenMap[path] = open;
					position.y += position.height;

					if (open)
					{
						EditorGUI.indentLevel++;

						DrawIndentedBox(position, field, path);

						foreach (var subField in wrapper.fields)
						{
							float subFieldHeight = GetFieldHeight(subField, path);
							position.height = subFieldHeight;
							DrawField(position, subField, path);
							position.y += position.height;
						}

						EditorGUI.indentLevel--;
					}
				}
			}
		}
		else
		{
			position.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.LabelField(position, $"{field.name}: {field.value}");
			position.y += position.height;
		}
	}

	private float GetFieldHeight(Field field, string path, bool changePath = true)
	{
		Type fieldType = Type.GetType(field.type);

		if(changePath)
			path = $"{path}.{field.name}";

		if (fieldType.IsArray || typeof(IList).IsAssignableFrom(fieldType))
		{
			ArrWrapper wrapper = JsonUtility.FromJson<ArrWrapper>(field.value);
			if (wrapper != null)
			{
				float fieldHeight = EditorGUIUtility.singleLineHeight;

				if (!fieldToOpenMap.TryGetValue(path, out bool open) || open)
				{
					foreach (var subField in wrapper.fields)
						fieldHeight += GetFieldHeight(subField, path);
				}

				return fieldHeight;
			}
		}
		else if (fieldType == typeof(string))
		{
			return EditorGUIUtility.singleLineHeight;
		}
		else if (fieldType.IsClass || (fieldType.IsValueType && !fieldType.IsPrimitive))
		{
			if (unityObjType.IsAssignableFrom(fieldType))
			{
				return EditorGUIUtility.singleLineHeight;
			}
			else
			{
				ClassWrapper wrapper = JsonUtility.FromJson<ClassWrapper>(field.value);
				if (wrapper != null)
				{
					float fieldHeight = EditorGUIUtility.singleLineHeight;

					if (!fieldToOpenMap.TryGetValue(path, out bool open) || open)
					{
						foreach (var subField in wrapper.fields)
							fieldHeight += GetFieldHeight(subField, path);
					}

					return fieldHeight;
				}
			}
		}
		else
		{
			return EditorGUIUtility.singleLineHeight;
		}

		return 0f;
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		string path = property.propertyPath;

		Field field = EditorUtils.GetTargetObjectOfProperty(property) as Field;
		if (field != null)
			return GetFieldHeight(field, path);

		return base.GetPropertyHeight(property, label);
	}
}
