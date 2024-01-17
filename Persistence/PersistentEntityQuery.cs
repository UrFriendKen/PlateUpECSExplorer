using System.Collections.Generic;
using System.Linq;

namespace KitchenECSExplorer.Persistence
{
    internal class PersistentEntityQuery : PersistentItem
    {
        public string Name { get; set; } = "Entity Query";
        public PersistentList<PersistentString> All { get; protected set; }
        public PersistentList<PersistentString> Any { get; protected set; }
        public PersistentList<PersistentString> None { get; protected set; }

        public PersistentEntityQuery()
        {
            InitLists(null, null, null);
        }

        public PersistentEntityQuery(string name, List<string> all, List<string> any, List<string> none)
        {
            Name = name ?? string.Empty;
            InitLists(all, any, none);
        }

        void InitLists(List<string> all, List<string> any, List<string> none)
        {
            All = all == null ? new PersistentList<PersistentString>(ComponentDelimiter) : new PersistentList<PersistentString>(all.Select(text => new PersistentString(text)), ComponentDelimiter);
            Any = any == null ? new PersistentList<PersistentString>(ComponentDelimiter) : new PersistentList<PersistentString>(any.Select(text => new PersistentString(text)), ComponentDelimiter);
            None = none == null ? new PersistentList<PersistentString>(ComponentDelimiter) : new PersistentList<PersistentString>(none.Select(text => new PersistentString(text)), ComponentDelimiter);
        }

        protected virtual char Delimiter => '$';
        protected virtual char ComponentDelimiter => '|';


        public override string Serialize()
        {
            return string.Join(Delimiter.ToString(), new string[]
            {
                Name,
                All.Serialize(),
                Any.Serialize(),
                None.Serialize()
            });
        }

        public override bool Deserialize(string data)
        {
            bool success = true;
            string[] splitData = data.Split(Delimiter);
            if (splitData.Length == 4)
            {
                Name = splitData[0];
                success &= All.Deserialize(splitData[1]) & Any.Deserialize(splitData[2]) & None.Deserialize(splitData[3]);
            }

            if (!success)
            {
                Main.LogError("Failed to deserialize persistent EntityQuery due to malformed data.");
                return false;
            }
            return true;
        }
    }
}
