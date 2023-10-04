using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniqueObject : MonoBehaviour
{
	[SerializeField]
	private UniqueObjectData data = null;
	public UniqueObjectData Data => data;

#if UNITY_EDITOR
	//[Button("Find Or Create UniqueObjectData")]
	public void FindOrCreateItemData()
	{ data = UniqueObjectData.CreateUniqueObjectData("Assets/Resources/UniqueObjectDatas", gameObject.name); }
#endif

	private bool registered = false;

	private void Awake()
	{
		RegisterIfNeeded();
	}

	public void RegisterIfNeeded()
	{
		if (registered || this == null)
			return;

		if (data != null)
		{
			//Debug.Log($"Register UOD {data}");
			UniqueObjectManager.Instance.RegisterUniqueObject(this);
			registered = true;
		}
	}

	private void Unregister()
	{
		if(registered)
		{
			UniqueObjectManager.Instance.UnregisterUniqueObject(this);
			registered = false;
		}
	}

	public void SetData(UniqueObjectData setData)
	{
		if (data != setData)
		{
			Unregister();

			data = setData;

			RegisterIfNeeded();
		}
	}

	private void OnDestroy()
	{
		if (UniqueObjectManager.Instance != null)
			Unregister();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue.SetA(0.1f);
		Gizmos.DrawSphere(transform.position, 0.2f);
	}
}
