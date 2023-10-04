using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonPropertyItem<T> : PropertyItem where T : MonoBehaviour
{
	// Not ideal to duplicate code from Singleton.cs
	// but single inheritance be like that sometimes

	private static T instance = null;
	public static T Instance
	{
		get
		{
			if (instance == null)
				instance = FindObjectOfType<T>();

			return instance;
		}
	}

	protected override void AwakeIfNeeded()
	{
		if (instance == null || instance == this)
		{
			instance = this as T;
			base.AwakeIfNeeded();
		}
		else if (instance != this)
		{
			Debug.LogWarning(string.Format("Multiple instances of {0}. Destroying new instance.", typeof(T)));
			Destroy(gameObject);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		instance = null;
	}
}
