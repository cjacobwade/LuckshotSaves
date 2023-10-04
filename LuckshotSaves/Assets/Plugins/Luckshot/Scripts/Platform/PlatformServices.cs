using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

namespace Luckshot.Platform
{
	public enum PlatformID
	{
		None = 0,

		Steam = 1,
		DRMFree = 2,
		PS4 = 3,
		XboxOne = 4,
		Switch = 5
	}

	public class PlatformServices : Singleton<PlatformServices>
	{
		private Platform currentPlatform = null;
		public static Platform CurrentPlatform
		{ get { return Instance.currentPlatform; } }

		public static PlatformID GetPlatformID()
		{
			if (CurrentPlatform != null)
				return CurrentPlatform.GetPlatformID();

			return PlatformID.DRMFree;
		}

		public static bool IsConsolePlatform()
		{
			PlatformID platform = GetPlatformID();

			return platform == PlatformID.Switch ||
				platform == PlatformID.XboxOne ||
				platform == PlatformID.PS4;
		}

		static readonly int saveVersion = 0;

		public static string GetSaveName()
		{ return "/mygamesave_v" + saveVersion; }

		private static readonly string savePresetsFolder = Path.Combine(Application.streamingAssetsPath, "Saves");
		public static string GetSavePresetsFolder()
		{ return savePresetsFolder; }

		public static string GetSaveLocation(string savePresetName = null)
		{
			if (!string.IsNullOrEmpty(savePresetName))
				return $"{savePresetsFolder}/{savePresetName}.save";

			return Application.persistentDataPath + GetSaveName() + ".save";
		}

		private AchievementData[] achievementDatas = null;
		public AchievementData[] AchievementDatas => achievementDatas;

		private bool finishedSetup = false;
		public bool FinishedSetup => finishedSetup;

		public event Action<PlatformServices> OnFinishedSetup = delegate { };

		public Coroutine Initialize()
		{ return StartCoroutine(InitializeRoutine()); }

		private IEnumerator InitializeRoutine()
		{
			achievementDatas = Resources.LoadAll<AchievementData>("AchievementDatas/");

#if UNITY_PS4
			currentPlatform = gameObject.AddComponent<PSNPlatform>();
#elif UNITY_SWITCH
			currentPlatform = gameObject.AddComponent<SwitchPlatform>();
#elif UNITY_EDITOR || UNITY_STANDALONE
			//if (BigHopsPrefs.Instance.FakePlatformID == PlatformID.DRMFree)
				currentPlatform = gameObject.AddComponent<DRMFreePlatform>();
			//else
			//	currentPlatform = gameObject.AddComponent<SteamPlatform>();
#endif

			Debug.Log("Platform: " + currentPlatform.GetType());

			yield return currentPlatform.Initialize();

#if !UNITY_EDITOR && UNITY_STANDALONE
			BigHopsPrefs.Instance.LoadConfig();
#endif

			//if (BigHopsPrefs.Instance.AutoLoadOnStart)
			{
				if (!SaveManager.Instance.LoadFromDisk())
				{
					SaveManager.Instance.SaveToDisk();
					SaveManager.Instance.LoadFromDisk();
				}
			}

			finishedSetup = true;
			OnFinishedSetup(this);
		}

		public static void SetPlatform(Platform platform)
		{ Instance.currentPlatform = platform; }

		public void TryUnlockAchievements(Func<AchievementData, bool> checkComplete, Action unlockCallback = null)
		{
			for (int i = 0; i < achievementDatas.Length; i++)
			{
				var achievementData = achievementDatas[i];
				if (!IsAchievementUnlocked(achievementData))
				{
					if (checkComplete(achievementData))
						UnlockAchievement(achievementData, unlockCallback);
				}
			}
		}

		public bool IsAchievementUnlocked(AchievementData achievementData)
		{ return currentPlatform.IsAchievementUnlocked(achievementData); }

		public void UnlockAchievement(AchievementData achievementdata, Action unlockCallback = null)
		{ currentPlatform.UnlockAchievement(achievementdata, unlockCallback); }
	}

}