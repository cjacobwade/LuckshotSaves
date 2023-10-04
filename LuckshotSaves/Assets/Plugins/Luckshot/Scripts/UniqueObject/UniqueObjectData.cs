using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[SaveLoadAsset(resourcePath = "UniqueObjectDatas/")]
[CreateAssetMenu(fileName = "UniqueObjectData", menuName = "Luckshot/Unique Object Data")]
//[InlineScriptableObject(path ="Assets/Resources/UniqueObjectDatas/", inline = false)]
public class UniqueObjectData : ScriptableObject
{
	[SerializeField]
	private string displayName = string.Empty;
	public string DisplayName => displayName;

	[ContextMenu("Set Display Name - Last Underscore")]
	private void SetDisplayName_LastUnderscore()
	{
		displayName = name.Split('_').Last();
	}

	[ContextMenu("Set Display Name - Full Name")]
	private void SetDisplayName_RemoveUnderscore()
	{
		displayName = name.Replace("_", " ");
	}

#if UNITY_EDITOR
	public static UniqueObjectData CreateUniqueObjectData(string path, string name)
	{
		Object prevSelection = Selection.activeObject;

		UniqueObjectData uniqueObjectData = AssetDatabase.LoadAssetAtPath<UniqueObjectData>(path + "/" + name + ".asset");
		if (uniqueObjectData == null)
		{
			uniqueObjectData = ScriptableObjectUtils.CreateAsset<UniqueObjectData>(path, name);
			uniqueObjectData.name = name;
		}

		Selection.activeObject = prevSelection;
		return uniqueObjectData;
	}
#endif
}
