using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class UniqueObjectManager : Singleton<UniqueObjectManager>
{
	[SerializeField]
	private Transform managersRoot = null;

	[Serializable]
	private class UniquePair
	{
		public string name = string.Empty;

		public UniqueObject uniqueObject = null;
		public UniqueObjectData data = null;

		public UniquePair(UniqueObject uo, UniqueObjectData uod)
		{
			uniqueObject = uo;
			data = uod;
			name = data.name;
		}
	}

	[Serializable]
	public class SceneUODMap
	{
		public string name = string.Empty;

		public Dictionary<UniqueObjectData, UniqueObject> dataToObjectMap = new Dictionary<UniqueObjectData, UniqueObject>();
		public Dictionary<string, UniqueObject> nameToObjectMap = new Dictionary<string, UniqueObject>(StringComparer.OrdinalIgnoreCase);

#if UNITY_EDITOR
		[SerializeField]
		private List<UniquePair> uniquePairs = new List<UniquePair>();
		private Dictionary<UniqueObjectData, UniquePair> dataToPairMap = new Dictionary<UniqueObjectData, UniquePair>();
#endif

		public void RegisterUniqueObject(UniqueObject uniqueObject)
		{
			if (dataToObjectMap.TryGetValue(uniqueObject.Data, out UniqueObject existingObj))
			{
				if (existingObj != null)
				{
					Debug.LogWarningFormat(existingObj, "UniqueObjectData {0} already registered. This is not allowed.", uniqueObject.Data);
					return;
				}
			}

			dataToObjectMap[uniqueObject.Data] = uniqueObject;
			nameToObjectMap[uniqueObject.Data.name] = uniqueObject;

#if UNITY_EDITOR
			if (dataToPairMap.TryGetValue(uniqueObject.Data, out UniquePair uniquePair))
			{
				dataToPairMap[uniqueObject.Data].uniqueObject = uniqueObject;
			}
			else
			{
				uniquePair = new UniquePair(uniqueObject, uniqueObject.Data);
				uniquePairs.Add(uniquePair);

				dataToPairMap[uniqueObject.Data] = uniquePair;
			}
#endif
		}

		public bool UnregisterUniqueObject(UniqueObject uniqueObject)
		{
#if UNITY_EDITOR
			if (dataToPairMap.TryGetValue(uniqueObject.Data, out UniquePair pair))
			{
				uniquePairs.Remove(pair);
				dataToPairMap.Remove(uniqueObject.Data);
			}
#endif

			if (dataToObjectMap.TryGetValue(uniqueObject.Data, out UniqueObject storedUO) &&
				uniqueObject == storedUO)
			{
				dataToObjectMap.Remove(uniqueObject.Data);
				nameToObjectMap.Remove(uniqueObject.Data.name);
				return true;
			}

			return false;
		}

		public T LookupUniqueObject<T>(string name) where T : Component
		{
			TryLookupUniqueObject<T>(name, out T t);
			return t;
		}

		public T LookupUniqueObject<T>(UniqueObjectData data) where T : Component
		{ return LookupUniqueObject<T>(data.name); }

		public bool TryLookupUniqueObject<T>(UniqueObjectData data, out T t) where T : Component
		{ return TryLookupUniqueObject<T>(data.name, out t); }

		public bool TryLookupUniqueObject<T>(string name, out T t) where T : Component
		{
			if (!string.IsNullOrEmpty(name) && nameToObjectMap.TryGetValue(name, out UniqueObject uniqueObject) &&
				uniqueObject != null && uniqueObject.TryGetComponent(out t))
			{
				return true;
			}

			t = null;
			return false;
		}
	}

	private Dictionary<Scene, SceneUODMap> sceneToUODMaps = new Dictionary<Scene, SceneUODMap>();

#if UNITY_EDITOR
	[SerializeField]
	private List<SceneUODMap> sceneUODMaps = new List<SceneUODMap>();
#endif

	public static bool IsActive()
	{ return Instance != null && Instance.sceneToUODMaps.Count > 0; }

	protected override void Awake()
	{
		base.Awake();

		if (UniqueObjectManager.Instance != this)
		{
			// Will be destroyed elsewhere
		}
		else
		{
			UniqueObject[] uniqueObjects = managersRoot.GetComponentsInChildren<UniqueObject>(true);
			for (int i = 0; i < uniqueObjects.Length; i++)
				uniqueObjects[i].RegisterIfNeeded();
		}
	}


	public void RegisterUniqueObject(UniqueObject uniqueObject)
	{
		if (!sceneToUODMaps.TryGetValue(uniqueObject.gameObject.scene, out SceneUODMap sceneUODMap))
		{
			sceneUODMap = new SceneUODMap();
			sceneUODMap.name = uniqueObject.gameObject.scene.name;

			sceneToUODMaps[uniqueObject.gameObject.scene] = sceneUODMap;
			#if UNITY_EDITOR
				sceneUODMaps.Add(sceneUODMap);
			#endif
		}

		sceneUODMap.RegisterUniqueObject(uniqueObject);
	}

	public void UnregisterUniqueObject(UniqueObject uniqueObject)
	{
		if (uniqueObject == null)
			return;

		if (sceneToUODMaps.TryGetValue(uniqueObject.gameObject.scene, out SceneUODMap sceneUODMap))
			sceneUODMap.UnregisterUniqueObject(uniqueObject);
	}

	public void ForceRegisterUniqueObjectsInScene(Scene scene)
	{
		GameObject[] rootGOs = scene.GetRootGameObjects();
		for (int i = 0; i < rootGOs.Length; i++)
		{
			UniqueObject[] uniqueObjects = rootGOs[i].GetComponentsInChildren<UniqueObject>(true);
			for (int j = 0; j < uniqueObjects.Length; j++)
			{
				UniqueObject uo = uniqueObjects[j];

				//if (!DestroyManager.IsMarkedForDestroy(uo))
					uo.RegisterIfNeeded();
			}
		}
	}

	public UniqueObject LookupUniqueObject(UnityEngine.Object uObject)
	{
		UniqueObjectData uod = uObject as UniqueObjectData;
		if (uod != null)
			return LookupUniqueObject(uod);

		return null;
	}

	public UniqueObject LookupUniqueObject(UniqueObjectData data, Scene scene = default)
	{ return LookupUniqueObject<UniqueObject>(data, scene); }

	public T LookupUniqueObject<T>(UniqueObjectData data, Scene scene = default) where T : Component
	{ return LookupUniqueObject<T>(data.name, scene); }

	public UniqueObject LookupUniqueObject(string name, Scene scene = default)
	{ return LookupUniqueObject<UniqueObject>(name, scene); }

	public T LookupUniqueObject<T>(string name, Scene scene = default) where T : Component
	{
		TryLookupUniqueObject(name, out T t, scene);
		return t;
	}

	public bool TryLookupUniqueObject<T>(UniqueObjectData data, out T t, Scene scene = default) where T : Component
	{ return TryLookupUniqueObject<T>(data.name, out t, scene); }

	public bool TryLookupUniqueObject<T>(string name, out T t, Scene scene = default) where T : Component
	{
		if (scene == default)
			scene = SceneManager.GetActiveScene();

		if (scene.IsValid() && scene.isLoaded)
		{
			if (sceneToUODMaps.TryGetValue(scene, out SceneUODMap sceneUODMap) &&
				sceneUODMap.TryLookupUniqueObject(name, out t))
			{
				return true;
			}
		}

		foreach (var kvp in sceneToUODMaps)
		{
			if (kvp.Value.TryLookupUniqueObject(name, out t))
				return true;
		}

		Debug.LogWarningFormat("UniqueObjectLookup for data {0} failed.", name);
		t = null;
		return false;
	}
}
