#pragma warning disable 0414

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Luckshot.Platform
{
	public abstract class Platform : MonoBehaviour
	{
		public abstract PlatformID GetPlatformID();

		public virtual bool IsInitialized()
		{ return true; }

		public virtual Coroutine Initialize()
		{ return StartCoroutine(Initialize_Async()); }

		protected virtual IEnumerator Initialize_Async()
		{ yield break; }

		public virtual void SaveToDisk(string text, string savePresetName = null)
		{
			Debug.Log("Saving to " + PlatformServices.GetSaveLocation(savePresetName));
		}

		public virtual string LoadFromDisk(string savePresetName = null)
		{
			Debug.Log("Loading " + PlatformServices.GetSaveLocation(savePresetName));
			return string.Empty;
		}

		#region Achievements
		protected HashSet<AchievementData> unlockedAchievements = new HashSet<AchievementData>();

		public bool IsAchievementUnlocked(AchievementData achievementData)
		{ return unlockedAchievements.Contains(achievementData); }

		public void UnlockAchievement(AchievementData achievementData, Action unlockCallback)
		{
			if (!unlockedAchievements.Contains(achievementData))
			{
				StartCoroutine(UnlockAchievement_Async(achievementData, () =>
				{
					unlockedAchievements.Add(achievementData);

					if(unlockCallback != null)
						unlockCallback();
				}));
			}
		}

		protected virtual IEnumerator UnlockAchievement_Async(AchievementData achievementData, Action unlockAction)
		{ yield break; }
		#endregion

		#region Users
		public class User
		{
			public int controllerID;
			public string username;
			public Texture2D userIcon;
		}

		private List<User> users = new List<User>();

		public virtual string GetCurrentUserName()
		{ return "Player"; }

		public virtual string GetBuildID()
		{ return "UnknownPlatformBuildID"; }
		#endregion

		// LEADERBOARDS
	}
}