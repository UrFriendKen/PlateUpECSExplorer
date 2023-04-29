using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KitchenECSExplorer
{
    internal class AchievementMenu : PlateUpExplorerMenu
    {
        private const float windowWidth = 775f;
        private Vector2 scrollPosition;

        public AchievementMenu()
        {
            ButtonName = "Achievements";
        }

        protected override void OnSetup()
        {
            GUILayout.BeginArea(new Rect(10f, 0f, windowWidth, 1000f));
            GUI.DrawTexture(new Rect(0, 0, windowWidth, 1000f), Background);

            List<Achievement> achievements = GetAchievementsStatus().OrderBy(x => x.Name).ToList();
            GUILayout.Label($"Achievements ({achievements.Count})", LabelCentreStyle);
            float rowWidth = windowWidth - 15f;

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);
            for (int i = 0; i < achievements.Count; i++)
            {
                GUILayout.BeginHorizontal(GUILayout.Width(rowWidth));
                Achievement achievement = achievements[i];
                GUILayout.Label($"{achievement.Name} ({(achievement.UnlockTime == null? "Not unlocked" : achievement.UnlockTime)})", LabelLeftStyle);
                if (GUILayout.Button("Clear", GUILayout.Width(0.1f * rowWidth)) && achievement.UnlockTime != null)
                {
                    achievement.Clear();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        protected IEnumerable<Achievement> GetAchievementsStatus()
        {
            return SteamUserStats.Achievements;
        }
    }
}