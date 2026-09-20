using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps the first three grass slots in the environment tile sheet synced with
/// the artist-owned 16x16 grass_base_1/2/3 PNG files. This only updates pixels
/// in the packed texture; it never changes scenes, objects, or transforms.
/// </summary>
public sealed class GrassBaseAutoSync : AssetPostprocessor
{
    const string EnvironmentFolder = "Assets/Art/Environment";
    const string TileSheetPath = EnvironmentFolder + "/ground_tiles_16px.png";
    const int TileSize = 16;

    static readonly string[] SourcePaths =
    {
        EnvironmentFolder + "/grass_base_1.png",
        EnvironmentFolder + "/grass_base_2.png",
        EnvironmentFolder + "/grass_base_3.png"
    };

    static bool syncQueued;

    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (syncQueued || !importedAssets.Any(SourcePaths.Contains))
            return;

        syncQueued = true;
        EditorApplication.delayCall += SyncGrassTiles;
    }

    static void SyncGrassTiles()
    {
        try
        {
            if (!File.Exists(TileSheetPath))
            {
                Debug.LogError($"Grass auto-sync could not find {TileSheetPath}.");
                return;
            }

            var sheet = LoadPng(TileSheetPath);
            if (sheet.width < TileSize * SourcePaths.Length || sheet.height < TileSize)
            {
                Debug.LogError($"Grass auto-sync expected a sheet at least 48x16, but found {sheet.width}x{sheet.height}.");
                UnityEngine.Object.DestroyImmediate(sheet);
                return;
            }

            int destinationY = sheet.height - TileSize;
            for (int index = 0; index < SourcePaths.Length; index++)
            {
                if (!File.Exists(SourcePaths[index]))
                {
                    Debug.LogError($"Grass auto-sync could not find {SourcePaths[index]}.");
                    UnityEngine.Object.DestroyImmediate(sheet);
                    return;
                }

                var tile = LoadPng(SourcePaths[index]);
                if (tile.width != TileSize || tile.height != TileSize)
                {
                    Debug.LogError($"Grass auto-sync requires 16x16 PNGs. {SourcePaths[index]} is {tile.width}x{tile.height}.");
                    UnityEngine.Object.DestroyImmediate(tile);
                    UnityEngine.Object.DestroyImmediate(sheet);
                    return;
                }

                sheet.SetPixels(index * TileSize, destinationY, TileSize, TileSize, tile.GetPixels());
                UnityEngine.Object.DestroyImmediate(tile);
            }

            sheet.Apply(false, false);
            File.WriteAllBytes(TileSheetPath, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
            AssetDatabase.ImportAsset(TileSheetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("GRASS_BASE_AUTO_SYNC_SUCCESS");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            syncQueued = false;
        }
    }

    static Texture2D LoadPng(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(path), false))
        {
            UnityEngine.Object.DestroyImmediate(texture);
            throw new InvalidDataException($"Could not decode PNG: {path}");
        }

        return texture;
    }
}
