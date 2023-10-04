using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSensor : MonoBehaviour
{
	[System.Serializable]
	public class ItemColliderCollection
	{
		public Item propertyItem;
		public List<Collider> colliders = new List<Collider>();
	}

	[SerializeField]
	private List<Item> collection = new List<Item>();
	public List<Item> Collection
	{
		get
		{
			for (int i = 0; i < collection.Count; i++)
			{
				if (collection[i] == null)
				{
					Item unsensed = collection[i];

					collection.RemoveAt(i--);
					OnItemExited(this, unsensed);
				}
			}

			return collection;
		}
	}

#if UNITY_EDITOR
	[SerializeField]
	private List<ItemColliderCollection> colliderCollections = new List<ItemColliderCollection>();
#endif

	private Dictionary<Item, ItemColliderCollection> itemToColliderCollection = new Dictionary<Item, ItemColliderCollection>();

	public ItemColliderCollection GetItemColliderCollection(Item item)
	{
		if (itemToColliderCollection.TryGetValue(item, out ItemColliderCollection itemColliderCollection))
			return itemColliderCollection;

		return null;
	}

	private List<Collider> collidersToRemove = new List<Collider>();
	private List<Item> itemsToRemove = new List<Item>();

	public delegate void ItemChangedEvent(ItemSensor sensor, Item item);

	public event ItemChangedEvent OnItemEntered = delegate {};
	public event ItemChangedEvent OnItemExited = delegate {};

	//[SerializeField]
	private bool detectTriggers = true;

	private List<Collider> triggers = new List<Collider>();
	public List<Collider> Triggers
	{
		get
		{
			if (triggers.Count == 0)
				CollectTriggers();

			return triggers;
		}

	}

	private void Awake()
	{
		CollectTriggers();
	}

	public void CollectTriggers()
	{
		triggers.Clear();

		Collider[] colliders = transform.GetComponentsInChildren<Collider>(true);
		for (int i = 0; i < colliders.Length; i++)
		{
			Collider collider = colliders[i];

			if (!collider.isTrigger)
				continue;

			if (collider.attachedRigidbody != null &&
				collider.attachedRigidbody.transform != transform)
				continue;

			triggers.Add(colliders[i]);
		}
	}

	private void OnDisable()
	{
		itemsToRemove.Clear();

		for (int i = 0; i < collection.Count; i++)
		{
			if (collection[i] != null)
				itemsToRemove.Add(collection[i]);
		}

		foreach (var item in itemsToRemove)
			RemoveItem(item);

		collection.Clear();
		itemToColliderCollection.Clear();

#if UNITY_EDITOR
		colliderCollections.Clear();
#endif
	}

	private void SensedItem_OnCollidersChanged(Item item)
	{
		CheckCollidersValid(item);
	}

	private void SensedItem_OnRigidbodyRemoved(Item item)
	{
		CheckCollidersValid(item);
	}

	private void SensedItem_OnChildRemoved(Item parentItem, Item childItem)
	{
		RemoveItem(childItem);
	}

	private void SensedItem_OnChildAdded(Item parentItem, Item childItem)
	{
		// only add item child if the trigger / layer settings are appropriate

		if (!collection.Contains(childItem))
		{
			bool match = false;
			foreach (var collider in childItem.AllColliders)
			{
				if (collider.isTrigger && !detectTriggers)
					continue;

				bool anyCollision = false;
				foreach (var trigger in triggers)
				{
					if (!Physics.GetIgnoreCollision(trigger, collider))
					{
						anyCollision = true;
						break;
					}
				}

				if (anyCollision)
				{
					match = true;
					break;
				}
			}

			if (match)
				AddItem(childItem);
		}
	}

	private void SensedItem_OnItemDisabled(Item item)
	{
		CheckCollidersValid(item);
	}

	private void SensedItem_OnItemDestroyed(Item item)
	{
		if(itemToColliderCollection.TryGetValue(item, out ItemColliderCollection collection))
		{
			collection.colliders.Clear();
			RemoveItem(item);
		}
	}

	private bool CheckCollidersValid(Item item)
	{
		if (itemToColliderCollection.TryGetValue(item, out ItemColliderCollection colliderCollection))
		{
			collidersToRemove.Clear();

			foreach (var collider in colliderCollection.colliders)
			{
				if (collider == null || !collider.enabled ||
					!collider.gameObject.activeInHierarchy)
				{
					collidersToRemove.Add(collider);
					continue;
				}

				if (!detectTriggers && collider.isTrigger)
				{
					collidersToRemove.Add(collider);
					continue;
				}

				if (!item.AllColliders.Contains(collider))
				{
					collidersToRemove.Add(collider);
					continue;
				}

				// this case shouldn't be possible and is expensive to check
				/*
				if(!Item.FindNearestParentItem(collider, out Item parentItem) || parentItem != item)
				{
					collidersToRemove.Add(collider);
					continue;
				}
				*/
			}

			for (int i = 0; i < collidersToRemove.Count; i++)
				colliderCollection.colliders.Remove(collidersToRemove[i]);

			if (colliderCollection.colliders.Count == 0)
			{
				// Manually check if there's any other overlaps since this check might be happening as colliders are swapped
				// as is a very common case on the player
				for (int i = 0; i < item.AllColliders.Count; i++)
				{
					Collider itemCol = item.AllColliders[i];
					if (!colliderCollection.colliders.Contains(itemCol) &&
						IsColliderValidAndOverlappingSensor(itemCol))
					{
						colliderCollection.colliders.Add(itemCol);
					}
				}
			}

			if (colliderCollection.colliders.Count > 0)
				return true;
		}

		if (item.ParentItem != null &&
			collection.Contains(item.ParentItem) &&
			CheckCollidersValid(item.ParentItem))
		{
			return true;
		}

		RemoveItem(item);
		return false;
	}

	private bool IsColliderValidAndOverlappingSensor(Collider itemCol)
	{
		if (itemCol.enabled && itemCol.gameObject.activeInHierarchy &&
			(detectTriggers || !itemCol.isTrigger))
		{
			for (int j = 0; j < triggers.Count; j++)
			{
				Collider triggerCol = triggers[j];
				if (PhysicsUtils.CheckColliderOverlap(itemCol, triggerCol))
					return true;
			}
		}

		return false;
	}

	private void AddItem(Item item)
	{
		// TODO: Subscribing/unsubscribing like this is going to generate lots of garbage
		// should make a wrapper for delegates so we can avoid the list rebuild C#
		// uses in the delegate += operator

		item.OnItemDisabled += SensedItem_OnItemDisabled; 
		item.OnItemDestroyed += SensedItem_OnItemDestroyed;

		item.OnCollidersChanged += SensedItem_OnCollidersChanged;
		item.OnRigidbodyRemoved += SensedItem_OnRigidbodyRemoved;

		item.OnChildAdded += SensedItem_OnChildAdded;
		item.OnChildRemoved += SensedItem_OnChildRemoved;

		collection.Add(item);
		OnItemEntered(this, item);

		foreach(var childItem in item.ChildItems)
			AddItem(childItem);
	}

	private void RemoveItem(Item item)
	{
		foreach (var childItem in item.ChildItems)
			RemoveItem(childItem);

		item.OnItemDisabled -= SensedItem_OnItemDisabled;
		item.OnItemDestroyed -= SensedItem_OnItemDestroyed;

		item.OnCollidersChanged -= SensedItem_OnCollidersChanged;
		item.OnRigidbodyRemoved -= SensedItem_OnRigidbodyRemoved;

		item.OnChildAdded -= SensedItem_OnChildAdded;
		item.OnChildRemoved -= SensedItem_OnChildRemoved;

		collection.Remove(item);
		OnItemExited(this, item);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!detectTriggers && other.isTrigger)
			return;

		if (Item.FindNearestParentItem(other, out Item item))
		{
			if (itemToColliderCollection.TryGetValue(item, out ItemColliderCollection colliderCollection))
			{
				if(!colliderCollection.colliders.Contains(other))
					colliderCollection.colliders.Add(other);
			}
			else
			{
				colliderCollection = new ItemColliderCollection();
				colliderCollection.propertyItem = item;
				colliderCollection.colliders.Add(other);

				itemToColliderCollection.Add(item, colliderCollection);

#if UNITY_EDITOR
				colliderCollections.Add(colliderCollection);
#endif
			}

			if (!collection.Contains(item))
				AddItem(item);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (Item.FindNearestParentItem(other, out Item item))
		{
			if (itemToColliderCollection.TryGetValue(item, out ItemColliderCollection itemInfo))
			{
				if(!IsColliderValidAndOverlappingSensor(other) &&
					itemInfo.colliders.Remove(other) &&
					itemInfo.colliders.Count == 0)
				{
					RemoveItem(item);
				}
			}
		}
	}
}
