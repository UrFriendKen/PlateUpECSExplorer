using System.Collections.Generic;
using System.Linq;

namespace KitchenECSExplorer.Persistence
{
    internal class PersistentList<T> : PersistentItem where T : PersistentItem, new()
    {
        public List<T> Items { get; protected set; } = new List<T>();
        public virtual char Delimiter { get; protected set; } = '\n';

        public PersistentList() { }

        public PersistentList(char delimiter)
        {
            Delimiter = delimiter;
        }

        public PersistentList(IEnumerable<T> items)
        {
            Items = items.ToList();
        }

        public PersistentList(IEnumerable<T> items, char delimiter)
        {
            Items = items.ToList();
            Delimiter = delimiter;
        }

        public override string Serialize()
        {
            return string.Join(Delimiter.ToString(), Items?.Select(x => x.Serialize()) ?? new string[0]);
        }

        public override bool Deserialize(string data)
        {
            if (string.IsNullOrEmpty(data))
                return true;
            bool success = true;
            Items = data.Split(Delimiter).Select(itemData =>
            {
                T item = new T();
                if (!item.Deserialize(itemData))
                {
                    success = false;
                }
                return item;
            }).ToList();
            return success;
        }

        public static implicit operator List<T>(PersistentList<T> list) => list.Items;
        public static implicit operator PersistentList<T>(List<T> list) => new PersistentList<T>(list);
    }
}
