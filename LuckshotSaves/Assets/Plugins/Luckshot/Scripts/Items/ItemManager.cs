using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ItemManager : Singleton<ItemManager>
{
	private Dictionary<string, ItemData> nameToDataMap = new Dictionary<string, ItemData>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<ItemData, Item> dataToPrefabMap = new Dictionary<ItemData, Item>();

	[SerializeField]
	private ItemDataPrefabMatrix itemDataPrefabMatrix = null;

	private List<Item> allItems = new List<Item>();
	public List<Item> AllItems => allItems;
	
	public IReadOnlyDictionary<string, ItemData> NameToDataMap => nameToDataMap;
	public IReadOnlyDictionary<ItemData, Item> DataToPrefabMap => dataToPrefabMap;

	[SerializeField]
	private float minItemCleanupHeight = -100f;

	[SerializeField]
	private int cleanupChecksPerFrame = 2;
	private int cleanupCheckIter = 0;

	private Collider[] searchColliders = new Collider[100];
	private HashSet<Item> searchHitItems = new HashSet<Item>();

	public void RegisterItem(Item item)
	{
		if (!allItems.Contains(item))
			allItems.Add(item);
	}

	public void DeregisterItem(Item item)
	{ allItems.Remove(item); }

	public ItemData GetItemDataByName(string name)
	{
		nameToDataMap.TryGetValue(name, out ItemData itemData);
		return itemData;
	}

	public Item GetItemPrefab(ItemData itemData)
	{
		if (itemDataPrefabMatrix.TryGetPrefab(itemData, out Item item))
			return item;

		if (dataToPrefabMap.TryGetValue(itemData, out item))
			return item;

		ItemData parentItemData = itemData.parentItemData;
		while(parentItemData != null)
		{
			if (dataToPrefabMap.TryGetValue(parentItemData, out item))
				return item;

			parentItemData = parentItemData.parentItemData;
		}

		return null;
	}

	private Dictionary<string, Type> propertyNameToType = new Dictionary<string, Type>();

	public Type GetTypeFromPropertyName(string name)
	{
		Type type = null;
		propertyNameToType.TryGetValue(name, out type);
		return type;
	}

	protected override void Awake()
	{
		base.Awake();

		if (Instance != this)
			return;

		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			foreach (var type in assembly.GetTypes())
			{
				if (type.IsSubclassOf(typeof(PropertyItem)))
					propertyNameToType.Add(type.Name, type);
			}
		}

		Item[] itemPrefabs = Resources.LoadAll<Item>("Prefabs");
		for(int i = 0; i < itemPrefabs.Length; i++)
		{
			Item itemPrefab = itemPrefabs[i];
			ItemData itemData = itemPrefab.Data;

			try
			{
				bool matrixLookup = itemDataPrefabMatrix.TryGetPrefab(itemData, out Item remapPrefab);
				if (matrixLookup)
					itemPrefab = remapPrefab;

				if (!nameToDataMap.ContainsKey(itemData.name))
				{
					nameToDataMap.Add(itemData.name, itemData);
					dataToPrefabMap.Add(itemData, itemPrefab);
				}
				else if(!matrixLookup)
				{
					Debug.LogError($"{itemData.Name} already mapped to {dataToPrefabMap[itemData]}. Need to remap {itemPrefab} in ItemDataPrefabMatrix.");
				}
			}
			catch(Exception exception)
            {
				// try catch so one broken item doesn't break all items
				Debug.LogException(exception, itemPrefab);
            }
		}
	}

	private void Update()
	{
		int cleanupChecks = 0;
		while(cleanupChecks < cleanupChecksPerFrame && allItems.Count > 0)
		{
			cleanupCheckIter = (int)Mathf.Repeat(cleanupCheckIter, allItems.Count);

			Item item = allItems[cleanupCheckIter];
			if (item.transform.position.y < minItemCleanupHeight)
			{
				if (item.GetProperty<SafeItem>())
					return;

				Destroy(item.gameObject);
				cleanupCheckIter--;
			}

			cleanupChecks++;
			cleanupCheckIter++;
		}
	}

	public Item InstantiateItemFromItemState(ItemState itemState)
	{
		ItemData itemData = GetItemDataByName(itemState.itemName);
		if (itemData != null)
		{
			Item itemPrefab = GetItemPrefab(itemData);

			Item item = Instantiate(itemPrefab);
			itemState.ApplyStateToItem(item);

			return item;
		}

		return null;
	}
}
