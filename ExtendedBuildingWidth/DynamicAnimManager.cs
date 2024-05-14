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

        private static float RescaleWidthToFitIntoGameCell(float width)
        {
            if (width > 0 && width <= 33)
            {
                return 25;
            }
            else if (width > 33 && width <= 66)
            {
                return 50;
            }
            else if (width > 66 && width <= 150)
            {
                return 100;
            }
            else
            {
                return 200;
            }
        }

        public static List<KAnim.Build.SymbolFrameInstance> SplitFrameIntoParts(
                KAnim.Build.SymbolFrameInstance origFrame,
                int textureWidth,
                int widthInCellsDelta,
                int middle_X,
                int middle_Width,
                FillingMethod fillingMethod,
                bool doFlipEverySecondIteration
            )
        {
            var result = new List<KAnim.Build.SymbolFrameInstance>();

            KAnim.Build.SymbolFrameInstance newFrame;
            float delta_Width = widthInCellsDelta * GameCellWidth;
            float orig_X = origFrame.uvMin.x * textureWidth;
            float orig_Width = (origFrame.uvMax.x - origFrame.uvMin.x) * textureWidth;
            float final_Width = orig_Width + delta_Width;
            float firstFrame_Width = middle_X;
            float lastFrame_X = middle_X + middle_Width;
            float lastFrame_Width = orig_Width - lastFrame_X + 1;
            float lastFrame_OutputX = orig_X + lastFrame_X + (delta_Width - 1) + 1;
            float origPivotX_pixels = (origFrame.bboxMin.x + origFrame.bboxMax.x) / 2f;

            int idxSourceFrameNum = -1;

            float next_NewFrame_X = orig_X;
            float next_ScreenOutput_X = orig_X;

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
            result.Add(newFrame);

            next_NewFrame_X = newFrame_X + (newFrame_Width - 1) + 1;
            next_ScreenOutput_X = screenOutput_X + (screenOutput_Width - 1) + 1;

            switch (fillingMethod)
            {
                case FillingMethod.Stretch:
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

                    result.Add(newFrame);

                    next_NewFrame_X = newFrame_X + (newFrame_Width - 1) + 1;
                    next_ScreenOutput_X = screenOutput_X + (screenOutput_Width - 1) + 1;

                    break;

                case FillingMethod.Repeat:
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
            screenOutput_X = newFrame_X + delta_Width; // prev_ScreenOutput_RightX; // newFrame_X + delta_Width;
            extended_CenterX = screenOutput_X + (screenOutput_Width / 2) - (final_Width / 2);
            newFrame = origFrame;
            idxSourceFrameNum++;
            newFrame.sourceFrameNum = idxSourceFrameNum;
            newFrame.uvMin.x = newFrame_X / textureWidth;
            newFrame.uvMax.x = newFrame.uvMin.x + newFrame_Width / textureWidth;
            newFrame.bboxMin.x = origPivotX_pixels - screenOutput_Width + extended_CenterX * 2;
            newFrame.bboxMax.x = origPivotX_pixels + screenOutput_Width + extended_CenterX * 2;

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

            var result = Sprite.Create(
                    texture,
                    rect,
                    //pivot: centered ? new Vector2(0.5f, 0.5f) : Vector2.zero,
                    pivot: flipByX ? new Vector2(-1f, 0f) : Vector2.zero,
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

        public static List<Sprite> GenerateSpritesForSymbol(
                KAnim.Build.Symbol symbol,
                Texture2D texture,
                bool doFlipEverySecondIteration
            )
        {
            var result = new List<Sprite>();

            bool flipByX = false;
            for (int frameIter = 0; frameIter < symbol.numFrames; frameIter++)
            {
                var origFrame = symbol.GetFrame(frameIter);

                var sprite = GenerateSpriteForFrame(
                        origFrame,
                        texture,
                        flipByX: flipByX
                    );
                result.Add(sprite);

                flipByX = doFlipEverySecondIteration ? !flipByX : false;
            }

            return result;
        }

        /// <summary>
        /// It is not convenient to split internal blocks of code into separate smaller methods.
        /// </summary>
        public static void SplitAnim(
                KAnimFile animFile,
                int widthInCellsDelta,
                int middle_X,
                int middle_Width,
                FillingMethod fillingMethod,
                bool doFlipEverySecondIteration
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

            var animData = animFile.GetData();
            var texture = animFile.textureList.FirstOrDefault();
            int textureWidth = texture.width;
            var BGD = KAnimBatchManager.Instance().GetBatchGroupData(animData.build.batchTag);
            //var delta_Width = widthInCellsDelta * cellWidthPx;

            // Generate new frames for symbols
            var symbolToNewFramesMap = new Dictionary<KAnimHashedString, List<KAnim.Build.SymbolFrameInstance>>();
            for (uint symbolIter = 0; symbolIter < animData.build.symbols.Count(); symbolIter++)
            {
                var symbol = animData.build.GetSymbolByIndex(symbolIter);

                if (symbol.hash.ToString() == "ui")
                {
                    continue;
                }

                for (int frameIter = 0; frameIter < symbol.numFrames; frameIter++)
                {
                    var origFrame = symbol.GetFrame(frameIter);
                    var smallerFrames = SplitFrameIntoParts(
                        origFrame,
                        textureWidth,
                        widthInCellsDelta,
                        middle_X,
                        middle_Width,
                        fillingMethod,
                        doFlipEverySecondIteration
                    );
                    symbolToNewFramesMap.Add(symbol.hash, smallerFrames);
                }
            }

            // Save initial frame indexes
            var oldFrameIndexToSymbolNameMap = new Dictionary<int, KAnimHashedString>();
            for (uint symbolIter = 0; symbolIter < animData.build.symbols.Count(); symbolIter++)
            {
                var symbol = animData.build.GetSymbolByIndex(symbolIter);

                if (symbol.hash.ToString() == "ui")
                {
                    continue;
                }

                for (int frameIter = 0; frameIter < symbol.numFrames; frameIter++)
                {
                    var frameIndex = symbol.GetFrameIdx(frameIter);
                    oldFrameIndexToSymbolNameMap.Add(frameIndex, symbol.hash);
                }
            }

            // Replace original symbol frames with the newly generated ones
            var oldFrameIndexes = oldFrameIndexToSymbolNameMap.Keys.OrderByDescending(x => x).Distinct().ToList();
            foreach (var oldFrameIndex in oldFrameIndexes)
            {
                BGD.symbolFrameInstances.RemoveAt(oldFrameIndex);

                var symbolNameHash = oldFrameIndexToSymbolNameMap[oldFrameIndex];

                var newFrames = symbolToNewFramesMap[symbolNameHash];
                BGD.symbolFrameInstances.InsertRange(oldFrameIndex, newFrames);
            }

            // Update 'symbol.numFrames' and total 'max visible frames'
            foreach (var symbolNameHash in symbolToNewFramesMap.Keys)
            {
                var symbol = animData.build.GetSymbol(symbolNameHash);
                symbol.numFrames += symbolToNewFramesMap[symbolNameHash].Count() - 1;

                animData.maxVisSymbolFrames = Math.Max(animData.maxVisSymbolFrames, symbol.numFrames);
            }
            BGD.UpdateMaxVisibleSymbols(animData.maxVisSymbolFrames);

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

            // Create mapping of original frame indexes to the newly created frames
            var origFrameElementIndexToNewFrameElementsMap = new Dictionary<int, List<KAnim.Anim.FrameElement>>();
            var symbolsAffected = symbolToNewFramesMap.Keys.ToList();
            int origFrameElementIndex = -1;
            foreach (var origFrameElement in BGD.frameElements)
            {
                origFrameElementIndex++;

                if (!symbolToNewFramesMap.ContainsKey(origFrameElement.symbol))
                {
                    continue;
                }

                var newFrameElements = new List<KAnim.Anim.FrameElement>();
                for (int newFrameElementIndex = 0; newFrameElementIndex < symbolToNewFramesMap[origFrameElement.symbol].Count; newFrameElementIndex++)
                {
                    var newFrameElement = origFrameElement;
                    newFrameElement.frame = newFrameElementIndex;
                    newFrameElements.Add(newFrameElement);
                }
                origFrameElementIndexToNewFrameElementsMap.Add(origFrameElementIndex, newFrameElements);
            }

            // Modify Anim Frame Elements
            var origFrameElementsIndexes = origFrameElementIndexToNewFrameElementsMap.Keys.OrderByDescending(x => x).Distinct().ToList();
            foreach (var idx in origFrameElementsIndexes)
            {
                BGD.frameElements.RemoveAt(idx);

                var newFrameElements = origFrameElementIndexToNewFrameElementsMap[idx];
                BGD.frameElements.InsertRange(idx, newFrameElements);
            }

            // Modify Anim Frames
            origFrameElementsIndexes = origFrameElementIndexToNewFrameElementsMap.Keys.OrderBy(x => x).Distinct().ToList();
            var modifiedAnimFrames = new List<KAnim.Anim.Frame>();
            int idxFrameElem = -1;
            foreach (var animFrame in BGD.animFrames)
            {
                idxFrameElem++;
                var modifiedAnimFrame = animFrame;

                var shift = 0;
                foreach (var idx in origFrameElementsIndexes.Where(x => x < animFrame.firstElementIdx))
                {
                    shift += origFrameElementIndexToNewFrameElementsMap[idx].Count - 1;
                }

                modifiedAnimFrame.firstElementIdx += shift;
                if (origFrameElementIndexToNewFrameElementsMap.ContainsKey(idxFrameElem))
                {
                    modifiedAnimFrame.numElements += origFrameElementIndexToNewFrameElementsMap[idxFrameElem].Count - 1;
                }

                modifiedAnimFrames.Add(modifiedAnimFrame);
            }
            BGD.animFrames.Clear();
            BGD.animFrames.AddRange(modifiedAnimFrames);
        }
    }

}