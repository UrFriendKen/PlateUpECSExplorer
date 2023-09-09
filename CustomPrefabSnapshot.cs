using Kitchen;
using System.Collections.Generic;
using UnityEngine;

namespace KitchenECSExplorer
{
    public static class CustomPrefabSnapshot
    {
        private static Dictionary<string, Texture2D> Snapshots = new Dictionary<string, Texture2D>();

        private static float NightFade;

        private static readonly int Fade = Shader.PropertyToID("_NightFade");

        private static void CacheShaderValues()
        {
            NightFade = Shader.GetGlobalFloat(Fade);
            Shader.SetGlobalFloat(Fade, 0f);
        }

        private static void ResetShaderValues()
        {
            Shader.SetGlobalFloat(Fade, NightFade);
        }

        public static Texture2D GetSnapshot(GameObject prefab, float scale = 0.5f, int imageSize = 512)
        {
            Quaternion rotation = Quaternion.LookRotation(new Vector3(1f, -1f, 1f), new Vector3(0f, 1f, 1f));
            return GetSnapshot(prefab, -0.25f * new Vector3(0f, 1f, 1f), rotation, scale, imageSize);
        }

        private static Texture2D GetSnapshot(GameObject prefab, Vector3 position, Quaternion rotation, float scale = 1f, int imageSize = 512)
        {
            string key = $"{prefab.GetInstanceID()}_{scale}_{imageSize}";
            if (Snapshots.TryGetValue(key, out Texture2D snapshot))
                return snapshot;

            CacheShaderValues();
            SnapshotTexture snapshotTexture = Snapshot.RenderPrefabToTexture(imageSize, imageSize, prefab, rotation, 0.5f, 0.5f, -10f, 10f, scale, position);
            ResetShaderValues();

            Snapshots.Add(key, snapshotTexture.Snapshot);

            return snapshotTexture.Snapshot;
        }
    }
}
