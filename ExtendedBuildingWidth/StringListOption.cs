namespace ExtendedBuildingWidth
{
    public class StringListOption : IListableOption
    {
        string IListableOption.GetProperName()
        {
            return name;
        }

        public static implicit operator StringListOption(LocString name) => new StringListOption(name);

        public static bool operator ==(StringListOption one, StringListOption two) => one.Equals(two);

        public static bool operator !=(StringListOption one, StringListOption two) => !one.Equals(two);

        private readonly string name;

        public StringListOption(string optionName)
        {
            name = optionName;
        }

        public override bool Equals(object obj)
        {
            return obj is StringListOption other && other.name == name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override string ToString()
        {
            return name;
        }
    }
}