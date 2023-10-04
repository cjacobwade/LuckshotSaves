using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

[System.Serializable]
public class ItemState
{
	public string uniqueName = string.Empty;
	public string itemName = string.Empty;
	public List<PropertyState> propertyStates = new List<PropertyState>();

	public void ApplyStateToItem(Item item)
	{
		for (int i = 0; i < propertyStates.Count; i++)
		{
			string typeString = propertyStates[i].type;

			PropertyItem property = item.GetComponent(typeString) as PropertyItem;
			if (property == null)
			{
				Type type = ItemManager.Instance.GetTypeFromPropertyName(typeString);
				if (type.IsSubclassOf(typeof(PropertyItem)))
					property = item.gameObject.AddComponent(type) as PropertyItem;
			}

			if (property != null)
				propertyStates[i].ApplyStateToPropertyItem(property);
		}

		item.OnLoaded();
	}

	public PropertyState this[Type propertyType]
	{ get { return this[propertyType.Name]; } }

	public PropertyState this[string propertyName]
	{
		get
		{
			for(int i = 0; i < propertyStates.Count; i++)
			{
				if (propertyStates[i].type == propertyName)
					return propertyStates[i];
			}

			return null;
		}
	}
}

public static class ItemStateUtils
{
	public static ItemState BuildItemState(this Item item)
	{
		ItemState itemState = new ItemState();

		PersistentItem persistentItem = item.GetProperty<PersistentItem>();

		itemState.itemName = item.Data.name;

		if (persistentItem != null)
			itemState.uniqueName = persistentItem.UniqueObject.Data.name;

		foreach (var kvp in item.TypeToPropertyMap)
		{
			try
			{
				itemState.propertyStates.Add(kvp.Value.BuildPropertyState());
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("Issue building {0} item {1} property state: {2}", item.name, kvp.Key.Name, e);
			}
		}

		return itemState;
	}
}
