﻿namespace ExtendedBuildingWidth
{
    public class AnimSplittingSettings
    {
        public string ConfigName { get; init; }
        public string SymbolName { get; init; } = Dialog_EditAnimSlicingSettings.JsonValueEmpty;
        public string FrameIndex { get; init; } = Dialog_EditAnimSlicingSettings.JsonValueEmpty;
        public bool IsActive { get; init; }
        public int MiddlePart_X { get; init; }
        public int MiddlePart_Width { get; init; }
        public FillingStyle FillingMethod { get; init; }
        public bool DoFlipEverySecondIteration { get; init; }
    }
}