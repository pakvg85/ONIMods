using System;
using System.Collections.Generic;
using System.Linq;

namespace ExtendedBuildingWidth
{
    public static class DataMapper
    {
        public static ExtendableConfigSettings_Gui SourceToGui(ExtendableConfigSettings entry)
        {
            var result = new ExtendableConfigSettings_Gui()
            {
                ConfigName = entry.ConfigName,
                MinWidth = entry.MinWidth,
                MaxWidth = entry.MaxWidth,
                AnimStretchModifier = entry.AnimStretchModifier
            };
            return result;
        }

        public static ExtendableConfigSettings GuiToSource(ExtendableConfigSettings_Gui entry)
        {
            var result = new ExtendableConfigSettings()
            {
                ConfigName = entry.ConfigName,
                MinWidth = entry.MinWidth,
                MaxWidth = entry.MaxWidth,
                AnimStretchModifier = entry.AnimStretchModifier
            };
            return result;
        }

        public static AnimSplittingSettings_Gui SourceToGui(AnimSplittingSettings entry)
        {
            var result = new AnimSplittingSettings_Gui()
            {
                Guid = Guid.NewGuid(),
                ConfigName = entry.ConfigName,
                SymbolName = entry.SymbolName,
                FrameIndex = entry.FrameIndex,
                IsActive = entry.IsActive,
                MiddlePart_X = entry.MiddlePart_X,
                MiddlePart_Width = entry.MiddlePart_Width,
                FillingStyle = entry.FillingMethod,
                DoFlipEverySecondIteration = entry.DoFlipEverySecondIteration
            };
            return result;
        }

        public static AnimSplittingSettings GuiToSource(AnimSplittingSettings_Gui entry)
        {
            var result = new AnimSplittingSettings()
            {
                ConfigName = entry.ConfigName,
                SymbolName = entry.SymbolName,
                FrameIndex = entry.FrameIndex,
                IsActive = entry.IsActive,
                MiddlePart_X = entry.MiddlePart_X,
                MiddlePart_Width = entry.MiddlePart_Width,
                FillingMethod = entry.FillingStyle,
                DoFlipEverySecondIteration = entry.DoFlipEverySecondIteration
            };
            return result;
        }

        public static AnimSplittingSettings_Internal GuiToInternal(AnimSplittingSettings_Gui entry, KAnimHashedString symbolName, int frameIndex)
        {
            var newEntry = new AnimSplittingSettings_Internal
            {
                ConfigName = entry.ConfigName,
                SymbolName = symbolName,
                FrameIndex = frameIndex,
                MiddlePart_X = entry.MiddlePart_X,
                MiddlePart_Width = entry.MiddlePart_Width,
                FillingStyle = entry.FillingStyle,
                DoFlipEverySecondIteration = entry.DoFlipEverySecondIteration
            };
            return newEntry;
        }

        public static AnimSplittingSettings_Internal SourceToInternal(AnimSplittingSettings entry, string SymbolName, string FrameIndex)
        {
            if (string.IsNullOrEmpty(SymbolName)
                || string.IsNullOrEmpty(FrameIndex)
                )
            {
                throw new ArgumentNullException("SymbolName / FrameIndex");
            }
            //var symbolNameHash = !string.IsNullOrEmpty(SymbolName) ? new KAnimHashedString(SymbolName) : default;
            //var frameIndex = !string.IsNullOrEmpty(FrameIndex) ? int.Parse(FrameIndex) : -1;
            var symbolNameHash = new KAnimHashedString(SymbolName);
            var frameIndex = int.Parse(FrameIndex);

            var newEntry = new AnimSplittingSettings_Internal
            {
                ConfigName = entry.ConfigName,
                SymbolName = symbolNameHash,
                FrameIndex = frameIndex,
                MiddlePart_X = entry.MiddlePart_X,
                MiddlePart_Width = entry.MiddlePart_Width,
                FillingStyle = entry.FillingMethod,
                DoFlipEverySecondIteration = entry.DoFlipEverySecondIteration
            };
            return newEntry;
        }

        public static List<AnimSplittingSettings_Internal> SourceToInternal(List<AnimSplittingSettings> list)
        {
            var result = new List<AnimSplittingSettings_Internal>();
            foreach (var entry in list)
            {
                if (!entry.IsActive)
                {
                    continue;
                }

                try
                {
                    if (!string.IsNullOrEmpty(entry.SymbolName)
                        && !string.IsNullOrEmpty(entry.FrameIndex)
                        )
                    {
                        var newEntry = SourceToInternal(entry, entry.SymbolName, entry.FrameIndex);
                        result.Add(newEntry);
                    }
                    else if (!string.IsNullOrEmpty(entry.SymbolName)
                            && string.IsNullOrEmpty(entry.FrameIndex)
                            )
                    {
                        var config = DynamicBuildingsManager.ConfigMap[entry.ConfigName];
                        var originalDef = DynamicBuildingsManager.ConfigToBuildingDefMap[config];
                        var symbol = originalDef.AnimFiles.First().GetData().build.GetSymbol(entry.SymbolName);
                        for (var i = 0; i < symbol.numFrames; i++)
                        {
                            var newEntry = SourceToInternal(entry, symbol.hash.ToString(), i.ToString());
                            result.Add(newEntry);
                        }
                    }
                    else if (string.IsNullOrEmpty(entry.SymbolName))
                    {
                        var config = DynamicBuildingsManager.ConfigMap[entry.ConfigName];
                        var originalDef = DynamicBuildingsManager.ConfigToBuildingDefMap[config];
                        var symbols = originalDef.AnimFiles.First().GetData().build.symbols;
                        foreach (var symbol in symbols)
                        {
                            for (var i = 0; i < symbol.numFrames; i++)
                            {
                                var newEntry = SourceToInternal(entry, symbol.hash.ToString(), i.ToString());
                                result.Add(newEntry);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"ExtendedBuildingWidth - MapToInternal List at {entry.ConfigName} - [{entry.SymbolName}, {entry.FrameIndex}]");
                    Debug.LogWarning(e.ToString());
                    throw e;
                }
            }
            return result;
        }
    }
}