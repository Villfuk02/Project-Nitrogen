using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Run.Shared
{
    public static class PersistentData
    {
        static readonly string FinishedTutorialKey = "finished_tutorial";

        public static bool FinishedTutorial
        {
            get => PlayerPrefs.GetInt(FinishedTutorialKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(FinishedTutorialKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        static readonly string KnownAttackersKey = "known_attackers";
        static readonly HashSet<string> KnownAttackers = new(PlayerPrefs.GetString(KnownAttackersKey, "").Split(';', StringSplitOptions.RemoveEmptyEntries));
        public static bool IsAttackerKnown(string name) => KnownAttackers.Contains(name);

        public static void RegisterKnownAttacker(string name)
        {
            if (KnownAttackers.Add(name))
            {
                PlayerPrefs.SetString(KnownAttackersKey, string.Join(';', KnownAttackers));
                PlayerPrefs.Save();
            }
        }
    }
}