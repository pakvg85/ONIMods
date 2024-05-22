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
        const int GameCellWidth = 100;

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
                    string origAnimName;
                    if (!configNameToAnimNamesMap.TryGetValue(item.ConfigName, out origAnimName))
                    {
                        continue;
                    }

                    KAnimFile origAnimFile;
                    if (!animTable.TryGetValue(new KAnimHashedString(origAnimName), out origAnimFile))
                    {
                        Debug.LogWarning($"ExtendedBuildingWidth - AddDynamicAnimsNames_To_ModLoadedKAnims: anim {origAnimName} for config {item.ConfigName} not loaded");
                        continue;
                    }

                    bool isMod = origAnimFile.mod != null;

                    for (var width = item.MinWidth; width <= item.MaxWidth; width++)
                    {
                        // At this moment we can't determine original buildingDef's width because it is not yet loaded.
                        // BuildingDefs are loaded later in 'BuildingConfigManager.RegisterBuilding'.
                        // So, KAnimFile will be created with the same width as the original, and name "XXX_widthYYY", but we simply won't be using it.
                        //if (width == originalDef.WidthInCells)
                        //{
                        //    continue;
                        //}

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
                    Debug.LogWarning($"ExtendedBuildingWidth - anims for config {item.ConfigName} were not loaded");
                    Debug.LogWarning(e.ToString());
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

        public static List<KAnim.Build.SymbolFrameInstance> SplitFrameIntoParts(
                KAnim.Build.SymbolFrameInstance origFrame,
                int textureWidth,
                int widthInCellsDelta,
                int middle_X,
                int middle_Width,
                FillingStyle fillingStyle,
                bool doFlipEverySecondIteration,
                int startingSourceFrameNum = 0
            )
        {
#if DEBUG
            Debug.Log("--- SplitFrameIntoParts");
#endif
            var result = new List<KAnim.Build.SymbolFrameInstance>();

            KAnim.Build.SymbolFrameInstance newFrame;
            float delta_Width = widthInCellsDelta * GameCellWidth;
            float orig_X = origFrame.uvMin.x * textureWidth;
            float orig_Width = (origFrame.uvMax.x - origFrame.uvMin.x) * textureWidth;
            float final_Width = orig_Width + delta_Width;
            float firstFrame_Width = middle_X;
            float lastFrame_X = middle_X + middle_Width;
            float lastFrame_Width = orig_Width - lastFrame_X + 1;
            float lastFrame_OutputX = 0 + lastFrame_X + (delta_Width - 1) + 1;
            float origPivotX_pixels = (origFrame.bboxMin.x + origFrame.bboxMax.x) / 2f;

            int idxSourceFrameNum = startingSourceFrameNum - 1;

            float next_NewFrame_X = orig_X;
            float next_ScreenOutput_X = 0;

            float newFrame_Width = firstFrame_Width;
            float newFrame_X = next_NewFrame_X;
            float screenOutput_Width = newFrame_Width;
            float screenOutput_X = next_ScreenOutput_X;
            float extended_CenterX = screenOutput_X + (screenOutput_Width / 2) - (final_Width / 2);
            newFrame = origFrame;
            idxSourceFrameNum++;
            newFrame.sourceFrameNum = idxSourceFrameNum;
            newFrame.uvMin.x = newFrame_X / textureWidth;
            newFrame.uvMax.x = newFrame.uvMin.x + newFrame_Width / textureWidth;
            newFrame.bboxMin.x = origPivotX_pixels - screenOutput_Width + extended_CenterX * 2;
            newFrame.bboxMax.x = origPivotX_pixels + screenOutput_Width + extended_CenterX * 2;
            //newFrame.bboxMin.y += 100;
            //newFrame.bboxMax.y += 100;
            result.Add(newFrame);

            next_NewFrame_X = newFrame_X + (newFrame_Width - 1) + 1;
            next_ScreenOutput_X = screenOutput_X + (screenOutput_Width - 1) + 1;

            switch (fillingStyle)
            {
                case FillingStyle.Stretch:
                    newFrame_Width = middle_Width;
                    newFrame_X = next_NewFrame_X; // orig_X + middle_X;
                    screenOutput_Width = newFrame_Width + delta_Width;
                    screenOutput_X = next_ScreenOutput_X;
                    extended_CenterX = screenOutput_X + (screenOutput_Width / 2) - (final_Width / 2);
                    newFrame = origFrame;
                    idxSourceFrameNum++;
                    newFrame.sourceFrameNum = idxSourceFrameNum;
                    newFrame.uvMin.x = newFrame_X / textureWidth;
                    newFrame.uvMax.x = newFrame.uvMin.x + newFrame_Width / textureWidth;
                    newFrame.bboxMin.x = origPivotX_pixels - screenOutput_Width + extended_CenterX * 2;
                    newFrame.bboxMax.x = origPivotX_pixels + screenOutput_Width + extended_CenterX * 2;
                    //newFrame.bboxMin.y += 100;
                    //newFrame.bboxMax.y += 100;

                    result.Add(newFrame);

                    next_NewFrame_X = newFrame_X + (newFrame_Width - 1) + 1;
                    next_ScreenOutput_X = screenOutput_X + (screenOutput_Width - 1) + 1;

                    break;

                case FillingStyle.Repeat:
                    float koef = (middle_Width + delta_Width) / middle_Width;
                    decimal koefRounded;
                    if (doFlipEverySecondIteration)
                    {
                        koefRounded = Math.Floor((decimal)koef) % 2 == 1 ? Math.Floor((decimal)koef) : Math.Ceiling((decimal)koef);
                    }
                    else
                    {
                        koefRounded = Math.Round((decimal)koef);
                    }
                    float scaledWidth = (middle_Width + delta_Width) / (float)koefRounded;

                    const int OverlappingToleranceInPixels = 3;

                    bool isNextFrameRightBorderOverlappingLastFrame = next_ScreenOutput_X + (scaledWidth - 1) <= lastFrame_OutputX + OverlappingToleranceInPixels;
                    bool mustMirrorFrame = false;
                    while (isNextFrameRightBorderOverlappingLastFrame)
                    {
                        newFrame_Width = middle_Width;
                        newFrame_X = next_NewFrame_X; // orig_X + firstFrame_Width;
                        screenOutput_Width = scaledWidth; // newFrame_Width;
                        screenOutput_X = next_ScreenOutput_X;
                        extended_CenterX = screenOutput_X + (screenOutput_Width / 2) - (final_Width / 2);
                        newFrame = origFrame;
                        idxSourceFrameNum++;
                        newFrame.sourceFrameNum = idxSourceFrameNum;
                        newFrame.uvMin.x = newFrame_X / textureWidth;
                        newFrame.uvMax.x = newFrame.uvMin.x + newFrame_Width / textureWidth;

                        int mirrorFrameModifier = mustMirrorFrame ? -1 : 1;
                        newFrame.bboxMin.x = origPivotX_pixels - screenOutput_Width * mirrorFrameModifier + extended_CenterX * 2;
                        newFrame.bboxMax.x = origPivotX_pixels + screenOutput_Width * mirrorFrameModifier + extended_CenterX * 2;
                        //newFrame.bboxMin.y += 100;
                        //newFrame.bboxMax.y += 100;

                        result.Add(newFrame);

                        //next_NewFrame_X = newFrame_X + (newFrame_Width - 1) + 1;
                        next_ScreenOutput_X = screenOutput_X + (screenOutput_Width - 1) + 1;
                        mustMirrorFrame = doFlipEverySecondIteration ? !mustMirrorFrame : mustMirrorFrame;

                        isNextFrameRightBorderOverlappingLastFrame = next_ScreenOutput_X + (scaledWidth - 1) <= lastFrame_OutputX + OverlappingToleranceInPixels;
                    }
                    next_NewFrame_X = newFrame_X + (newFrame_Width - 1) + 1;

                    break;

                default:
                    break;
            }

            newFrame_Width = lastFrame_Width;
            newFrame_X = orig_X + lastFrame_X; // prev_NewFrame_RightX; // orig_X + lastFrame_X;
            screenOutput_Width = newFrame_Width;
            screenOutput_X = lastFrame_OutputX;
            //screenOutput_X = newFrame_X + delta_Width; // prev_ScreenOutput_RightX; // newFrame_X + delta_Width;
            //screenOutput_X = next_ScreenOutput_X;
            extended_CenterX = screenOutput_X + (screenOutput_Width / 2) - (final_Width / 2);
            newFrame = origFrame;
            idxSourceFrameNum++;
            newFrame.sourceFrameNum = idxSourceFrameNum;
            newFrame.uvMin.x = newFrame_X / textureWidth;
            newFrame.uvMax.x = newFrame.uvMin.x + newFrame_Width / textureWidth;
            newFrame.bboxMin.x = origPivotX_pixels - screenOutput_Width + extended_CenterX * 2;
            newFrame.bboxMax.x = origPivotX_pixels + screenOutput_Width + extended_CenterX * 2;
            //newFrame.bboxMin.y += 100;
            //newFrame.bboxMax.y += 100;

            result.Add(newFrame);

            next_NewFrame_X = newFrame_X + (newFrame_Width - 1) + 1;
            next_ScreenOutput_X = screenOutput_X + (screenOutput_Width - 1) + 1;

            return result;
        }

        public static void StretchBuildingGameObject(UnityEngine.GameObject gameObject, int width, int originalWidth, float animStretchModifier)
        {
            var buildingGameObject = gameObject.GetComponent<Building>().gameObject;
            var animController = buildingGameObject.GetComponent<KBatchedAnimController>();

            // Just stretching animController width for a building does not look fine (because for example dynamic bridges get progressively
            // more narrow), so it should be adjusted with modifier. For gas bridge this modifier is '1.12f'
            animController.animWidth = (float)width / (float)originalWidth * animStretchModifier;
        }

        public static Sprite GenerateSpriteForFrame(
                KAnim.Build.SymbolFrameInstance symbolFrameInstance,
                Texture2D texture,
                bool flipByX = false
            )
        {
#if DEBUG
            Debug.Log("--- GenerateSpriteForFrame");
#endif
            //const bool centered = false;

            float textureWidth = texture.width;
            float textureHeight = texture.height;

            float minX = symbolFrameInstance.uvMin.x;
            float maxX = symbolFrameInstance.uvMax.x;
            float maxY = symbolFrameInstance.uvMax.y;
            float minY = symbolFrameInstance.uvMin.y;

            //int flipByXModifier = flipByX ? -1 : 1;

            int width = (int)((float)textureWidth * Mathf.Abs(maxX - minX));
            int height = (int)((float)textureHeight * Mathf.Abs(minY - maxY));

            Rect rect = default(Rect);
            rect.x = (float)((int)((float)textureWidth * minX));
            rect.y = (float)((int)((float)textureHeight * maxY));
            rect.width = (float)width;
            rect.height = (float)height;

            //if (flipByX)
            //{
            //    rect.x = -(float)((int)((float)textureWidth * maxX));
            //    rect.width = -rect.width;
            //}
            //Debug.Log($"Sprite coords: {rect.x}, {rect.width}, {rect.y}, {rect.height}");

            float pixelsPerUnit = GameCellWidth;
            float widthScaled = Mathf.Abs(symbolFrameInstance.bboxMax.x - symbolFrameInstance.bboxMin.x) / 2;

            if (width != 0)
            {
                pixelsPerUnit = Math.Abs ((float)GameCellWidth / (widthScaled / (float)width));
            }

            ////// Sprite - CreateSprite(texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape);
            //var method = typeof(Sprite).GetMethod("CreateSprite", BindingFlags.Static | BindingFlags.NonPublic);
            //var rez = method.Invoke(
            //    obj: null, 
            //    parameters: new object[] { 
            //        texture, 
            //        rect, 
            //        Vector2.zero, 
            //        pixelsPerUnit, 
            //        0U, 
            //        SpriteMeshType.FullRect,
            //        Vector4.zero,
            //        false
            //    });
            //var result = rez as Sprite;

            var pivot = flipByX ? new Vector2(-1f, 0f) : Vector2.zero;

#if DEBUG
            Debug.Log($"rect=[{rect.x}, {rect.width}] - [{rect.y}, {rect.height}]; pixelsPerUnit={pixelsPerUnit}; pivot={pivot}");
            Debug.Log($"texture={texture.width} - {texture.height}; widthScaled={widthScaled}");
#endif

            var result = Sprite.Create(
                    texture,
                    rect,
                    //pivot: centered ? new Vector2(0.5f, 0.5f) : Vector2.zero,
                    pivot: pivot,
                    pixelsPerUnit,
                    extrude: 0U,
                    meshType: SpriteMeshType.FullRect
                );

            //SpriteRenderer spriteRenderer = new SpriteRenderer();
            //spriteRenderer.sprite = result;
            //spriteRenderer.flipX = true;
            //spriteRenderer.

            result.name = $"{texture.name}:{symbolFrameInstance.symbolIdx}:{symbolFrameInstance.buildImageIdx}";
            return result;
        }

        private static AnimSplittingSettings_Internal MapToInternal(AnimSplittingSettings entry, string SymbolName, string FrameIndex)
        {
            if (   string.IsNullOrEmpty(SymbolName)
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
                IsActive = entry.IsActive,
                MiddlePart_X = entry.MiddlePart_X,
                MiddlePart_Width = entry.MiddlePart_Width,
                FillingStyle = entry.FillingMethod,
                DoFlipEverySecondIteration = entry.DoFlipEverySecondIteration
            };
            return newEntry;
        }

        private static List<AnimSplittingSettings_Internal> MapToInternal(List<AnimSplittingSettings> list)
        {
            var result = new List<AnimSplittingSettings_Internal>();
            foreach (var entry in list)
            {
                try
                {
                    if (   !string.IsNullOrEmpty(entry.SymbolName)
                        && !string.IsNullOrEmpty(entry.FrameIndex)
                        )
                    {
                        var newEntry = MapToInternal(entry, entry.SymbolName, entry.FrameIndex);
                        result.Add(newEntry);
                    }
                    else if (  !string.IsNullOrEmpty(entry.SymbolName)
                            && string.IsNullOrEmpty(entry.FrameIndex)
                            )
                    { 
                        var config = DynamicBuildingsManager.ConfigMap[entry.ConfigName];
                        var originalDef = DynamicBuildingsManager.ConfigToBuildingDefMap[config];
                        var symbol = originalDef.AnimFiles.First().GetData().build.GetSymbol(entry.SymbolName);
                        for (var i = 0; i < symbol.numFrames; i++)
                        {
                            var newEntry = MapToInternal(entry, symbol.hash.ToString(), i.ToString());
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
                                var newEntry = MapToInternal(entry, symbol.hash.ToString(), i.ToString());
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

        public static void SplitAnim(
                KAnimFile animFile,
                int widthInCellsDelta,
                List<AnimSplittingSettings> settingsItems
            )
        {
            if (animFile == null)
            {
                throw new ArgumentNullException("animFile");
            }
            if (animFile?.GetData()?.build?.batchTag == null)
            {
                throw new ArgumentNullException("animFile?.GetData()?.build?.batchTag");
            }

            var settingsInternal = MapToInternal(settingsItems);
#if DEBUG
            Debug.Log($"settingsItems:");
            foreach (var value in settingsItems)
            {
                Debug.Log($"{value.ConfigName} - [{value.SymbolName}, {value.FrameIndex}]");
            }

            Debug.Log($"settingsItemsInternal:");
            foreach (var value in settingsInternal)
            {
                Debug.Log($"{value.ConfigName} - [{value.SymbolName}, {value.FrameIndex}]");
            }
#endif
            var settingsItemsDict = settingsInternal.ToDictionary(x => System.Tuple.Create(x.SymbolName, x.FrameIndex), y => y);

            var validSymbolNames = settingsInternal.Select(x => x.SymbolName).Distinct().ToHashSet();

            var animData = animFile.GetData();
            var texture = animFile.textureList.First();
            int textureWidth = texture.width;
            var BGD = KAnimBatchManager.Instance().GetBatchGroupData(animData.build.batchTag);

#if DEBUG
            Debug.Log($"BGD before: maxVisibleSymbols={BGD.maxVisibleSymbols} - maxSymbolFrameInstancesPerbuild={BGD.maxSymbolFrameInstancesPerbuild}");
            Debug.Log($"--- BGD.frameElementSymbolIndices:");
            var idx1 = -1;
            foreach (var value in BGD.frameElementSymbolIndices)
            {
                idx1++;
                Debug.Log($"frameElem #{idx1}: [{value.Key}, {value.Value}]");
            }
            Debug.Log($"--- animData: anCnt={animData.animCount} - elCnt={animData.elementCount} - frCnt={animData.frameCount} - idx={animData.index} - bldIdx={animData.buildIndex} - firAnIdx={animData.firstAnimIndex} - firElIdx={animData.firstElementIndex} - maxVis={animData.maxVisSymbolFrames}");
            for (var idx = 0; idx < animData.animCount; idx++)
            {
                var anim = animData.GetAnim(idx);
                Debug.Log($"anim #{idx}: firstFrameIdx={anim.firstFrameIdx} - numFrames={anim.numFrames}");
            }
#endif

            var tupleToNewFramesMap = GenerateNewFramesForSymbols(animData, validSymbolNames, settingsItemsDict, textureWidth, widthInCellsDelta);
            ReplaceOriginalSymbolFramesWithTheNewlyGenerated(BGD, tupleToNewFramesMap);
            UpdateSymbolNumFramesAndTotalMaxVisibleFrames(animData, BGD, tupleToNewFramesMap);
            var origFrameElementIndexToNewFrameElementsMap = CreateMappingOfOriginalFrameIndexesToTheNewlyCreatedFrames(BGD, tupleToNewFramesMap);
            ModifyAnimFrameElements(BGD, origFrameElementIndexToNewFrameElementsMap);
            ModifyAnimFrames(BGD, origFrameElementIndexToNewFrameElementsMap);

            animData.maxVisSymbolFrames = Math.Max(animData.maxVisSymbolFrames, BGD.symbolFrameInstances.Count);
            BGD.UpdateMaxVisibleSymbols(BGD.symbolFrameInstances.Count);

#if DEBUG
            Debug.Log($"BGD after:  maxVisibleSymbols={BGD.maxVisibleSymbols} - maxSymbolFrameInstancesPerbuild={BGD.maxSymbolFrameInstancesPerbuild}");
            Debug.Log($"--- animData: anCnt={animData.animCount} - elCnt={animData.elementCount} - frCnt={animData.frameCount} - idx={animData.index} - bldIdx={animData.buildIndex} - firAnIdx={animData.firstAnimIndex} - firElIdx={animData.firstElementIndex} - maxVis={animData.maxVisSymbolFrames}");
            Debug.Log($"--- BGD.frameElements.Count={BGD.frameElements.Count} - BGD.animFrames.Count={BGD.animFrames.Count}");
            for (var idx = 0; idx < animData.animCount; idx++)
            {
                var anim = animData.GetAnim(idx);
                Debug.Log($"anim #{idx}: firstFrameIdx={anim.firstFrameIdx} - numFrames={anim.numFrames}");
            }
            Debug.Log($"--- BGD.frameElementSymbolIndices:");
            var idx2 = -1;
            foreach (var value in BGD.frameElementSymbolIndices)
            {
                idx2++;
                Debug.Log($"frameElem #{idx2}: [{value.Key}, {value.Value}]");
            }
#endif
        }

        private static Dictionary<System.Tuple<KAnimHashedString, int>, List<KAnim.Build.SymbolFrameInstance>> GenerateNewFramesForSymbols(
            KAnimFileData animData,
            HashSet<KAnimHashedString> validSymbolNames,
            Dictionary<System.Tuple<KAnimHashedString, int>, AnimSplittingSettings_Internal> settingsItemsDict,
            int textureWidth,
            int widthInCellsDelta
            )
        {
#if DEBUG
            Debug.Log($"--- Generate new frames for symbols");
#endif
            var tupleToNewFramesMap = new Dictionary<System.Tuple<KAnimHashedString, int>, List<KAnim.Build.SymbolFrameInstance>>();
            for (uint symbolIter = 0; symbolIter < animData.build.symbols.Count(); symbolIter++)
            {
                var symbol = animData.build.GetSymbolByIndex(symbolIter);

                if (   symbol.hash.ToString() == "ui"
                    || !validSymbolNames.Contains(symbol.hash)
                    )
                {
                    for (int origFrameIndex = 0; origFrameIndex < symbol.numFrames; origFrameIndex++)
                    {
                        var origFrame = symbol.GetFrame(origFrameIndex);
                        var key = System.Tuple.Create(symbol.hash, origFrameIndex);
                        tupleToNewFramesMap.Add(key, new List<KAnim.Build.SymbolFrameInstance>() { origFrame });
                    }
                    continue;
                }

#if DEBUG
                Debug.Log($"Processing symbol {symbol.hash}");
#endif
                int shift = 0;
                for (int origFrameIndex = 0; origFrameIndex < symbol.numFrames; origFrameIndex++)
                {
                    var origFrame = symbol.GetFrame(origFrameIndex);

                    var shiftedIndex = origFrameIndex + shift;
                    var origFrameWithShiftedIndex = origFrame;
                    origFrameWithShiftedIndex.sourceFrameNum = shiftedIndex;

                    var key = System.Tuple.Create(symbol.hash, origFrameIndex);

                    if (!settingsItemsDict.TryGetValue(key, out var settings_Item))
                    {
                        tupleToNewFramesMap.Add(key, new List<KAnim.Build.SymbolFrameInstance>() { origFrameWithShiftedIndex });
                        continue;
                    }

#if DEBUG
                    Debug.Log($"Processing frame: origFrameIndex={origFrameIndex} / shiftedIndex={shiftedIndex} / shift={shift}");
#endif
                    var smallerFrames = SplitFrameIntoParts(
                        origFrame,
                        textureWidth,
                        widthInCellsDelta,
                        settings_Item.MiddlePart_X,
                        settings_Item.MiddlePart_Width,
                        settings_Item.FillingStyle,
                        settings_Item.DoFlipEverySecondIteration,
                        startingSourceFrameNum: shiftedIndex
                    );
                    shift += smallerFrames.Count - 1;
#if DEBUG
                    Debug.Log($"new shift={shift}");
                    Debug.Log($"orig frame: [{origFrame.symbolIdx}, {origFrame.sourceFrameNum}] - ({origFrame.uvMin.x},{origFrame.uvMax.x}) - ({origFrame.bboxMin.x},{origFrame.bboxMax.x})");
                    var idx = -1;
                    foreach (var frame in smallerFrames)
                    {
                        idx++;
                        Debug.Log($"frame #{idx}: [{frame.symbolIdx}, {frame.sourceFrameNum}] - ({frame.uvMin.x},{frame.uvMax.x}) - ({frame.bboxMin.x},{frame.bboxMax.x})");
                    }
#endif

                    tupleToNewFramesMap.Add(key, smallerFrames);
                }
            }

            return tupleToNewFramesMap;
        }

        private static void ReplaceOriginalSymbolFramesWithTheNewlyGenerated(
                KBatchGroupData BGD,
                Dictionary<System.Tuple<KAnimHashedString, int>, List<KAnim.Build.SymbolFrameInstance>> tupleToNewFramesMap
            )
        {
#if DEBUG
            Debug.Log($"--- Replace original symbol frames with the newly generated ones");
            Debug.Log($"BGD.symbolFrameInstances before: {BGD.symbolFrameInstances.Count}");
            var idx = -1;
            foreach (var frame in BGD.symbolFrameInstances)
            {
                idx++;
                Debug.Log($"frame #{idx}: [{frame.symbolIdx}, {frame.sourceFrameNum}] - ({frame.uvMin.x},{frame.uvMax.x}) - ({frame.bboxMin.x},{frame.bboxMax.x})");
            }
#endif

            BGD.symbolFrameInstances.Clear();
            foreach (var entryKey in tupleToNewFramesMap.Keys)
            {
                var values = tupleToNewFramesMap[entryKey];
                BGD.symbolFrameInstances.AddRange(values);
            }

#if DEBUG
            Debug.Log($"BGD.symbolFrameInstances after: {BGD.symbolFrameInstances.Count}");
            var idx2 = -1;
            foreach (var frame in BGD.symbolFrameInstances)
            {
                idx2++;
                Debug.Log($"frame #{idx2}: [{frame.symbolIdx}, {frame.sourceFrameNum}] - ({frame.uvMin.x},{frame.uvMax.x}) - ({frame.bboxMin.x},{frame.bboxMax.x})");
            }
#endif
        }

        private static void UpdateSymbolNumFramesAndTotalMaxVisibleFrames(
                KAnimFileData animData,
                KBatchGroupData BGD,
                Dictionary<System.Tuple<KAnimHashedString, int>, List<KAnim.Build.SymbolFrameInstance>> tupleToNewFramesMap
            )
        {
#if DEBUG
            Debug.Log($"--- Update 'symbol.numFrames' and total 'max visible frames'");
            Debug.Log($"Symbols before:");
            string lookup;
            for (var symbolIdx = 0; symbolIdx < animData.build.symbols.Count(); symbolIdx++)
            {
                var symbol = animData.build.symbols[symbolIdx];
                lookup = "";
                foreach (var jjj in symbol.frameLookup) { lookup += jjj.ToString() + ";"; }
                Debug.Log($"symbol: [{symbol.hash}] - {symbol.firstFrameIdx} - {symbol.numFrames} - {symbol.symbolIndexInSourceBuild} - {symbol.numLookupFrames} - lookup=[{lookup}]");
            }
#endif
            foreach (var keyValuePair in tupleToNewFramesMap)
            {
                var tuple = keyValuePair.Key;
                var values = keyValuePair.Value;
                var symbolNameHash = tuple.Item1;
                var symbol = animData.build.GetSymbol(symbolNameHash);
                symbol.numFrames += values.Count() - 1;
            }

            // 'firstFrameIdx' and lookup info were tainted because 'BGD.symbolFrameInstances' was modified.
            // We have to redefine 'firstFrameIdx' here and then redefine lookup info in 'KGlobalAnimParser.PostParse(BGD)'
            for (var symbolIdx = 0; symbolIdx < animData.build.symbols.Count(); symbolIdx++)
            {
                var symbol = animData.build.symbols[symbolIdx];

                symbol.numLookupFrames = 0;
                symbol.frameLookup = new int[] { };

                if (symbolIdx == 0)
                {
                    symbol.firstFrameIdx = 0;
                }
                else
                {
                    var prevSymbol = animData.build.symbols[symbolIdx - 1];
                    symbol.firstFrameIdx = prevSymbol.firstFrameIdx + (prevSymbol.numFrames - 1) + 1;
                }
            }
            KGlobalAnimParser.PostParse(BGD);
#if DEBUG
            Debug.Log($"Symbols after:");
            for (var symbolIdx = 0; symbolIdx < animData.build.symbols.Count(); symbolIdx++)
            {
                var symbol = animData.build.symbols[symbolIdx];
                lookup = "";
                foreach (var jjj in symbol.frameLookup) { lookup += jjj.ToString() + ";"; }
                Debug.Log($"symbol: [{symbol.hash}] - {symbol.firstFrameIdx} - {symbol.numFrames} - {symbol.symbolIndexInSourceBuild} - {symbol.numLookupFrames} - lookup=[{lookup}]");
            }
#endif
        }

        private static Dictionary<int, List<KAnim.Anim.FrameElement>> CreateMappingOfOriginalFrameIndexesToTheNewlyCreatedFrames(
                KBatchGroupData BGD,
                Dictionary<System.Tuple<KAnimHashedString, int>, List<KAnim.Build.SymbolFrameInstance>> tupleToNewFramesMap
            )
        {
#if DEBUG
            Debug.Log($"--- Create mapping of original frame indexes to the newly created frames");
#endif
            var origFrameElementIndexToNewFrameElementsMap = new Dictionary<int, List<KAnim.Anim.FrameElement>>();
            int origFrameElementIndex = -1;
            foreach (var origFrameElement in BGD.frameElements)
            {
                origFrameElementIndex++;

#if DEBUG
                Debug.Log($"origFrameElement: [{origFrameElement.symbol}, {origFrameElement.frame}]");
#endif

                var tuple = System.Tuple.Create(origFrameElement.symbol, origFrameElement.frame);

                List<KAnim.Build.SymbolFrameInstance> newSymbolFrames;
                if (!tupleToNewFramesMap.TryGetValue(tuple, out newSymbolFrames))
                {
                    throw new KeyNotFoundException($"key tuple not found: [{origFrameElement.symbol}, {origFrameElement.frame}]");
                }

                var newFrameElements = new List<KAnim.Anim.FrameElement>();
                foreach (var newSymbolFrame in newSymbolFrames)
                {
                    var newFrameElement = origFrameElement;
                    newFrameElement.frame = newSymbolFrame.sourceFrameNum;

#if DEBUG
                    Debug.Log($"newFrameElement: [{newFrameElement.symbol}, {newFrameElement.frame}]");
#endif
                    newFrameElements.Add(newFrameElement);
                }
                origFrameElementIndexToNewFrameElementsMap.Add(origFrameElementIndex, newFrameElements);
            }

#if DEBUG
            Debug.Log($"origFrameElementIndexToNewFrameElementsMap:"); 
            foreach (var kkk in origFrameElementIndexToNewFrameElementsMap)
            {
                Debug.Log($"key = origFrameElementIndex={kkk.Key} - numFrames={kkk.Value.Count}");
            }
#endif
            return origFrameElementIndexToNewFrameElementsMap;
        }

        private static void ModifyAnimFrameElements(
                KBatchGroupData BGD,
                Dictionary<int, List<KAnim.Anim.FrameElement>> origFrameElementIndexToNewFrameElementsMap
            )
        {
#if DEBUG
            Debug.Log($"--- Modify Anim Frame Elements");
            Debug.Log($"FrameElems before:");
            var idx = -1;
            foreach (var frameElem in BGD.frameElements)
            {
                idx++;
                Debug.Log($"frameElem #{idx}: [{frameElem.symbol}, {frameElem.frame}]");
            }
#endif
            var origFrameElementsIndexes = origFrameElementIndexToNewFrameElementsMap.Keys.OrderByDescending(x => x).Distinct().ToList();
            foreach (var origIndex in origFrameElementsIndexes)
            {
                BGD.frameElements.RemoveAt(origIndex);

                var newFrameElements = origFrameElementIndexToNewFrameElementsMap[origIndex];
                BGD.frameElements.InsertRange(origIndex, newFrameElements);
            }

#if DEBUG
            Debug.Log($"FrameElems after:");
            var idx2 = -1;
            foreach (var frameElem in BGD.frameElements)
            {
                idx2++;
                Debug.Log($"frameElem #{idx2}: [{frameElem.symbol}, {frameElem.frame}]");
            }
#endif
        }

        private static void ModifyAnimFrames(
                KBatchGroupData BGD,
                Dictionary<int, List<KAnim.Anim.FrameElement>> origFrameElementIndexToNewFrameElementsMap
            )
        {
#if DEBUG
            Debug.Log($"--- Modify Anim Frames");
            Debug.Log($"AnimFrames before:");
            var idx = -1;
            foreach (var animFrame in BGD.animFrames)
            {
                idx++;
                Debug.Log($"animFrame #{idx}: {animFrame.firstElementIdx} - {animFrame.numElements}");
            }
#endif
            var origFrameElementsIndexes = origFrameElementIndexToNewFrameElementsMap.Keys.OrderBy(x => x).Distinct().ToList();
            var modifiedAnimFrames = new List<KAnim.Anim.Frame>();
            int idxFrame = -1;
            foreach (var animFrame in BGD.animFrames)
            {
                idxFrame++;
                var modifiedAnimFrame = animFrame;
#if DEBUG
                Debug.Log($"origAnimFrame #{idxFrame}: {animFrame.firstElementIdx} - {animFrame.numElements}");
#endif
                var shift = 0;
                foreach (var origIndex in origFrameElementsIndexes.Where(x => x < animFrame.firstElementIdx))
                {
                    shift += origFrameElementIndexToNewFrameElementsMap[origIndex].Count - 1;
                }

                modifiedAnimFrame.firstElementIdx += shift;

                for (var mmm = animFrame.firstElementIdx; mmm < animFrame.firstElementIdx + animFrame.numElements; mmm++)
                {
                    List<KAnim.Anim.FrameElement> newFrameElements;
                    if (origFrameElementIndexToNewFrameElementsMap.TryGetValue(mmm, out newFrameElements))
                    {
                        modifiedAnimFrame.numElements += newFrameElements.Count - 1;
                    }
                }
#if DEBUG
                Debug.Log($"modifAnimFrame #{idxFrame}: shift={shift} - {modifiedAnimFrame.firstElementIdx} - {modifiedAnimFrame.numElements}");
#endif
                modifiedAnimFrames.Add(modifiedAnimFrame);
            }

            BGD.animFrames.Clear();
            BGD.animFrames.AddRange(modifiedAnimFrames);

#if DEBUG
            Debug.Log($"AnimFrames after:");
            var idx2 = -1;
            foreach (var animFrame in BGD.animFrames)
            {
                idx2++;
                Debug.Log($"animFrame #{idx2}: {animFrame.firstElementIdx} - {animFrame.numElements}");
            }
#endif
        }
    }

}