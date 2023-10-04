using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

[DefaultExecutionOrder(-1)]
[RequireComponent(typeof(Item))]
public abstract class PropertyItem : MonoBehaviour
{
	private Item item = null;
	public Item Item => item;

	public bool HasProperty(Item item)
    {  return item.TypeToPropertyMap.TryGetValue(GetType(), out PropertyItem propertyItem); }

	public bool MatchesItemData(ItemData itemData, bool requireExact = false)
	{
		if (requireExact)
			return itemData == item.Data;
		else
			return ItemData.HaveSameRoot(itemData, item.Data);
	}

	public bool IsFarEnoughFromObject(Item item, UniqueObjectData uod, float distance = 7f)
	{
		UniqueObject uniqueObject = UniqueObjectManager.Instance.LookupUniqueObject(uod);
		if (uniqueObject == null)
			return true;

		return Vector3.Distance(item.Center, uniqueObject.transform.position) > distance;
	}

	public bool IsCloseEnoughToObject(Item item, UniqueObjectData uod, float distance = 7f)
	{
		UniqueObject uniqueObject = UniqueObjectManager.Instance.LookupUniqueObject(uod);
		if (uniqueObject == null)
			return false;

		return Vector3.Distance(item.Center, uniqueObject.transform.position) < distance;
	}

	public event Action<PropertyItem> OnPropertyItemAdded = delegate { };
	public event Action<PropertyItem> OnPropertyItemDestroyed = delegate { };

	public event Action<PropertyItem> OnStateChanged = delegate {};

	protected bool hasAwoken = false;

	public void Awake()
	{
		if (hasAwoken)
			return;

		hasAwoken = true;

		AwakeIfNeeded();
	}

	protected virtual void AwakeIfNeeded()
	{
		item = GetComponent<Item>();
		item.RegisterProperty(this);

		OnPropertyItemAdded(this);
	}

	public virtual void OnLoaded()
	{
	}

	protected void StateChanged()
	{
		OnStateChanged(this);
	}

	protected virtual void OnDestroy()
	{
		OnPropertyItemDestroyed(this);

		if(item != null)
			item.DeregisterProperty(this);
	}
}
