using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorldState
{
	public List<ItemState> itemStates = new List<ItemState>();

	public ItemState this[ItemData itemData]
	{ get { return this[itemData.name]; } }

	public ItemState this[string itemName]
	{
		get
		{
			for(int i =0; i < itemStates.Count; i++)
			{
				if (itemStates[i].itemName == itemName)
					return itemStates[i];
			}

			return null;
		}
	}
}
