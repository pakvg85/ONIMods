using System;

namespace ExtendedBuildingWidth
{
    public class AnimSplittingSettings_Gui
    {
        public Guid Guid { get; init; }
        public string ConfigName { get; init; }
        public string SymbolName { get; set; }
        public string FrameIndex { get; set; }
        public bool IsActive { get; set; }
        public int MiddlePart_X { get; set; }
        public int MiddlePart_Width { get; set; }
        public FillingStyle FillingStyle { get; set; }
        public bool DoFlipEverySecondIteration { get; set; }
    }
}