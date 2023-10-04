using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveZone : PropertyItem
{
	[SerializeField]
	ItemSensor sensor = null;

	[SaveLoad]
	ItemState[] ContainedItems
	{
		get
		{
			List<ItemState> itemStates = new List<ItemState>();

			List<Item> items = sensor.Collection;
			for(int i = 0; i < items.Count; i++)
				itemStates.Add(items[i].BuildItemState());

			return itemStates.ToArray();
		}

		set
		{
			for(int i = 0; i < value.Length; i++)
				ItemManager.Instance.InstantiateItemFromItemState(value[i]);
		}
	}
}
