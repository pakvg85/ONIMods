using HarmonyLib;
using PeterHan.PLib.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtendedBuildingWidth
{
    public class DynamicAnimManager
    {
        public static void AddDynamicAnimsNames_To_ModLoadedKAnims()
        {
            var dummyModSettings = POptions.ReadSettings<ModSettings>() ?? new ModSettings();
            var configsToBeExtended = dummyModSettings.GetExtendableConfigSettingsList();
            var configNameToAnimNamesMap = dummyModSettings.GetConfigNameToAnimNamesMap();

            var animTable = Traverse.Create<Assets>().Field("AnimTable").GetValue() as Dictionary<HashedString, KAnimFile>;

            var kAnimFilesAdded = new List<KAnimFile>();

            foreach (var item in configsToBeExtended)
            {
                try
                {
                    if (!configNameToAnimNamesMap.ContainsKey(item.ConfigName))
                    {
                        continue;
                    }

                    string origAnimName = configNameToAnimNamesMap[item.ConfigName];

                    KAnimFile origAnimFile = null;

                    var origAnimNameHashedString = new KAnimHashedString(origAnimName);

                    if (!animTable.TryGetValue(origAnimNameHashedString, out origAnimFile))
                    {
                        Debug.Log($"ExtendedBuildingWidth WARNING - anim {origAnimName} for config {item.ConfigName} not loaded");
                        continue;
                    }

                    bool isMod = origAnimFile.mod != null;

                    for (var width = item.MinWidth; width <= item.MaxWidth; width++)
                    {
                        if (width == 3)
                        {
                            continue;
                        }

                        var dynamicAnimName = DynamicBuildingsManager.GetDynamicName(origAnimName, width);

                        KAnimFile kAnimFile = null;
                        if (!isMod)
                        {
                            var animFile = Traverse.Create(origAnimFile).Field("animFile").GetValue() as TextAsset;
                            var buildFile = Traverse.Create(origAnimFile).Field("buildFile").GetValue() as TextAsset;
                            var textures = Traverse.Create(origAnimFile).Field("textures").GetValue() as List<Texture2D>;

                            kAnimFile = ModUtil.AddKAnim(dynamicAnimName, animFile, buildFile, textures);
                        }
                        else
                        {
                            var animMod = origAnimFile.mod;

                            kAnimFile = ModUtil.AddKAnimMod(dynamicAnimName, animMod);
                        }

                        if (kAnimFile == null)
                        {
                            throw new Exception($"kAnimFile == null for {item.ConfigName}");
                        }

                        kAnimFilesAdded.Add(kAnimFile);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log($"ExtendedBuildingWidth WARNING - anims for config {item.ConfigName} were not loaded");
                    Debug.Log(e.ToString());
                }
            }

            Assets.Anims.AddRange(kAnimFilesAdded);
            foreach (var kanimFile2 in kAnimFilesAdded)
            {
                if (kanimFile2 != null)
                {
                    HashedString key2 = kanimFile2.name;
                    animTable[key2] = kanimFile2;
                }
            }
        }

        public static void OverwriteAnimFiles(BuildingDef dynamicDef, string dynamicAnimName)
        {
            var hashedString = new KAnimHashedString(dynamicAnimName);
            var dynamicAnim = Assets.GetAnim(hashedString);
            if (dynamicAnim == null)
            {
                throw new Exception($"ExtendedBuildingWidth OverwriteAnimFiles: dynamicAnim == null for {dynamicAnimName}");
            }

            dynamicDef.AnimFiles = new KAnimFile[] { dynamicAnim };
        }

        public static void SplitAnim(BuildingDef dynamicDef, int widthInCellsDelta, int middle_X, int middle_Width, FillingMethod extStyle, bool doFlipEverySecondIteration)
        {
            var animFile = dynamicDef.AnimFiles.FirstOrDefault();

            var animData = animFile.GetData();

            var texture = animFile.textureList.FirstOrDefault();

            var BGD = KAnimBatchManager.Instance().GetBatchGroupData(animData.build.batchTag);
            var symbolFrameInstances = BGD.symbolFrameInstances;

            //Debug.Log($"ExtendedBuildingWidth - frameList.Count = {symbolFrameInstances.Count()}");
            //Debug.Log($"ExtendedBuildingWidth - animData.maxVisSymbolFrames = {animData.maxVisSymbolFrames}, BGD.maxVisibleSymbols = {BGD.maxVisibleSymbols}");
            //Debug.Log($"ExtendedBuildingWidth - animFile.name = {animFile.name}, dynamicDef.AnimFiles.Count = {dynamicDef.AnimFiles.Count()}");

            //foreach (var entry in symbolFrameInstances)
            //{
            //    Debug.Log($"ListEntry before: {entry.sourceFrameNum}; {entry.duration}; {entry.buildImageIdx}; {entry.symbolIdx}; {entry.uvMax.x}");
            //}

            //Debug.Log($"ExtendedBuildingWidth - build name = {animData.build.name}, build.batchTag = {animData.build.batchTag}");
            //Debug.Log($"ExtendedBuildingWidth - symbols count = {animData.build.symbols.Count()}");

            var oldFrameToSymbolMap = new Dictionary<int, KAnimHashedString>();
            var symbolToNewFramesMap = new Dictionary<KAnimHashedString, List<KAnim.Build.SymbolFrameInstance>>();

            const int cellWidthPx = 100;
            var delta_Width = widthInCellsDelta * cellWidthPx;

            //Debug.Log($"Symbols before:");
            //for (var i = 0; i < animData.build.symbols.Count(); i++)
            //{
            //    var entry = animData.build.symbols[i];
            //    Debug.Log($"{entry.hash}; {entry.firstFrameIdx}; {entry.numFrames}; {entry.numLookupFrames}; {entry.symbolIndexInSourceBuild}");
            //}

            for (uint idxBuildSymbol = 0; idxBuildSymbol < animData.build.symbols.Count(); idxBuildSymbol++)
            {
                var symbol = animData.build.GetSymbolByIndex(idxBuildSymbol);

                if (symbol.hash.ToString() == "ui")
                {
                    continue;
                }

                //Debug.Log($"ExtendedBuildingWidth - symbol {idxBuildSymbol}: firstFrameIdx = {symbol.firstFrameIdx}, symbol.numLookupFrames = {symbol.numLookupFrames}");

                int framesAddedForSymbol = 0;
                for (int j = 0; j < symbol.numFrames; j++)
                {
                    var frameIndex = symbol.GetFrameIdx(j);
                    var origFrame = symbol.GetFrame(j);

                    //Debug.Log($"ExtendedBuildingWidth - origFrame index = {frameIndex}");

                    float textureWidth = texture.width;

                    float orig_X = origFrame.uvMin.x * textureWidth;
                    float orig_Width = (origFrame.uvMax.x - origFrame.uvMin.x) * textureWidth;
                    float final_X = orig_X;
                    float final_Width = orig_Width + delta_Width;

                    float firstFrame_Width = middle_X;
                    float lastFrame_X = middle_X + middle_Width;
                    float lastFrame_Width = orig_Width - lastFrame_X + 1;
                    float lastFrame_OutputX = orig_X + lastFrame_X + (delta_Width - 1) + 1;

                    //Debug.Log($"ExtendedBuildingWidth - origFrame UV = {origFrame.uvMin.x}, {origFrame.uvMax.x}");
                    //Debug.Log($"ExtendedBuildingWidth - origFrame bbox = {origFrame.bboxMin.x}, {origFrame.bboxMax.x}");
                    //Debug.Log($"ExtendedBuildingWidth - origFrame in pixels = {orig_X}, {orig_Width}");

                    KAnim.Build.SymbolFrameInstance newFrame;
                    float newFrame_X = float.NaN;
                    float screenOutput_X = float.NaN;
                    float screenOutput_Width = float.NaN;
                    float newFrame_Width = float.NaN;
                    float extended_CenterX = float.NaN;

                    float next_NewFrame_X = float.NaN;
                    float next_ScreenOutput_X = float.NaN;

                    float origPivotX_pixels = (origFrame.bboxMin.x + origFrame.bboxMax.x) / 2f;
                    //float kleiPivotWidth = origFrame.bboxMax.x - origFrame.bboxMin.x;
                    //float pivotXPercent = 0.5f - (kleiPivotX / kleiPivotWidth); // 0 is left, 0.5 is center, 1 is right

                    oldFrameToSymbolMap.Add(frameIndex, symbol.hash);
                    symbolToNewFramesMap.Add(symbol.hash, new List<KAnim.Build.SymbolFrameInstance>());

                    int idxSourceFrameNum = -1;

                    next_NewFrame_X = orig_X;
                    next_ScreenOutput_X = orig_X;

                    newFrame = origFrame;
                    idxSourceFrameNum++;
                    newFrame.sourceFrameNum = idxSourceFrameNum;
                    newFrame_Width = firstFrame_Width;
                    newFrame_X = next_NewFrame_X;
                    newFrame.uvMin.x = newFrame_X / textureWidth;
                    newFrame.uvMax.x = newFrame.uvMin.x + newFrame_Width / textureWidth;
                    screenOutput_Width = newFrame_Width;
                    screenOutput_X = next_ScreenOutput_X;
                    extended_CenterX = screenOutput_X + (screenOutput_Width / 2) - (final_Width / 2);
                    newFrame.bboxMin.x = origPivotX_pixels - screenOutput_Width + extended_CenterX * 2;
                    newFrame.bboxMax.x = origPivotX_pixels + screenOutput_Width + extended_CenterX * 2;
                    symbolToNewFramesMap[symbol.hash].Add(newFrame);
                    framesAddedForSymbol++;

                    next_NewFrame_X = newFrame_X + (newFrame_Width - 1) + 1;
                    next_ScreenOutput_X = screenOutput_X + (screenOutput_Width - 1) + 1;

                    float scaledWidth = float.NaN;
                    if (middle_Width > 0 && middle_Width <= 33)
                    {
                        scaledWidth = 25;
                    }
                    else if (middle_Width > 33 && middle_Width <= 66)
                    {
                        scaledWidth = 50;
                    }
                    else if (middle_Width > 66 && middle_Width <= 150)
                    {
                        scaledWidth = 100;
                    }
                    else
                    {
                        scaledWidth = 200;
                    }

                    switch (extStyle)
                    {
                        case FillingMethod.Stretch:
                            newFrame = origFrame;
                            idxSourceFrameNum++;
                            newFrame.sourceFrameNum = idxSourceFrameNum;
                            newFrame_Width = middle_Width;
                            newFrame_X = next_NewFrame_X; // orig_X + middle_X;
                            newFrame.uvMin.x = newFrame_X / textureWidth;
                            newFrame.uvMax.x = newFrame.uvMin.x + newFrame_Width / textureWidth;
                            screenOutput_Width = newFrame_Width + delta_Width;
                            screenOutput_X = next_ScreenOutput_X;
                            extended_CenterX = screenOutput_X + (screenOutput_Width / 2) - (final_Width / 2);
                            newFrame.bboxMin.x = origPivotX_pixels - screenOutput_Width + extended_CenterX * 2;
                            newFrame.bboxMax.x = origPivotX_pixels + screenOutput_Width + extended_CenterX * 2;
                            symbolToNewFramesMap[symbol.hash].Add(newFrame);
                            framesAddedForSymbol++;

                            next_NewFrame_X = newFrame_X + (newFrame_Width - 1) + 1;
                            next_ScreenOutput_X = screenOutput_X + (screenOutput_Width - 1) + 1;

                            break;

                        case FillingMethod.Repeat:
                            var next_ScreenOutput_RightX = next_ScreenOutput_X + (middle_Width - 1);

                            bool shit = next_ScreenOutput_RightX <= lastFrame_OutputX;
                            bool mirrorNextFrame = false;
                            while (shit)
                            {
                                newFrame = origFrame;
                                idxSourceFrameNum++;
                                newFrame.sourceFrameNum = idxSourceFrameNum;
                                newFrame_Width = middle_Width;
                                newFrame_X = next_NewFrame_X; // orig_X + firstFrame_Width;
                                newFrame.uvMin.x = newFrame_X / textureWidth;
                                newFrame.uvMax.x = newFrame.uvMin.x + newFrame_Width / textureWidth;
                                screenOutput_Width = scaledWidth; // newFrame_Width;
                                screenOutput_X = next_ScreenOutput_X;
                                extended_CenterX = screenOutput_X + (screenOutput_Width / 2) - (final_Width / 2);

                                if (!mirrorNextFrame)
                                {
                                    newFrame.bboxMin.x = origPivotX_pixels - screenOutput_Width + extended_CenterX * 2;
                                    newFrame.bboxMax.x = origPivotX_pixels + screenOutput_Width + extended_CenterX * 2;
                                }
                                else
                                {
                                    newFrame.bboxMin.x = origPivotX_pixels + screenOutput_Width + extended_CenterX * 2;
                                    newFrame.bboxMax.x = origPivotX_pixels - screenOutput_Width + extended_CenterX * 2;
                                }

                                symbolToNewFramesMap[symbol.hash].Add(newFrame);
                                framesAddedForSymbol++;

                                //next_NewFrame_X = newFrame_X + (newFrame_Width - 1) + 1;
                                next_ScreenOutput_X = screenOutput_X + (screenOutput_Width - 1) + 1;

                                next_ScreenOutput_RightX = next_ScreenOutput_X + (middle_Width - 1);
                                shit = next_ScreenOutput_RightX <= lastFrame_OutputX;

                                if (doFlipEverySecondIteration)
                                {
                                    mirrorNextFrame = !mirrorNextFrame;
                                }
                            }
                            next_NewFrame_X = newFrame_X + (newFrame_Width - 1) + 1;

                            break;

                        default:
                            break;
                    }

                    newFrame = origFrame;
                    idxSourceFrameNum++;
                    newFrame.sourceFrameNum = idxSourceFrameNum;
                    newFrame_Width = lastFrame_Width;
                    newFrame_X = orig_X + lastFrame_X; // prev_NewFrame_RightX; // orig_X + lastFrame_X;
                    newFrame.uvMin.x = newFrame_X / textureWidth;
                    newFrame.uvMax.x = newFrame.uvMin.x + newFrame_Width / textureWidth;
                    screenOutput_Width = newFrame_Width;
                    screenOutput_X = newFrame_X + delta_Width; // prev_ScreenOutput_RightX; // newFrame_X + delta_Width;
                    extended_CenterX = screenOutput_X + (screenOutput_Width / 2) - (final_Width / 2);
                    newFrame.bboxMin.x = origPivotX_pixels - screenOutput_Width + extended_CenterX * 2;
                    newFrame.bboxMax.x = origPivotX_pixels + screenOutput_Width + extended_CenterX * 2;
                    symbolToNewFramesMap[symbol.hash].Add(newFrame);
                    framesAddedForSymbol++;

                    next_NewFrame_X = newFrame_X + (newFrame_Width - 1) + 1;
                    next_ScreenOutput_X = screenOutput_X + (screenOutput_Width - 1) + 1;
                }
                symbol.numFrames = symbol.numFrames + framesAddedForSymbol - 1;

                //Debug.Log($"ExtendedBuildingWidth - animData.maxVisSymbolFrames before: {animData.maxVisSymbolFrames}");
                animData.maxVisSymbolFrames = Math.Max(animData.maxVisSymbolFrames, symbol.numFrames);
                //Debug.Log($"ExtendedBuildingWidth - animData.maxVisSymbolFrames after: {animData.maxVisSymbolFrames}");
            }
            //Debug.Log($"ExtendedBuildingWidth - BGD.maxVisibleSymbols before: {BGD.maxVisibleSymbols}");
            //var maxVisibleSymbols = BGD.maxVisibleSymbols;
            //maxVisibleSymbols = Math.Max(maxVisibleSymbols, animData.maxVisSymbolFrames);
            //Traverse.Create(BGD).Property("maxVisibleSymbols").SetValue(maxVisibleSymbols);
            BGD.UpdateMaxVisibleSymbols(animData.maxVisSymbolFrames);
            //Debug.Log($"ExtendedBuildingWidth - BGD.maxVisibleSymbols after: {BGD.maxVisibleSymbols}");

            var oldFrameIndexes = oldFrameToSymbolMap.Keys.OrderByDescending(x => x).Distinct().ToList();

            foreach (var idx in oldFrameIndexes)
            {
                symbolFrameInstances.RemoveAt(idx);

                var xxx = oldFrameToSymbolMap[idx];
                int innerIter = -1;
                foreach (var newFrame in symbolToNewFramesMap[xxx])
                {
                    innerIter++;
                    symbolFrameInstances.Insert(idx + innerIter, newFrame);
                }
            }

            //Debug.Log($"Symbols intermid:");
            //for (var i = 0; i < animData.build.symbols.Count(); i++)
            //{
            //    var entry = animData.build.symbols[i];
            //    Debug.Log($"{entry.hash}; {entry.firstFrameIdx}; {entry.numFrames}; {entry.numLookupFrames}; {entry.symbolIndexInSourceBuild}");
            //}

            // 1. Update symbolFrameInstances
            for (var i = 0; i < animData.build.symbols.Count(); i++)
            {
                var symbol = animData.build.symbols[i];

                symbol.numLookupFrames = 0;
                symbol.frameLookup = new int[] { };

                if (i == 0)
                {
                    symbol.firstFrameIdx = 0;
                }
                else
                {
                    var prevSymbol = animData.build.symbols[i - 1];
                    symbol.firstFrameIdx = prevSymbol.firstFrameIdx + (prevSymbol.numFrames - 1) + 1;
                }
            }

            // 2. Update BatchGroupData
            KGlobalAnimParser.PostParse(BGD);

            //foreach (var entry in symbolFrameInstances)
            //{
            //    Debug.Log($"ListEntry after: {entry.sourceFrameNum}; {entry.duration}; {entry.buildImageIdx}; {entry.symbolIdx}; {entry.uvMax.x}");
            //}

            //Debug.Log($"Symbols after:");
            //for (var i = 0; i < animData.build.symbols.Count(); i++)
            //{
            //    var entry = animData.build.symbols[i];
            //    Debug.Log($"{entry.hash}; {entry.firstFrameIdx}; {entry.numFrames}; {entry.numLookupFrames}; {entry.symbolIndexInSourceBuild}");
            //}

            var frameElements = BGD.frameElements;
            //Debug.Log($"Frame elems:");
            int idxFrameElem = -1;
            //foreach (var entry in frameElements)
            //{
            //    idxFrameElem++;
            //    Debug.Log($"{idxFrameElem}) {entry.frame}, {entry.symbol}, {entry.transform}");
            //}

            var animFrames = BGD.animFrames;
            //Debug.Log($"Anim Frames:");
            //var idxAnimFrame = -1;
            //foreach (var entry in animFrames)
            //{
            //    idxAnimFrame++;
            //    Debug.Log($"{idxAnimFrame}) {entry.firstElementIdx}, {entry.numElements}, {entry.hasHead}");
            //}

            var newFrameElems = new Dictionary<int, List<KAnim.Anim.FrameElement>>();
            var symbolsAffected = symbolToNewFramesMap.Keys.ToList();
            int frameElemIndex = -1;
            foreach (var frameElem in frameElements)
            {
                frameElemIndex++;

                if (!symbolToNewFramesMap.ContainsKey(frameElem.symbol))
                {
                    continue;
                }

                newFrameElems.Add(frameElemIndex, new List<KAnim.Anim.FrameElement>());
                for (int jj = 0; jj < symbolToNewFramesMap[frameElem.symbol].Count; jj++)
                {
                    var newFrameElem = frameElem;
                    newFrameElem.frame = jj;

                    newFrameElems[frameElemIndex].Add(newFrameElem);
                }
            }

            var newFrameElemsIndexes = newFrameElems.Keys.OrderByDescending(x => x).Distinct().ToList();

            foreach (var idx in newFrameElemsIndexes)
            {
                frameElements.RemoveAt(idx);

                int innerIter = -1;
                foreach (var newFrameElem in newFrameElems[idx])
                {
                    innerIter++;
                    frameElements.Insert(idx + innerIter, newFrameElem);
                }
            }

            //Debug.Log($"Frame elems after:");
            //idxFrameElem = -1;
            //foreach (var entry in frameElements)
            //{
            //    idxFrameElem++;
            //    Debug.Log($"{idxFrameElem}) {entry.frame}, {entry.symbol}, {entry.transform}");
            //}

            var newFrameElemsIndexes2 = newFrameElems.Keys.OrderBy(x => x).Distinct().ToList();
            var modifiedAnimFrames = new List<KAnim.Anim.Frame>();
            idxFrameElem = -1;
            foreach (var animFrame in BGD.animFrames)
            {
                idxFrameElem++;
                var modifiedAnimFrame = animFrame;

                var shift = 0;
                foreach (var jj in newFrameElemsIndexes2.Where(x => x < animFrame.firstElementIdx))
                {
                    shift += newFrameElems[jj].Count - 1;
                }

                modifiedAnimFrame.firstElementIdx += shift;
                if (newFrameElems.ContainsKey(idxFrameElem))
                {
                    modifiedAnimFrame.numElements += newFrameElems[idxFrameElem].Count - 1;
                }

                modifiedAnimFrames.Add(modifiedAnimFrame);
            }

            //Debug.Log($"modifiedAnimFrames:");
            //idxFrameElem = -1;
            //foreach (var entry in modifiedAnimFrames)
            //{
            //    idxFrameElem++;
            //    Debug.Log($"{idxFrameElem}) {entry.firstElementIdx}, {entry.numElements}, {entry.hasHead}");
            //}

            BGD.animFrames.Clear();
            BGD.animFrames.AddRange(modifiedAnimFrames);
        }

        public static void StretchBuildingGameObject(UnityEngine.GameObject gameObject, int width, int originalWidth, float animStretchModifier)
        {
            var buildingGameObject = gameObject.GetComponent<Building>().gameObject;
            var animController = buildingGameObject.GetComponent<KBatchedAnimController>();

            // Just stretching animController width for a building does not look fine (because for example dynamic bridges get progressively
            // more narrow), so it should be adjusted with modifier. For gas bridge this modifier is '1.12f'
            animController.animWidth = (float)width / (float)originalWidth * animStretchModifier;
        }
    }
}