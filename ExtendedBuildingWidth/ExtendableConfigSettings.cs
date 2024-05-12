namespace ExtendedBuildingWidth
{
    public class ExtendableConfigSettings
    {
        public string ConfigName { get; set; }
        public int MinWidth { get; set; }
        public int MaxWidth { get; set; }
        public float AnimStretchModifier { get; set; }
    }

    public enum FillingMethod
    {
        Stretch,
        Repeat
    }

    public class AnimSplittingSettings
    {
        public string ConfigName { get; set; }
        public bool IsActive { get; set; }
        public int MiddlePart_X { get; set; }
        public int MiddlePart_Width { get; set; }
        public FillingMethod FillingMethod { get; set; }
        public bool DoFlipEverySecondIteration { get; set; }
    }

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