using KitchenLib.DevUI;
using UnityEngine;

namespace KitchenECSExplorer
{
    public class PlateUpExplorerMenu : BaseUI
    {
        protected GUIStyle LabelLeftStyle;
        protected GUIStyle LabelCentreStyle;
        protected GUIStyle LabelMiddleCentreStyle;

        protected Texture2D Background;
        public sealed override void OnInit()
        {
            Background = new Texture2D(64, 64);
            Color grayWithAlpha = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    Background.SetPixel(x, y, grayWithAlpha);
                }
            }
            Background.Apply();
            OnInitialise();
        }

        public sealed override void Setup()
        {
            if (LabelLeftStyle == null)
            {
                LabelLeftStyle = new GUIStyle(GUI.skin.label);
                LabelLeftStyle.alignment = TextAnchor.MiddleLeft;
                LabelLeftStyle.padding.left = 10;
                LabelLeftStyle.stretchWidth = true;
            }


            if (LabelCentreStyle == null)
            {
                LabelCentreStyle = new GUIStyle(GUI.skin.label);
                LabelCentreStyle.alignment = TextAnchor.MiddleCenter;
                LabelCentreStyle.stretchWidth = true;
            }

            if (LabelMiddleCentreStyle == null)
            {
                LabelMiddleCentreStyle = new GUIStyle(GUI.skin.label);
                LabelMiddleCentreStyle.alignment = TextAnchor.MiddleCenter;
                LabelMiddleCentreStyle.stretchWidth = true;
                LabelMiddleCentreStyle.stretchHeight = true;
            }
            OnSetup();
        }

        public virtual void OnInitialise()
        {
        }

        public virtual void OnSetup()
        {

        }
    }
}
