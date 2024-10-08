﻿using KitchenData;
using UnityEngine;

namespace KitchenECSExplorer
{
    internal class GameDataMenu : PlateUpExplorerMenu
    {
        private Vector2 scrollPosition = new Vector2(0, 0);

        ObjectData Data;

        private const float windowWidth = 775f;

        public GameDataMenu()
        {
            ButtonName = "GameData";
        }

        protected override void OnInitialise()
        {
            Data = new ObjectData("GameData", GameData.Main);
        }

        protected override void OnSetup() // This is called evey frame the menu is open, This is also where you draw your UnityGUI
        {
            // Temp while OnInit does not work
            if (Data == null)
            {
                OnInitialise();
            }

            GUILayout.BeginArea(new Rect(10f, 0f, windowWidth, 1000f));
            GUI.DrawTexture(new Rect(0, 0, windowWidth, 1000f), Background);
            DrawObjectHierarchy(Data, ref scrollPosition);
            GUILayout.EndArea();
        }
    }
}
