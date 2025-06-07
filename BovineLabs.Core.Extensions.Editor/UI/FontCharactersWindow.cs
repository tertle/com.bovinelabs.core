// <copyright file="FontCharactersWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.UI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.Editor.Helpers;
    using Unity.Assertions;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEditor.TextCore.Text;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.TextCore;
    using UnityEngine.TextCore.LowLevel;
    using UnityEngine.TextCore.Text;
    using UnityEngine.UIElements;
    using AtlasPopulationMode = UnityEngine.TextCore.Text.AtlasPopulationMode;
    using Object = UnityEngine.Object;

    // Based on https://forum.unity.com/threads/pick-all-chars-from-ttf.616078/
    public class FontCharactersWindow : EditorWindow
    {
        private const GlyphRenderMode GlyphRenderMode = UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA;

        private const GlyphLoadFlags GlyphLoadFlags =
            UnityEngine.TextCore.LowLevel.GlyphLoadFlags.LOAD_RENDER | UnityEngine.TextCore.LowLevel.GlyphLoadFlags.LOAD_NO_HINTING;

        private const int FontWeights = 20;
        private const int FontWeightHalf = FontWeights / 2;

        private static readonly Action<Object, GlyphRenderMode> CreateFontAssetFromSelectedObject;

        private static readonly Func<List<Glyph>, List<Glyph>, int, GlyphPackingMode, GlyphRenderMode, int, int, List<GlyphRect>, List<GlyphRect>, bool>
            TryPackGlyphsInAtlas;

        private static readonly Func<List<Glyph>, int, GlyphRenderMode, byte[], int, int, FontEngineError> RenderGlyphsToTexture;

        private static readonly Action<FontAsset, List<Glyph>> FontAssetGlyphTable;
        private static readonly Action<FontAsset, List<Character>> FontAssetCharacterTable;
        private static readonly Action<FontAsset, FontFeatureTable> FontAssetFontFeatureTable;
        private static readonly Action<FontAsset, int> FontAssetAtlasWidth;
        private static readonly Action<FontAsset, int> FontAssetAtlasHeight;
        private static readonly Action<FontAsset, int> FontAssetAtlasPadding;
        private static readonly Action<FontAsset> FontAssetSortAllTables;

        private TextField? characterSequence;
        private static Shader? shaderRefMobileBitmap;

        private readonly List<Glyph> fontGlyphTable = new();
        private readonly List<Character> fontCharacterTable = new();

        private readonly Dictionary<uint, uint> characterLookupMap = new();
        private readonly Dictionary<uint, List<uint>> glyphLookupMap = new();

        private readonly List<Glyph> glyphsToPack = new();
        private readonly List<Glyph> glyphsPacked = new();
        private readonly List<GlyphRect> freeGlyphRects = new();
        private readonly List<GlyphRect> usedGlyphRects = new();
        private readonly List<Glyph> glyphsToRender = new();
        private readonly List<uint> availableGlyphsToAdd = new();
        private readonly List<uint> missingCharacters = new();
        private readonly List<uint> excludedCharacters = new();

        static FontCharactersWindow()
        {
            var assembly = typeof(FontAssetCreatorWindow).Assembly;
            var type = assembly.GetType("UnityEditor.TextCore.Text.FontAsset_CreationMenu");

            var createFontAssetFromSelectedObjectMethod = type.GetMethod("CreateFontAssetFromSelectedObject", BindingFlags.Static | BindingFlags.NonPublic);
            CreateFontAssetFromSelectedObject =
                (Action<Object, GlyphRenderMode>)createFontAssetFromSelectedObjectMethod!.CreateDelegate(typeof(Action<Object, GlyphRenderMode>));

            var tryPackGlyphsInAtlasMethod = typeof(FontEngine).GetMethod("TryPackGlyphsInAtlas", BindingFlags.Static | BindingFlags.NonPublic);
            TryPackGlyphsInAtlas =
                (Func<List<Glyph>, List<Glyph>, int, GlyphPackingMode, GlyphRenderMode, int, int, List<GlyphRect>, List<GlyphRect>, bool>)
                tryPackGlyphsInAtlasMethod!.CreateDelegate(
                    typeof(Func<List<Glyph>, List<Glyph>, int, GlyphPackingMode, GlyphRenderMode, int, int, List<GlyphRect>, List<GlyphRect>, bool>));

            var renderGlyphsToTextureMethod = typeof(FontEngine).GetMethod("RenderGlyphsToTexture", BindingFlags.Static | BindingFlags.NonPublic, null,
                new[] { typeof(List<Glyph>), typeof(int), typeof(GlyphRenderMode), typeof(byte[]), typeof(int), typeof(int) }, null);

            RenderGlyphsToTexture =
                (Func<List<Glyph>, int, GlyphRenderMode, byte[], int, int, FontEngineError>)renderGlyphsToTextureMethod!.CreateDelegate(
                    typeof(Func<List<Glyph>, int, GlyphRenderMode, byte[], int, int, FontEngineError>));

            FontAssetGlyphTable = (Action<FontAsset, List<Glyph>>)typeof(FontAsset).GetProperty("glyphTable", BindingFlags.Instance | BindingFlags.Public)!
                .GetSetMethod(true)
                .CreateDelegate(typeof(Action<FontAsset, List<Glyph>>));

            FontAssetCharacterTable =
                (Action<FontAsset, List<Character>>)typeof(FontAsset).GetProperty("characterTable", BindingFlags.Instance | BindingFlags.Public)!
                    .GetSetMethod(true)
                    .CreateDelegate(typeof(Action<FontAsset, List<Character>>));

            FontAssetFontFeatureTable =
                (Action<FontAsset, FontFeatureTable>)typeof(FontAsset).GetProperty("fontFeatureTable", BindingFlags.Instance | BindingFlags.Public)!
                    .GetSetMethod(true)
                    .CreateDelegate(typeof(Action<FontAsset, FontFeatureTable>));

            FontAssetAtlasWidth = (Action<FontAsset, int>)typeof(FontAsset).GetProperty("atlasWidth", BindingFlags.Instance | BindingFlags.Public)!
                .GetSetMethod(true)
                .CreateDelegate(typeof(Action<FontAsset, int>));

            FontAssetAtlasHeight = (Action<FontAsset, int>)typeof(FontAsset).GetProperty("atlasHeight", BindingFlags.Instance | BindingFlags.Public)!
                .GetSetMethod(true)
                .CreateDelegate(typeof(Action<FontAsset, int>));

            FontAssetAtlasPadding = (Action<FontAsset, int>)typeof(FontAsset).GetProperty("atlasPadding", BindingFlags.Instance | BindingFlags.Public)!
                .GetSetMethod(true)
                .CreateDelegate(typeof(Action<FontAsset, int>));

            FontAssetSortAllTables =
                (Action<FontAsset>)typeof(FontAsset).GetMethod("SortAllTables", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate(
                    typeof(Action<FontAsset>));
        }

        private static Shader Shader
        {
            get
            {
                if (!shaderRefMobileBitmap)
                {
                    shaderRefMobileBitmap = Shader.Find("TextMeshPro/Mobile/Bitmap");

                    if (!shaderRefMobileBitmap)
                    {
                        shaderRefMobileBitmap = Shader.Find("Text/Bitmap");
                    }

                    if (!shaderRefMobileBitmap)
                    {
                        shaderRefMobileBitmap = Shader.Find("Hidden/Internal-GUITextureClipText");
                    }
                }

                return shaderRefMobileBitmap;
            }
        }

        private void CreateGUI()
        {
            var root = this.rootVisualElement;

            var fontField = new ObjectField("Font") { objectType = typeof(Font) };
            root.Add(fontField);

            this.characterSequence = new TextField("Character Sequence (Decimal)", -1, true, false, default);
            root.Add(this.characterSequence);

            var pickCharButton = new Button { text = "Pick All Chars Range From Font" };
            root.Add(pickCharButton);
            pickCharButton.clicked += () =>
            {
                var f = fontField.value as Font;
                this.characterSequence.value = PickAllCharsRangeFromFont(f);
            };

            root.Add(new VisualElement { style = { flexGrow = 1 } });

            var createFontButton = new Button { text = "Create Fonts from Selection" };
            root.Add(createFontButton);
            createFontButton.clicked += () =>
            {
                createFontButton.SetEnabled(false);
                this.CreateFontsFromSelection();
                createFontButton.SetEnabled(true);
            };
        }

        private static string PickAllCharsRangeFromFont(Font? font)
        {
            var chars = string.Empty;
            if (font)
            {
                TrueTypeFontImporter? fontImporter = null;

                // A GLITCH: Unity's Font.CharacterInfo doesn't work properly on dynamic mode, we need to change it to Unicode first
                if (font.dynamic)
                {
                    var assetPath = AssetDatabase.GetAssetPath(font);
                    fontImporter = AssetImporter.GetAtPath(assetPath) as TrueTypeFontImporter;

                    // Nested font, cbf handling
                    if (fontImporter == null)
                    {
                        return chars;
                    }

                    fontImporter.fontTextureCase = FontTextureCase.Unicode;
                    fontImporter.SaveAndReimport();
                }

                // Only Non-Dynamic Fonts define the characterInfo array
                var minMaxRange = new int2(-1, -1);
                for (var i = 0; i < font.characterInfo.Length; i++)
                {
                    var charInfo = font.characterInfo[i];
                    var apply = true;
                    if (minMaxRange.x < 0 || minMaxRange.y < 0)
                    {
                        apply = false;
                        minMaxRange = new int2(charInfo.index, charInfo.index);
                    }
                    else if (charInfo.index == minMaxRange.y + 1)
                    {
                        apply = false;
                        minMaxRange.y = charInfo.index;
                    }

                    if (apply || i == font.characterInfo.Length - 1)
                    {
                        if (!string.IsNullOrEmpty(chars))
                        {
                            chars += ",";
                        }

                        chars += minMaxRange.x + "-" + minMaxRange.y;
                        if (i == font.characterInfo.Length - 1)
                        {
                            if (charInfo.index >= 0 && (charInfo.index < minMaxRange.x || charInfo.index > minMaxRange.y))
                            {
                                chars += "," + charInfo.index + "-" + charInfo.index;
                            }
                        }
                        else
                        {
                            minMaxRange = new int2(charInfo.index, charInfo.index);
                        }
                    }
                }

                // Change back to dynamic font
                if (fontImporter)
                {
                    fontImporter.fontTextureCase = FontTextureCase.Dynamic;
                    fontImporter.SaveAndReimport();
                }
            }

            return chars;
        }

        private void CreateFontsFromSelection()
        {
            var selectedFonts = Selection.objects?.OfType<Font>().ToArray();
            if (selectedFonts == null || selectedFonts.Length == 0)
            {
                return;
            }

            var splitFonts = selectedFonts
                .Select(s => (s, s.name.Split('-')))
                .Where(c => c.Item2.Length == 2)
                .GroupBy(c => c.Item2[0], c => (c.s, c.Item2[1]))
                .ToArray();

            foreach (var group in splitFonts)
            {
                var dynamicFonts = CreateDynamicFonts(group);
                var staticFonts = this.CreateStaticFonts(group);

                for (var i = 0; i < dynamicFonts.Length; i++)
                {
                    if (!staticFonts[i])
                    {
                        continue;
                    }

                    var staticFont = staticFonts[i];
                    var dynamicFont = dynamicFonts[i];

                    staticFont.fallbackFontAssetTable ??= new List<FontAsset>();
                    staticFont.fallbackFontAssetTable.Add(dynamicFont);
                }
            }
        }

        private static FontAsset[] CreateDynamicFonts(IGrouping<string, (Font Font, string Type)> group)
        {
            var dynamicFonts = new FontAsset[FontWeights];

            foreach (var (font, type) in group)
            {
                var index = GetIndex(type);
                if (index < 0)
                {
                    BLGlobalLogger.LogWarningString($"Font {font} was not a valid type.");
                    continue;
                }

                dynamicFonts[index] = CreateDynamicFont(font);
            }

            UpdateRegularFontWeight(dynamicFonts);
            Move("Dynamic", dynamicFonts, true);

            return dynamicFonts;
        }

        private FontAsset[] CreateStaticFonts(IGrouping<string, (Font Font, string Type)> group)
        {
            var staticFont = new FontAsset[FontWeights];

            foreach (var (font, type) in group)
            {
                var index = GetIndex(type);
                if (index < 0)
                {
                    BLGlobalLogger.LogWarningString($"Font {font} was not a valid type.");
                    continue;
                }

                staticFont[index] = this.CreateStaticFont(font);
            }

            UpdateRegularFontWeight(staticFont);
            Move("Static", staticFont, false);

            return staticFont;
        }

        private static void UpdateRegularFontWeight(FontAsset[] dynamicFonts)
        {
            var activeFontIndex = GetIndex("regular");
            var font = dynamicFonts[activeFontIndex];
            if (!font)
            {
                return;
            }

            var fontWeightTable = font.fontWeightTable;

            for (var loopFontIndex = 0; loopFontIndex < dynamicFonts.Length; loopFontIndex++)
            {
                // We don't assign ourselves
                if (activeFontIndex == loopFontIndex)
                {
                    continue;
                }

                var index = loopFontIndex % FontWeightHalf;

                var fwt = fontWeightTable[index];
                if (loopFontIndex / FontWeightHalf == 0)
                {
                    fwt.regularTypeface = dynamicFonts[loopFontIndex];
                }
                else
                {
                    fwt.italicTypeface = dynamicFonts[loopFontIndex];
                }

                fontWeightTable[index] = fwt;
            }

            EditorUtility.SetDirty(font);
        }

        private static void Move(string type, FontAsset[] fonts, bool nameFile)
        {
            var assetPath = AssetDatabase.GetAssetPath(fonts.First(f => f));
            var directoryName = Path.GetDirectoryName(assetPath)!;
            var newDirectory = Path.Combine(directoryName, type);

            AssetDatabaseHelper.CreateDirectories(ref newDirectory);

            foreach (var asset in fonts)
            {
                if (!asset)
                {
                    continue;
                }

                var oldPath = AssetDatabase.GetAssetPath(asset);
                var withoutExtension = Path.GetFileNameWithoutExtension(oldPath);
                var moveTo = $"{newDirectory}/{(nameFile ? $"{withoutExtension}-{type}" : withoutExtension)}.asset";
                AssetDatabase.MoveAsset(oldPath, moveTo);
            }
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo", Justification = "Formating")]
        private static int GetIndex(string type)
        {
            switch (type.ToLower())
            {
                case "thin":
                    return 1;
                case "extralight":
                    return 2;
                case "light":
                    return 3;
                case "regular":
                    return 4;
                case "medium":
                    return 5;
                case "semibold":
                    return 6;
                case "bold":
                    return 7;
                case "extrabold":
                case "heavy":
                    return 8;
                case "black":
                    return 9;
                case "thinitalic":
                    return 11;
                case "extralightitalic":
                    return 12;
                case "lightitalic":
                    return 13;
                case "italic":
                    return 14;
                case "mediumitalic":
                    return 15;
                case "semibolditalic":
                    return 16;
                case "bolditalic":
                    return 17;
                case "extrabolditalic":
                case "heavyitalic":
                    return 18;
                case "blackitalic":
                    return 19;
                default:
                    return -1;
            }
        }

        private static FontAsset CreateDynamicFont(Font font)
        {
            CreateFontAssetFromSelectedObject(font, GlyphRenderMode);

            var assetPath = AssetDatabase.GetAssetPath(font);
            var directoryName = Path.GetDirectoryName(assetPath)!;
            var withoutExtension = Path.GetFileNameWithoutExtension(assetPath);

            var path = directoryName + "/" + withoutExtension + " SDF.asset";

            var fontAsset = AssetDatabase.LoadAssetAtPath<FontAsset>(path);

            var so = new SerializedObject(fontAsset);
            so.FindProperty("m_IsMultiAtlasTexturesEnabled").boolValue = true;
            so.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
            fontAsset.ClearFontAssetData();
            so.ApplyModifiedPropertiesWithoutUndo();

            return fontAsset;
        }

        private FontAsset CreateStaticFont(Font font)
        {
            CreateFontAssetFromSelectedObject(font, GlyphRenderMode);

            var assetPath = AssetDatabase.GetAssetPath(font);
            var directoryName = Path.GetDirectoryName(assetPath)!;
            var withoutExtension = Path.GetFileNameWithoutExtension(assetPath);

            var path = directoryName + "/" + withoutExtension + " SDF.asset";

            var fontAsset = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

            const int pointSize = 72;
            const int padding = 8;
            const int atlasWidth = 1024;
            const int atlasHeight = 1024;

            var atlasTextureBuffer = new byte[atlasWidth * atlasHeight];

            FontEngine.InitializeFontEngine();

            var faceInfo = this.PackAndRender(atlasWidth, atlasHeight, pointSize, padding, atlasTextureBuffer);

            var fontAtlasTexture = this.CreateFontAtlasTexture(atlasWidth, atlasHeight, atlasTextureBuffer);
            fontAtlasTexture.name = withoutExtension + " Atlas";

            fontAsset.faceInfo = faceInfo;
            FontAssetGlyphTable(fontAsset, this.fontGlyphTable);
            FontAssetCharacterTable(fontAsset, this.fontCharacterTable);
            FontAssetSortAllTables(fontAsset);

            FontAssetFontFeatureTable(fontAsset, this.GetAllFontFeatures(faceInfo));

            for (var index = 1; index < fontAsset.atlasTextures.Length; ++index)
            {
                DestroyImmediate(fontAsset.atlasTextures[index], true);
            }

            typeof(FontAsset).GetField("m_AtlasTextureIndex", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(fontAsset, 0);
            FontAssetAtlasWidth(fontAsset, atlasWidth);
            FontAssetAtlasHeight(fontAsset, atlasHeight);
            FontAssetAtlasPadding(fontAsset, padding);

            var atlasTexture = fontAsset.atlasTextures[0];

            var setAtlasTextureIsReadable =
                (Action<Texture2D, bool>)typeof(FontAsset).GetField("SetAtlasTextureIsReadable", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

            if (!atlasTexture.isReadable)
            {
                setAtlasTextureIsReadable(atlasTexture, true);
            }

            atlasTexture.Reinitialize(atlasWidth, atlasHeight);
            atlasTexture.Apply(false);

            Graphics.CopyTexture(fontAtlasTexture, atlasTexture);
            atlasTexture.Apply(false);

            fontAtlasTexture.hideFlags = HideFlags.None;

            setAtlasTextureIsReadable(fontAsset.atlasTexture, false);

            typeof(FontAsset).GetProperty("freeGlyphRects", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(fontAsset, this.freeGlyphRects);
            typeof(FontAsset).GetProperty("usedGlyphRects", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(fontAsset, this.usedGlyphRects);
            fontAsset.fontAssetCreationEditorSettings = this.SaveFontCreationSettings(fontAsset, font, atlasWidth, atlasHeight, pointSize, padding);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(fontAsset));
            fontAsset.ReadFontAssetDefinition();
            AssetDatabase.Refresh();
            TextEventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);

            return fontAsset;
        }

        private FontAssetCreationEditorSettings SaveFontCreationSettings(
            FontAsset fontAsset, Font font, int atlasWidth, int atlasHeight, int pointSize, int padding)
        {
            return new FontAssetCreationEditorSettings
            {
                sourceFontFileGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(font)),
                faceIndex = 0,
                pointSizeSamplingMode = 1,
                pointSize = pointSize,
                padding = padding,
                paddingMode = 2,
                packingMode = 4,
                atlasWidth = atlasWidth,
                atlasHeight = atlasHeight,
                characterSetSelectionMode = 5,
                characterSequence = this.characterSequence!.value,
                referencedFontAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(fontAsset)),
                referencedTextAssetGUID = string.Empty,
                renderMode = (int)GlyphRenderMode,
                includeFontFeatures = true,
            };
        }

        private FaceInfo PackAndRender(int atlasWidth, int atlasHeight, int pointSize, int padding, byte[] atlasTextureBuffer)
        {
            Assert.AreEqual(atlasWidth * atlasHeight, atlasTextureBuffer.Length);

            var characterSet = ParseNumberSequence(this.characterSequence!.value);

            // Clear the various lists used in the generation process.
            this.availableGlyphsToAdd.Clear();
            this.missingCharacters.Clear();
            this.excludedCharacters.Clear();
            this.characterLookupMap.Clear();
            this.glyphLookupMap.Clear();
            this.glyphsToPack.Clear();
            this.glyphsPacked.Clear();

            // Check if requested characters are available in the source font file.
            foreach (var unicode in characterSet)
            {
                if (FontEngine.TryGetGlyphIndex(unicode, out var glyphIndex))
                {
                    // Skip over potential duplicate characters.
                    if (!this.characterLookupMap.TryAdd(unicode, glyphIndex))
                    {
                        continue;
                    }

                    // Add character to character lookup map.

                    // Skip over potential duplicate glyph references.
                    if (this.glyphLookupMap.TryGetValue(glyphIndex, out var value))
                    {
                        // Add additional glyph reference for this character.
                        value.Add(unicode);
                        continue;
                    }

                    // Add glyph reference to glyph lookup map.
                    this.glyphLookupMap.Add(glyphIndex, new List<uint> { unicode });

                    // Add glyph index to list of glyphs to add to texture.
                    this.availableGlyphsToAdd.Add(glyphIndex);
                }
                else
                {
                    // Add Unicode to list of missing characters.
                    this.missingCharacters.Add(unicode);
                }
            }

            // Pack available glyphs in the provided texture space.
            if (this.availableGlyphsToAdd.Count > 0)
            {
                var packingModifier = 1;

                // Set point size
                FontEngine.SetFaceSize(pointSize);

                this.glyphsToPack.Clear();
                this.glyphsPacked.Clear();

                this.freeGlyphRects.Clear();
                this.freeGlyphRects.Add(new GlyphRect(0, 0, atlasWidth - packingModifier, atlasHeight - packingModifier));
                this.usedGlyphRects.Clear();

                foreach (var glyphIndex in this.availableGlyphsToAdd)
                {
                    if (FontEngine.TryGetGlyphWithIndexValue(glyphIndex, GlyphLoadFlags, out var glyph))
                    {
                        if (glyph.glyphRect is { width: > 0, height: > 0 })
                        {
                            this.glyphsToPack.Add(glyph);
                        }
                        else
                        {
                            this.glyphsPacked.Add(glyph);
                        }
                    }
                }

                TryPackGlyphsInAtlas(this.glyphsToPack, this.glyphsPacked, padding, GlyphPackingMode.ContactPointRule, GlyphRenderMode, atlasWidth, atlasHeight,
                    this.freeGlyphRects, this.usedGlyphRects);
            }
            else
            {
                var packingModifier = 1;

                FontEngine.SetFaceSize(pointSize);

                this.glyphsToPack.Clear();
                this.glyphsPacked.Clear();

                this.freeGlyphRects.Clear();
                this.freeGlyphRects.Add(new GlyphRect(0, 0, atlasWidth - packingModifier, atlasHeight - packingModifier));
                this.usedGlyphRects.Clear();
            }

            this.fontCharacterTable.Clear();
            this.fontGlyphTable.Clear();
            this.glyphsToRender.Clear();

            // Add glyphs and characters successfully added to texture to their respective font tables.
            foreach (var glyph in this.glyphsPacked)
            {
                var glyphIndex = glyph.index;

                this.fontGlyphTable.Add(glyph);

                // Add glyphs to list of glyphs that need to be rendered.
                if (glyph.glyphRect is { width: > 0, height: > 0 })
                {
                    this.glyphsToRender.Add(glyph);
                }

                foreach (var unicode in this.glyphLookupMap[glyphIndex])
                {
                    // Create new Character
                    this.fontCharacterTable.Add(new Character(unicode, glyph));
                }
            }

            foreach (var glyph in this.glyphsToPack)
            {
                foreach (var unicode in this.glyphLookupMap[glyph.index])
                {
                    this.excludedCharacters.Add(unicode);
                }
            }

            var faceInfo = FontEngine.GetFaceInfo();

            // Render and add glyphs to the given atlas texture.
            if (this.glyphsToRender.Count > 0)
            {
                RenderGlyphsToTexture(this.glyphsToRender, padding, GlyphRenderMode, atlasTextureBuffer, atlasWidth, atlasHeight);
            }

            return faceInfo;
        }

        private Texture2D CreateFontAtlasTexture(int atlasWidth, int atlasHeight, byte[] atlasTextureBuffer)
        {
            var colors = new Color32[atlasWidth * atlasHeight];
            var fontAtlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.Alpha8, false, true);
            for (var index = 0; index < colors.Length; ++index)
            {
                var num = atlasTextureBuffer[index];
                colors[index] = new Color32(num, num, num, num);
            }

            fontAtlasTexture.SetPixels32(colors, 0);
            fontAtlasTexture.Apply(false, false);

            return fontAtlasTexture;
        }

        private static uint[] ParseNumberSequence(string sequence)
        {
            var unicodeList = new List<uint>();
            string[] sequences = sequence.Split(',');

            foreach (var seq in sequences)
            {
                string[] s1 = seq.Split('-');

                if (s1.Length == 1)
                {
                    try
                    {
                        unicodeList.Add(uint.Parse(s1[0]));
                    }
                    catch
                    {
                        BLGlobalLogger.LogInfoString("No characters selected or invalid format.");
                    }
                }
                else
                {
                    for (var j = uint.Parse(s1[0]); j < uint.Parse(s1[1]) + 1; j++)
                    {
                        unicodeList.Add(j);
                    }
                }
            }

            return unicodeList.ToArray();
        }

        private FontFeatureTable GetAllFontFeatures(FaceInfo faceInfo)
        {
            var window = CreateInstance<FontAssetCreatorWindow>();

            typeof(FontAssetCreatorWindow).GetField("m_FaceInfo", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, faceInfo);
            typeof(FontAssetCreatorWindow).GetField("m_AvailableGlyphsToAdd", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window,
                this.availableGlyphsToAdd);

            var fft =
                (FontFeatureTable)typeof(FontAssetCreatorWindow).GetMethod("GetAllFontFeatures", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window,
                    null);

            DestroyImmediate(window);

            return fft;
        }

        [MenuItem(EditorMenus.RootMenuTools + "Font Characters", false)]
        private static void Init()
        {
            var window = (FontCharactersWindow)GetWindow(typeof(FontCharactersWindow));
            window.titleContent = new GUIContent("Font Characters");
            window.ShowUtility();
        }
    }
}
