using Kitchen;
using UnityEngine;

namespace KitchenECSExplorer
{
    internal class ViewsMenu : PlateUpExplorerMenu
    {
        private Vector2 scrollPosition = new Vector2(0, 0);

        static ObjectData Data;

        private const float windowWidth = 775f;

        public ViewsMenu()
        {
            ButtonName = "Views";
        }

        protected override void OnInitialise()
        {
            if (Data == null)
                Data = new ObjectData("Views", null);
        }

        public static void Populate(AssetDirectory dir)
        {
            Data = new ObjectData("Views", dir.ViewPrefabs);
            Data.IsExpanded = true;
        }

        protected override void OnSetup() // This is called evey frame the menu is open, This is also where you draw your UnityGUI
        {
            GUILayout.BeginArea(new Rect(10f, 0f, windowWidth, 1000f));
            GUI.DrawTexture(new Rect(0, 0, windowWidth, 1000f), Background);
            DrawObjectHierarchy(Data, ref scrollPosition);
            GUILayout.EndArea();
        }
    }
}
