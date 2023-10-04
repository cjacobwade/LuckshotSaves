using Luckshot.Platform;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	private void Awake()
	{
		PlatformServices.Instance.Initialize();
	}
}
