namespace ExtendedBuildingWidth
{
    public class AnimSplittingSettings
    {
        public string ConfigName { get; set; }
        public string SymbolName { get; set; }
        public string FrameIndex { get; set; }
        public bool IsActive { get; set; }
        public int MiddlePart_X { get; set; }
        public int MiddlePart_Width { get; set; }
        public FillingStyle FillingMethod { get; set; }
        public bool DoFlipEverySecondIteration { get; set; }
    }
}