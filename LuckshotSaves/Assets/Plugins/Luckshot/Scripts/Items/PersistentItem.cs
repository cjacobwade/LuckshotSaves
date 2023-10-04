using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentItem : PropertyItem
{
	[SerializeField]
	private UniqueObject uniqueObject = null;
	public UniqueObject UniqueObject => uniqueObject;

    protected override void AwakeIfNeeded()
    {
        base.AwakeIfNeeded();

		//if(!DestroyManager.IsMarkedForDestroy(gameObject))
			SaveManager.Instance.RegisterItem(this);
	}

    protected override void OnDestroy()
	{
		base.OnDestroy();

		if(SaveManager.Instance != null)
			SaveManager.Instance.DeregisterItem(this);
	}
}
