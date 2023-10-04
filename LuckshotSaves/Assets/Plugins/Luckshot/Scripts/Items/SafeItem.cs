using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeItem : PropertyItem
{
	// Items deriving from this won't be cleaned up by systems

	public static bool IsSafeItem(Item item)
	{
		if (item.GetProperty<SafeItem>() != null)
			return true;

		return false;
	}
}
