using UnityEngine;
using System.Collections;
using System;
using System.IO;

namespace Luckshot.Platform
{
	public class DRMFreePlatform : Platform
	{
		public override PlatformID GetPlatformID()
		{
			PlatformID platformID = PlatformID.DRMFree;

			/*
			if (BigHopsPrefs.Instance.BuildMode != BuildMode.Release &&
				BigHopsPrefs.Instance.FakePlatformID != PlatformID.None)
			{
				platformID = BigHopsPrefs.Instance.FakePlatformID;
			}
			*/

			return platformID;
		}

		protected override IEnumerator Initialize_Async()
		{
			// TODO: Hook up in-game achievement shower
			yield break;
		}

		public override void SaveToDisk(string saveText, string savePresetName)
		{
			base.SaveToDisk(saveText, savePresetName);

			string saveLocation = PlatformServices.GetSaveLocation(savePresetName);
			if (File.Exists(saveLocation))
				File.Delete(saveLocation);

			File.WriteAllText(saveLocation, saveText);
		}

		public override string LoadFromDisk(string savePresetName)
		{
			base.LoadFromDisk(savePresetName);

			string saveLocation = PlatformServices.GetSaveLocation(savePresetName);
			if (!File.Exists(saveLocation))
				return null;

			string loadText = File.ReadAllText(saveLocation);
			return loadText;
		}
	}
}