using HarmonyLib;
using KitchenLib.DevUI;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace KitchenECSExplorer
{
    internal class GDOMenu : PlateUpExplorerMenu
    {
        private string componentFilterText = "";
        private static Vector2 vanillafilterScrollPosition = new Vector2(0, 0);
        private static Vector2 customsfilterScrollPosition = new Vector2(0, 0);

        private static Dictionary<string, ComponentType> Components = new Dictionary<string, ComponentType>();

        private static List<Type> VanillaGDOs = new List<Type>();
        private static List<Type> CustomGDOs = new List<Type>();


        private Type SelectedGDO = null;
        private bool IsSelectedVanilla = false;
        
        private static List<string> QueryAll = new List<string>();
        private static Vector2 queryAllScrollPosition = new Vector2(0, 0);
        private static List<string> QueryAny = new List<string>();
        private static Vector2 queryAnyScrollPosition = new Vector2(0, 0);
        private static List<string> QueryNone = new List<string>();
        private static Vector2 queryNoneScrollPosition = new Vector2(0, 0);

        //private static Vector2 resultsScrollPosition = new Vector2(0, 0);

        //private static List<EntityData> watchingEntities = new List<EntityData>();
        //private static List<ComponentType> watchingEntitiesSelectedComponent = new List<ComponentType>();
        //private static Vector2 watchingEntitiesScrollPosition = new Vector2(0, 0);
        //private static List<Vector2> watchingEntitiesComponentsScrollPosition = new List<Vector2>();
        //private static List<Vector2> watchingEntitiesComponentInfoScrollPosition = new List<Vector2>();

        public GDOMenu()
        {
            ButtonName = "GDOs";
        }

        public override void OnInitialise()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Get all non-abstract types in the assembly that inherit from KitchenData.GameDataObject
                VanillaGDOs.AddRange(assembly.GetTypes().Where(
                        type => type.IsSubclassOf(typeof(KitchenData.GameDataObject)) &&
                        !type.IsAbstract).ToList());

                // Get all non-abstract types in the assembly that inherit from KitchenLib.Customs.CustomGameDataObject
                CustomGDOs.AddRange(assembly.GetTypes().Where(
                    type => type.IsSubclassOf(
                    typeof(KitchenLib.Customs.CustomGameDataObject)) &&
                    !type.IsAbstract));
            }
            VanillaGDOs = VanillaGDOs.OrderBy(type => type.FullName).ToList();
            CustomGDOs = CustomGDOs.OrderBy(type => type.FullName).ToList();
            Main.LogInfo($"Number of Vanilla GDOs = {VanillaGDOs.Count}");
            Main.LogInfo($"Number of Custom GDOs = {CustomGDOs.Count}");
        }

        public override void OnSetup() // This is called evey frame the menu is open, This is also where you draw your UnityGUI
        {
            float windowWidth = 775f;
            float componentListWidth = windowWidth - 40f;
            float queryListWidth = windowWidth / 3f - 15f;

            #region All Components List
            GUILayout.BeginArea(new Rect(10f, 0f, windowWidth, 250f));
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 250f), Background, ScaleMode.StretchToFill);

            GUILayout.Label("Filter");

            componentFilterText = GUILayout.TextField(componentFilterText);
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(windowWidth * 0.4f));
            GUILayout.Label("Vanilla GDO Types", LabelCentreStyle);
            vanillafilterScrollPosition = GUILayout.BeginScrollView(vanillafilterScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

            for (int i = 0; i < VanillaGDOs.Count; i++)
            {
                string typeString = VanillaGDOs[i].FullName;
                if (string.IsNullOrEmpty(componentFilterText) || typeString.ToLower().Contains(componentFilterText.ToLower()))
                {
                    if (GUILayout.Button(typeString, ButtonLeftStyle, GUILayout.Width(windowWidth * 0.4f - 15f)))
                    {
                        SelectedGDO = VanillaGDOs[i];
                        IsSelectedVanilla = true;
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(windowWidth * 0.6f));
            GUILayout.Label("Custom GDO Types", LabelCentreStyle);
            customsfilterScrollPosition = GUILayout.BeginScrollView(customsfilterScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

            for (int i = 0; i < CustomGDOs.Count; i++)
            {
                string typeString = CustomGDOs[i].FullName;
                if (string.IsNullOrEmpty(componentFilterText) || typeString.ToLower().Contains(componentFilterText.ToLower()))
                {
                    if (GUILayout.Button(typeString, ButtonLeftStyle, GUILayout.Width(windowWidth * 0.6f - 15f)))
                    {
                        SelectedGDO = CustomGDOs[i];
                        IsSelectedVanilla = false;
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            #endregion

            if (SelectedGDO != null)
            {
                GUILayout.BeginArea(new Rect(10f, 260f, windowWidth, 700));
                GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 700f), Background, ScaleMode.StretchToFill);
                if (IsSelectedVanilla)
                {
                    DrawVanilla();
                }
                else
                {
                    DrawCustom();
                }
                GUILayout.EndArea();
            }
        }

        private void DrawVanilla()
        {

        }

        private void DrawCustom()
        {

        }
    }
}
