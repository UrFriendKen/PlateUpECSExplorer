namespace KitchenECSExplorer.Persistence
{
    internal class PersistentString : PersistentItem
    {
        public string Text { get; protected set; } = null;

        public PersistentString() { }

        public PersistentString(string text)
        {
            Text = text;
        }

        public override bool Deserialize(string data)
        {
            Text = data;
            return true;
        }

        public override string Serialize()
        {
            return Text;
        }

        public static implicit operator string(PersistentString s) => s.Text;
        public static implicit operator PersistentString(string s) => new PersistentString(s);
    }
}
