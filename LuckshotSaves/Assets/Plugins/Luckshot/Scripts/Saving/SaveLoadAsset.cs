using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Class)]
public class SaveLoadAssetAttribute : Attribute
{
	public string resourcePath = string.Empty;

	public SaveLoadAssetAttribute(string resourcePath = "")
	{
		this.resourcePath = resourcePath;
	}
}
