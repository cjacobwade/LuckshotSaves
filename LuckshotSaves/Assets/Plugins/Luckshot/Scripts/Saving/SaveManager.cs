
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Luckshot.Platform;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SaveManager : SingletonPropertyItem<SaveManager>
{
	// Save versions
	// v0 - 6/23/2023, proper nested class support
	// v2 - 7/27/2023, loadsavescene true by default forces new ini file

	private const int saveVersion = 0;

	[SaveLoad]
	private int SaveVersion
	{
		get { return saveVersion; }
		set 
		{
			if (value < saveVersion)
			{
				Debug.LogError("Loading old save file. You may lose some data.");
			}
			else if(value > saveVersion)
			{
				Debug.LogError("Build version is older than this save. You may lose some data.");
			}

			// TODO: Handle migrating saves?
		}
	}

	private List<PersistentItem> persistentItems = new List<PersistentItem>();
	public List<PersistentItem> PersistentItems => persistentItems;

	private WorldState worldState = null;

	public event Action<PersistentItem> OnItemRegistered = delegate { };
	public event Action<PersistentItem> OnItemDeregistered = delegate { };

	public event Action OnWillSave = delegate {};
	public event Action OnSaveFinished = delegate {};

	public event Action OnWillLoad = delegate {};
	public event Action OnLoadFinished = delegate {};

	public void RegisterItem(PersistentItem persistentItem)
	{
		persistentItems.Add(persistentItem);

		if(worldState != null)
		{
			for (int i = 0; i < worldState.itemStates.Count; i++)
			{
				ItemState itemState = worldState.itemStates[i];
				if (persistentItem.UniqueObject.Data.name == itemState.uniqueName)
				{
					try
					{
						itemState.ApplyStateToItem(persistentItem.Item);
					}
					catch (Exception e)
					{
						Debug.LogErrorFormat($"Exception applying {persistentItem.Item.name} state: {e}", persistentItem);
					}
				}
			}
		}

		OnItemRegistered(persistentItem);
	}

	public void DeregisterItem(PersistentItem persistentItem)
	{ 
		persistentItems.Remove(persistentItem);
		OnItemDeregistered(persistentItem);
	}

	public void CollectWorldState()
	{
		// TODO: Rather than restarting from scratch maybe can update things based on dirty state?

		worldState = new WorldState();

		for (int i = 0; i < persistentItems.Count; i++)
		{
			var persistentItem = persistentItems[i];

			try
			{
				var itemState = persistentItem.Item.BuildItemState();
				worldState.itemStates.Add(itemState);
			}
			catch(Exception e)
			{
				Debug.LogErrorFormat($"Exception building {persistentItem.Item.name} state: {e}", persistentItem);
			}
		}
	}

	public void ClearSave()
    {
		OnWillSave();

		worldState = new WorldState();
		SaveToDisk();

		OnSaveFinished();
	}
    
    public string SaveToString()
    {
        if (worldState == null)
            worldState = new WorldState();

        return JsonUtility.ToJson(worldState);
    }

	[ContextMenu("Save World State")]
	public void SaveToDisk(string savePresetName = null)
	{
		string saveOutput = SaveToString();
		PlatformServices.CurrentPlatform.SaveToDisk(saveOutput, savePresetName);		
	}
	
	public void CreateSavePreset(string presetName)
	{
		#if UNITY_EDITOR
		string path = PlatformServices.GetSaveLocation(presetName);
		if (File.Exists(path))
		{
			if (!EditorUtility.DisplayDialog("Overwrite Save-Preset?", $"There is already a save-preset called \"{presetName}\" - do you want to overwrite it?", "Yes", "No"))
			{
				return;
			}
		}
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		SaveWorldState(presetName);
		AssetDatabase.Refresh();
		EditorUtility.DisplayDialog("Created Save-Preset", $"Your preset has been saved to {path}", "OK");
		#endif
	}

	[ContextMenu("Load World State")]
	public bool LoadFromDisk(string savePresetName = null)
	{
		OnWillLoad();

		string loadText = PlatformServices.CurrentPlatform.LoadFromDisk(savePresetName);
		if (string.IsNullOrEmpty(loadText))
			return false;

		worldState = JsonUtility.FromJson<WorldState>(loadText);

		for(int i = 0; i < worldState.itemStates.Count; i++)
		{
			ItemState itemState = worldState.itemStates[i];
			if (itemState != null && !string.IsNullOrEmpty(itemState.uniqueName))
			{
				for (int j = 0; j < persistentItems.Count; j++)
				{
					var persistentItem = persistentItems[j];
					if (persistentItem.UniqueObject.Data.name == itemState.uniqueName)
					{
						try
						{
							itemState.ApplyStateToItem(persistentItem.Item);
						}
						catch(Exception e)
						{
							Debug.LogErrorFormat($"Exception applying {persistentItem.Item.name} state: {e}", persistentItem);
						}
						
						break;
					}
				}
			}
		}

		OnLoadFinished();
        
		return true;
	}

	public void SaveWorldState(string savePresetName = null)
	{
		OnWillSave();

		CollectWorldState();
		SaveToDisk(savePresetName);

		OnSaveFinished();
	}

	private void OnApplicationQuit()
	{
		Debug.Log("OnApplicationQuit (SaveManager)");

		//if (BigHopsPrefs.Instance.AutoSaveOnQuit) 
			SaveWorldState();
	}
}
