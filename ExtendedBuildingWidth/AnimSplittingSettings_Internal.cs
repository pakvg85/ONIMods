namespace ExtendedBuildingWidth
{
    public class AnimSplittingSettings_Internal
    {
        public string ConfigName { get; init; }
        public KAnimHashedString SymbolName { get; init; }
        public int FrameIndex { get; init; }
        public bool IsActive { get; init; }
        public int MiddlePart_X { get; init; }
        public int MiddlePart_Width { get; init; }
        public FillingStyle FillingStyle { get; init; }
        public bool DoFlipEverySecondIteration { get; init; }
    }
}