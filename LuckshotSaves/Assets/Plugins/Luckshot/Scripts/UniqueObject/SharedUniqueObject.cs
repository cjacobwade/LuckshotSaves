using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SharedUniqueObject : MonoBehaviour
{
	[SerializeField]
	private UniqueObject uniqueObject = null;

	[SerializeField, UnityEngine.Serialization.FormerlySerializedAs("uniqueObjectData")]
	private UniqueObjectData data = null;
	public UniqueObjectData Data => data;

	private static Dictionary<UniqueObjectData, UniqueObject> dataToObjectMap = new Dictionary<UniqueObjectData, UniqueObject>();

	private void Awake()
	{
		if (!dataToObjectMap.ContainsKey(data))
			ReparentSharedUniqueObject();
	}

    public UniqueObject GetSharedUniqueObject()
	{
		if (!dataToObjectMap.TryGetValue(data, out UniqueObject prevUO) || prevUO == null)
		{
			prevUO = uniqueObject;
			prevUO.SetData(data);
			
			dataToObjectMap[data] = prevUO;
		}

		return prevUO;
	}

	public void ReparentSharedUniqueObject()
	{
		UniqueObject prevUniqueObject = GetSharedUniqueObject();
		if(prevUniqueObject != null && prevUniqueObject != uniqueObject)
        {
			prevUniqueObject.SetData(null);
			uniqueObject.SetData(data);

			dataToObjectMap[data] = uniqueObject;
        }
	}
}
