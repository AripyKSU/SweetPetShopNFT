using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PetShopAssetImportSetup
{
    private const string Root = "Assets/Art/PetShop";
    private const string AutoApplySessionKey = "PetShopAssetImportSetup.Applied";

    private readonly struct ImportRule
    {
        public readonly string Path;
        public readonly int MaxSize;
        public readonly TextureImporterCompression Compression;
        public readonly Vector4 Border;

        public ImportRule(
            string path,
            int maxSize,
            TextureImporterCompression compression,
            Vector4 border = default)
        {
            Path = path;
            MaxSize = maxSize;
            Compression = compression;
            Border = border;
        }
    }

    private static readonly ImportRule[] Rules =
    {
        new($"{Root}/Backgrounds/bg_petshop_main.png", 2048, TextureImporterCompression.CompressedHQ),
        new($"{Root}/Environment/fence_auction.png", 2048, TextureImporterCompression.CompressedHQ),
        new($"{Root}/Environment/rug_inventory.png", 2048, TextureImporterCompression.CompressedHQ),
        new($"{Root}/Pets/Common/pet_pomeranian.png", 1024, TextureImporterCompression.CompressedHQ),
        new($"{Root}/Pets/Common/pet_korean_shorthair.png", 1024, TextureImporterCompression.CompressedHQ),
        new($"{Root}/Pets/Common/pet_lop_rabbit.png", 1024, TextureImporterCompression.CompressedHQ),
        new($"{Root}/Pets/Special/pet_golden_retriever.png", 1024, TextureImporterCompression.CompressedHQ),
        new($"{Root}/Pets/NFT/pet_fennec_nft.png", 1024, TextureImporterCompression.CompressedHQ),
        new($"{Root}/Props/cushion_special.png", 1024, TextureImporterCompression.CompressedHQ),
        new($"{Root}/Props/cushion_nft.png", 1024, TextureImporterCompression.CompressedHQ),
        new($"{Root}/Props/prop_price_sign.png", 512, TextureImporterCompression.Uncompressed, new Vector4(64, 64, 64, 64)),
    };

    [InitializeOnLoadMethod]
    private static void ScheduleAutomaticApply()
    {
        if (SessionState.GetBool(AutoApplySessionKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoApplySessionKey, true);
        EditorApplication.delayCall += ApplyImportSettings;
    }

    [MenuItem("Tools/Pet Shop/Apply Asset Import Settings")]
    public static void ApplyImportSettings()
    {
        var missing = new List<string>();

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (ImportRule rule in Rules)
            {
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(rule.Path) == null)
                {
                    missing.Add(rule.Path);
                    continue;
                }

                if (AssetImporter.GetAtPath(rule.Path) is not TextureImporter importer)
                {
                    missing.Add(rule.Path);
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.spriteBorder = rule.Border;
                importer.sRGBTexture = true;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = rule.MaxSize;
                importer.textureCompression = rule.Compression;
                importer.SaveAndReimport();
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Pet Shop import setup could not find:\n" + string.Join("\n", missing));
        }

        Debug.Log($"Pet Shop import settings applied to {Rules.Length} assets.");
    }

    [MenuItem("Tools/Pet Shop/Validate Asset Import Settings")]
    public static void ValidateImportSettings()
    {
        var errors = new List<string>();

        foreach (ImportRule rule in Rules)
        {
            if (AssetImporter.GetAtPath(rule.Path) is not TextureImporter importer)
            {
                errors.Add($"Missing TextureImporter: {rule.Path}");
                continue;
            }

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.filterMode != FilterMode.Bilinear ||
                importer.wrapMode != TextureWrapMode.Clamp ||
                importer.mipmapEnabled ||
                !importer.alphaIsTransparency ||
                importer.maxTextureSize != rule.MaxSize ||
                importer.textureCompression != rule.Compression ||
                importer.spriteBorder != rule.Border)
            {
                errors.Add($"Importer settings differ: {rule.Path}");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join("\n", errors));
        }

        Debug.Log($"Pet Shop import settings validated for {Rules.Length} assets.");
    }
}
