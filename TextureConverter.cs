using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TextureConverter : EditorWindow
{
    public string texturesFolder = "Assets/Textures";

    private static EditorWindow _editorWindow;
    private Vector2 _scrollPos;

#if UNITY_EDITOR
    [MenuItem("Tools/Texture Converter/Texture Converter")]
    public static void ShowFolderPanel()
    {
        OpenFolderPanel();
    }

    [MenuItem("Tools/Texture Converter/Texture Converter (Window)")]
    public static void ShowWindow()
    {
        _editorWindow = GetWindow(typeof(TextureConverter), false);
    }

    [MenuItem("Assets/Texture Converter/Texture Converter")]
    public static void ShowFolderPanelRightClick()
    {
        OpenFolderPanel();
    }

    [MenuItem("Assets/Texture Converter/Texture Converter (Window)")]
    public static void ShowWindowRightClick()
    {
        _editorWindow = GetWindow(typeof(TextureConverter), false);
    }

    private static void OpenFolderPanel()
    {
        string path = UnityEditor.EditorUtility.OpenFolderPanel("Select Folder", "", "");
        if (path.Length != 0)
        {
            bool confirmedDialog = UnityEditor.EditorUtility.DisplayDialog(
                title: "Convert Textures to JPG and PNG",
                message: "This will created duplicated JPG and PNG textures in the same folder. Are you sure you want to continue?",
                ok: "Apply",
                cancel: "Cancel"
            );

            if (confirmedDialog)
            {
                ConvertTextures(RemoveBefore(path, "Assets"));
            }
        }
    }

    private static string RemoveBefore(string value, string character)
    {
        int index = value.IndexOf(character);
        if (index > 0)
        {
            value = value.Substring(index);
        }
        return value;
    }

    private void OnInspectorUpdate()
    {
        if (!_editorWindow)
            _editorWindow = GetWindow(typeof(TextureConverter), false);
    }

    private void OnGUI()
    {
        if (_editorWindow)
        {
            GUILayout.BeginArea(new Rect(0, 0, _editorWindow.position.size.x, _editorWindow.position.size.y));
            GUILayout.BeginVertical();
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, true, GUILayout.ExpandHeight(true));
        }

        // Textures Folder
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        texturesFolder = EditorGUILayout.TextField("Textures Folder", texturesFolder);
        GUILayout.Space(10f);
        GUILayout.EndVertical();

        // Button
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        if (GUILayout.Button("Generate PBR Textures"))
        {
            ConvertTextures(texturesFolder);
        }
        GUILayout.Space(10f);
        GUILayout.EndVertical();

        GUILayout.Space(100);
        if (_editorWindow)
        {
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    private static void ConvertTextures(string texturesFolder)
    {
        EditorUtility.DisplayProgressBar("Packing Textures, please wait...", "", 1f);
        try
        {
            // "keyword l: <label> t: <type>"
            // https://docs.unity3d.com/ScriptReference/AssetDatabase.FindAssets.html
            string[] guids = AssetDatabase.FindAssets("_ t: texture2D", new[] { texturesFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
                if (tex == null)
                {
                    continue;
                }

                string textureName = tex.name;
                string folder = Path.GetDirectoryName(path) + "/";
                TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(path);
                bool hasAlpha = textureImporter.DoesSourceTextureHaveAlpha();
                
                // Set Texture Importer before copying it. (To have the same result)
                // TextureType (Default)
                TextureImporterType textureType = textureImporter.textureType;
                textureImporter.textureType = TextureImporterType.Default;
                // sRGB (true)
                bool sRGB = textureImporter.sRGBTexture;
                textureImporter.sRGBTexture = true;
                // MaxTextureSize (4096)
                string platform = EditorUserBuildSettings.activeBuildTarget.ToString();
                int maxTextureSize = 4096;
                bool hasPlatformSettings = textureImporter.GetPlatformTextureSettings(platform, out maxTextureSize, out TextureImporterFormat textureFormat, out int compressionQuality, out bool etc1AlphaSplitEnabled);
                if (hasPlatformSettings)
                {
                    textureImporter.SetPlatformTextureSettings(
                        new TextureImporterPlatformSettings() {
                            name = platform,
                            overridden = true,
                            maxTextureSize = 4096,
                            format = TextureImporterFormat.RGBA32,
                            compressionQuality = 100
                        });
                }
                else
                {
                    maxTextureSize = textureImporter.maxTextureSize;
                    textureImporter.maxTextureSize = 4096;
                }
                EditorUtility.SetDirty(textureImporter);
                textureImporter.SaveAndReimport();

                // Duplicate texture
                Texture2D texture = TexturePackerUtilities.DuplicateTexture(tex);

                SaveTexture(texture, folder, textureName, hasAlpha, textureImporter);

                // Set back to original settings
                textureImporter.textureType = textureType;
                textureImporter.sRGBTexture = sRGB;
                if (hasPlatformSettings)
                {
                    textureImporter.SetPlatformTextureSettings(
                        new TextureImporterPlatformSettings() {
                            name = platform,
                            overridden = true,
                            maxTextureSize = maxTextureSize,
                            format = textureFormat,
                            compressionQuality = 100
                        });
                }
                else
                {
                    textureImporter.maxTextureSize = maxTextureSize;
                }
                EditorUtility.SetDirty(textureImporter);
                textureImporter.SaveAndReimport();
            }
        }
        catch (Exception e)
        {
            Debug.Log($"TextureConverter Exception: {e}");
        }

        EditorUtility.ClearProgressBar();
    }

    private static void SaveTexture(Texture2D texture, string folder, string textureName, bool hasAlpha = false, TextureImporter originalTextureImporter = null)
    {
        byte[] bytes;
        string extension;
        if (hasAlpha)
        {
            bytes = ImageConversion.EncodeToPNG(texture);
            extension = "png";
        }
        else
        {
            bytes = ImageConversion.EncodeToJPG(texture);
            extension = "jpg";
        }

        string path = $"{folder}/{textureName}.{extension}";
        if (bytes != null)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            File.WriteAllBytes(path, bytes);
        }

        if(originalTextureImporter != null)
        {
            // TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(path);

            // textureImporter.SaveAndReimport();
        }

        AssetDatabase.Refresh();
    }
#endif
}
