using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Big Hops/Item Data Prefab Matrix")]
public class ItemDataPrefabMatrix : ScriptableObject
{
	[System.Serializable]
	public class ItemDataPrefabPair
	{
		public ItemData itemData = null;
		public Item itemPrefab = null;
	}

	public List<ItemDataPrefabPair> itemDataPrefabPairs = new List<ItemDataPrefabPair>();

	public bool TryGetPrefab(ItemData itemData, out Item prefab)
	{
		foreach (ItemDataPrefabPair pair in itemDataPrefabPairs)
		{
			if (pair.itemData == itemData)
			{
				prefab = pair.itemPrefab;
				return true;
			}
		}

		prefab = null;
		return false;
	}

	public bool Contains(ItemData itemData)
	{
		foreach (ItemDataPrefabPair pair in itemDataPrefabPairs)
		{
			if (pair.itemData == itemData)
				return true;
		}
		return false;
	}
}
