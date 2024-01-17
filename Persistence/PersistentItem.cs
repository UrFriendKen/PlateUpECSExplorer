using System.IO;
using UnityEngine;

namespace KitchenECSExplorer.Persistence
{
    internal abstract class PersistentItem
    {
        public bool Load(string filename)
        {
            string filepath = GetFilepath(filename);
            if (filepath == null || !File.Exists(filepath))
                return false;
            return Deserialize(File.ReadAllText(filepath));
        }

        public void Save(string filename)
        {
            string filepath = GetFilepath(filename);
            if (filepath == null)
                return;

            string data = Serialize();
            try
            {
                File.WriteAllText(filepath, data);
            }
            catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(GetFolderPath());
                File.WriteAllText(filepath, data);
            }
            
        }

        private string GetFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, Main.SAVE_FOLDER);
        }

        private string GetFilepath(string filename)
        {
            return string.IsNullOrEmpty(filename) ? null : Path.Combine(GetFolderPath(), filename);
        }

        public abstract string Serialize();
        public abstract bool Deserialize(string data);
    }
}
