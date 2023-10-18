using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TexturePackerSimple : EditorWindow
{
    private const string PACKER_SHADER_NAME = "Hidden/TexturePackerSimple";
    private static Material _blitMaterial;

    public enum Channel
    {
        R = 0,
        G = 1,
        B = 2,
        A = 3,
    }

    public enum Format
    {
        JPG,
        PNG,
    }

    public Texture2D redTexture = null;
    public Channel redTextureChannel = Channel.R;
    public float redDefault = 1f;
    public bool redInvert = false;

    public Texture2D greenTexture = null;
    public Channel greenTextureChannel = Channel.G;
    public float greenDefault = 1f;
    public bool greenInvert = false;

    public Texture2D blueTexture = null;
    public Channel blueTextureChannel = Channel.B;
    public float blueDefault = 1f;
    public bool blueInvert = false;

    public Format format = Format.JPG;

    private static EditorWindow _editorWindow;
    private Vector2 _scrollPos;

#if UNITY_EDITOR
    [MenuItem("Tools/Texture Packer/Texture Packer Simple")]
    public static void ShowWindow()
    {
        _editorWindow = GetWindow(typeof(TexturePackerSimple), false);
    }

    [MenuItem("Assets/Texture Packer/Texture Packer Simple")]
    public static void ShowWindowRightClick()
    {
        _editorWindow = GetWindow(typeof(TexturePackerSimple), false);
    }

    private void OnInspectorUpdate()
    {
        if (!_editorWindow)
            _editorWindow = GetWindow(typeof(TexturePackerSimple), false);
    }

    private void OnGUI()
    {
        if (_editorWindow)
        {
            GUILayout.BeginArea(new Rect(0, 0, _editorWindow.position.size.x, _editorWindow.position.size.y));
            GUILayout.BeginVertical();
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, true, GUILayout.ExpandHeight(true));
        }

        GUILayout.BeginVertical(EditorStyles.helpBox);
        // Red Texture
        GUILayout.BeginVertical(EditorStyles.helpBox);
        redTexture = (Texture2D)EditorGUILayout.ObjectField("Red", redTexture, typeof(Texture2D), false);
        if (!redTexture)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("No image found, use slider to set value");
            redDefault = EditorGUILayout.Slider(redDefault, 0f, 1f);
            GUILayout.EndHorizontal();
        }
        else
        {
            redTextureChannel = (Channel)EditorGUILayout.EnumPopup("Channel:", redTextureChannel);
            redInvert = EditorGUILayout.Toggle("Invert", redInvert);
        }
        GUILayout.EndVertical();

        // Green Texture
        GUILayout.BeginVertical(EditorStyles.helpBox);
        greenTexture = (Texture2D)EditorGUILayout.ObjectField("Green", greenTexture, typeof(Texture2D), false);
        if (!greenTexture)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("No image found, use slider to set value");
            greenDefault = EditorGUILayout.Slider(greenDefault, 0f, 1f);
            GUILayout.EndHorizontal();
        }
        else
        {
            greenTextureChannel = (Channel)EditorGUILayout.EnumPopup("Channel:", greenTextureChannel);
            greenInvert = EditorGUILayout.Toggle("Invert", greenInvert);
        }
        GUILayout.EndVertical();

        // Blue Texture
        GUILayout.BeginVertical(EditorStyles.helpBox);
        blueTexture = (Texture2D)EditorGUILayout.ObjectField("Blue", blueTexture, typeof(Texture2D), false);
        if (!blueTexture)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("No image found, use slider to set value");
            blueDefault = EditorGUILayout.Slider(blueDefault, 0f, 1f);
            GUILayout.EndHorizontal();
        }
        else
        {
            blueTextureChannel = (Channel)EditorGUILayout.EnumPopup("Channel:", blueTextureChannel);
            blueInvert = EditorGUILayout.Toggle("Invert", blueInvert);
        }
        GUILayout.EndVertical();
        GUILayout.EndVertical();

        GUILayout.Space(10f);

        // Export Settings
        GUILayout.BeginVertical(EditorStyles.helpBox);
        format = (Format)EditorGUILayout.EnumPopup("Format:", format);
        GUILayout.EndVertical();

        GUILayout.Space(10f);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        if (GUILayout.Button("Generate Texture"))
        {
            EditorUtility.DisplayProgressBar("Packing Textures, please wait...", "", 1f);
            try
            {
                PackTexture(
                    redTexture, greenTexture, blueTexture, 
                    redTextureChannel, greenTextureChannel, blueTextureChannel, 
                    redDefault, greenDefault, blueDefault,
                    redInvert, greenInvert, blueInvert);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
            EditorUtility.ClearProgressBar();
        }
        GUILayout.EndVertical();

        GUILayout.Space(100);
        if (_editorWindow)
        {
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    private void PackTexture(
        Texture2D redTex, Texture2D greenTex, Texture2D blueTex,
        Channel redChannel, Channel greenChannel, Channel blueChannel,
        float redDefault, float greenDefault, float blueDefault,
        bool redInvert, bool greenInvert, bool blueInvert)
    {
        int width = 1024;
        int height = 1024;
        bool isJPG = format == Format.JPG;
        string extension = isJPG ? "jpg" : "png";
        string path = "";
        if(redTex != null)
        {
            width = redTex.width;
            height = redTex.height;
            path = AssetDatabase.GetAssetPath(redTex);
        }
        else if(greenTex != null)
        {
            width = greenTex.width;
            height = greenTex.height;
            path = AssetDatabase.GetAssetPath(greenTex);
        }
        else if(blueTex != null)
        {
            width = blueTex.width;
            height = blueTex.height;
            path = AssetDatabase.GetAssetPath(blueTex);
        }
        string folderName = Path.GetDirectoryName(path) + "/";
        string fileName = Path.GetFileNameWithoutExtension(path);
        string savePath = folderName + fileName + "_converted." + extension;
        // string textureName = tex.name;
        // string folder = Path.GetDirectoryName(path) + "/";


        ///------------------------------------------------------------------
        /// Set Texture Importer before copying it. (To have the same result)
        ///------------------------------------------------------------------
        string platform = EditorUserBuildSettings.activeBuildTarget.ToString();
        TextureImporter textureImporter_R = null;
        TextureImporter textureImporter_G = null;
        TextureImporter textureImporter_B = null;
        TextureImporterType textureType_R = TextureImporterType.Default;
        TextureImporterType textureType_G = TextureImporterType.Default;
        TextureImporterType textureType_B = TextureImporterType.Default;
        bool sRGB_R = true;
        bool sRGB_G = true;
        bool sRGB_B = true;
        int maxTextureSize_R = 4096;
        int maxTextureSize_G = 4096;
        int maxTextureSize_B = 4096;
        bool hasPlatformSettings_R = false;
        bool hasPlatformSettings_G = false;
        bool hasPlatformSettings_B = false;
        TextureImporterFormat textureFormat_R = TextureImporterFormat.RGBA32;
        TextureImporterFormat textureFormat_G = TextureImporterFormat.RGBA32;
        TextureImporterFormat textureFormat_B = TextureImporterFormat.RGBA32;

        // RED TEXTURE
        if(redTexture != null)
        {
            // Get Texture Importer
            string path_R = AssetDatabase.GetAssetPath(redTexture);
            textureImporter_R = (TextureImporter)TextureImporter.GetAtPath(path_R);
            // TextureType
            textureType_R = textureImporter_R.textureType;
            textureImporter_R.textureType = TextureImporterType.Default;
            // sRGB
            sRGB_R = textureImporter_R.sRGBTexture;
            textureImporter_R.sRGBTexture = true;
            // Get MaxTextureSize
            hasPlatformSettings_R = textureImporter_R.GetPlatformTextureSettings(platform, out maxTextureSize_R, out textureFormat_R, out int _, out bool _);
            if (hasPlatformSettings_R)
            {
                textureImporter_R.SetPlatformTextureSettings(
                    new TextureImporterPlatformSettings()
                    {
                        name = platform,
                        overridden = true,
                        maxTextureSize = 4096,
                        format = TextureImporterFormat.RGBA32,
                        compressionQuality = 100
                    });
            }
            else
            {
                maxTextureSize_R = textureImporter_R.maxTextureSize;
                textureImporter_R.maxTextureSize = 4096;
            }
            // Needs this to make sure the texture setting is reimported
            EditorUtility.SetDirty(textureImporter_R);
            textureImporter_R.SaveAndReimport();
        }

        // GREEN TEXTURE
        if(greenTexture != null)
        {
            // Get Texture Importer
            string path_G = AssetDatabase.GetAssetPath(greenTexture);
            textureImporter_G = (TextureImporter)TextureImporter.GetAtPath(path_G);
            // TextureType
            textureType_G = textureImporter_G.textureType;
            textureImporter_G.textureType = TextureImporterType.Default;
            // sRGB
            sRGB_G = textureImporter_G.sRGBTexture;
            textureImporter_G.sRGBTexture = true;
            // Get MaxTextureSize
            hasPlatformSettings_G = textureImporter_G.GetPlatformTextureSettings(platform, out maxTextureSize_G, out textureFormat_G, out int _, out bool _);
            if (hasPlatformSettings_G)
            {
                textureImporter_G.SetPlatformTextureSettings(
                    new TextureImporterPlatformSettings()
                    {
                        name = platform,
                        overridden = true,
                        maxTextureSize = 4096,
                        format = TextureImporterFormat.RGBA32,
                        compressionQuality = 100
                    });
            }
            else
            {
                maxTextureSize_G = textureImporter_G.maxTextureSize;
                textureImporter_G.maxTextureSize = 4096;
            }
            // Needs this to make sure the texture setting is reimported
            EditorUtility.SetDirty(textureImporter_G);
            textureImporter_G.SaveAndReimport();
        }

        // BLUE TEXTURE
        if(blueTexture != null)
        {
            // Get Texture Importer
            string path_B = AssetDatabase.GetAssetPath(blueTexture);
            textureImporter_B = (TextureImporter)TextureImporter.GetAtPath(path_B);
            // TextureType
            textureType_B = textureImporter_B.textureType;
            textureImporter_B.textureType = TextureImporterType.Default;
            // sRGB
            sRGB_B = textureImporter_B.sRGBTexture;
            textureImporter_B.sRGBTexture = true;
            // Get MaxTextureSize
            hasPlatformSettings_B = textureImporter_B.GetPlatformTextureSettings(platform, out maxTextureSize_B, out textureFormat_B, out int _, out bool _);
            if (hasPlatformSettings_B)
            {
                textureImporter_B.SetPlatformTextureSettings(
                    new TextureImporterPlatformSettings()
                    {
                        name = platform,
                        overridden = true,
                        maxTextureSize = 4096,
                        format = TextureImporterFormat.RGBA32,
                        compressionQuality = 100
                    });
            }
            else
            {
                maxTextureSize_B = textureImporter_B.maxTextureSize;
                textureImporter_B.maxTextureSize = 4096;
            }
            // Needs this to make sure the texture setting is reimported
            EditorUtility.SetDirty(textureImporter_B);
            textureImporter_B.SaveAndReimport();
        }


        ///------
        /// Blit
        ///------
        Texture2D tex = GenerateTexture(
            redTexture, greenTexture, blueTexture, 
            redTextureChannel, greenTextureChannel, blueTextureChannel, 
            redDefault, greenDefault, blueDefault, 
            redInvert, greenInvert, blueInvert,
            width, height);
        SaveTexture(tex, savePath, isJPG);


        ///------------------------------
        /// Set back to original settings
        ///------------------------------
        // RED TEXTURE
        if(redTexture != null)
        {
            textureImporter_R.textureType = textureType_R;
            textureImporter_R.sRGBTexture = sRGB_R;
            if (hasPlatformSettings_R)
            {
                textureImporter_R.SetPlatformTextureSettings(
                    new TextureImporterPlatformSettings()
                    {
                        name = platform,
                        overridden = true,
                        maxTextureSize = maxTextureSize_R,
                        format = textureFormat_R,
                        compressionQuality = 100
                    });
            }
            else
            {
                textureImporter_R.maxTextureSize = maxTextureSize_R;
            }
            EditorUtility.SetDirty(textureImporter_R);
            textureImporter_R.SaveAndReimport();
        }

        // GREEN TEXTURE
        if(greenTexture != null)
        {
            textureImporter_G.textureType = textureType_G;
            textureImporter_G.sRGBTexture = sRGB_G;
            if (hasPlatformSettings_G)
            {
                textureImporter_G.SetPlatformTextureSettings(
                    new TextureImporterPlatformSettings()
                    {
                        name = platform,
                        overridden = true,
                        maxTextureSize = maxTextureSize_G,
                        format = textureFormat_G,
                        compressionQuality = 100
                    });
            }
            else
            {
                textureImporter_G.maxTextureSize = maxTextureSize_G;
            }
            EditorUtility.SetDirty(textureImporter_G);
            textureImporter_G.SaveAndReimport();
        }

        // BLUE TEXTURE
        if(blueTexture != null)
        {
            textureImporter_B.textureType = textureType_B;
            textureImporter_B.sRGBTexture = sRGB_B;
            if (hasPlatformSettings_B)
            {
                textureImporter_B.SetPlatformTextureSettings(
                    new TextureImporterPlatformSettings()
                    {
                        name = platform,
                        overridden = true,
                        maxTextureSize = maxTextureSize_B,
                        format = textureFormat_B,
                        compressionQuality = 100
                    });
            }
            else
            {
                textureImporter_B.maxTextureSize = maxTextureSize_B;
            }
            EditorUtility.SetDirty(textureImporter_B);
            textureImporter_B.SaveAndReimport();
        }
    }

    private Texture2D GenerateTexture(
        Texture2D redTex, Texture2D greenTex, Texture2D blueTex,
        Channel redChannel, Channel greenChannel, Channel blueChannel,
        float redDefault, float greenDefault, float blueDefault,
        bool redInvert, bool greenInvert, bool blueInvert,
        int width, int height)
    {
        // RenderTexture blitRenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture blitRenderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        blitRenderTexture.useMipMap = false;
        blitRenderTexture.filterMode = FilterMode.Bilinear;

        Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, mipCount: 1, linear: true);

        if (_blitMaterial == null)
        {
            _blitMaterial = new Material(Shader.Find(PACKER_SHADER_NAME));
        }

        if(redTex != null)
        {
            _blitMaterial.SetTexture("_RedTex", redTex);
        }
        if(greenTex != null)
        {
            _blitMaterial.SetTexture("_GreenTex", greenTex);
        }
        if(blueTex != null)
        {
            _blitMaterial.SetTexture("_BlueTex", blueTex);
        }

        _blitMaterial.SetInt("_RedChannel", (redTex != null) ? (int)redChannel : -1);
        _blitMaterial.SetInt("_GreenChannel", (greenTex != null) ? (int)greenChannel : -1);
        _blitMaterial.SetInt("_BlueChannel", (blueTex != null) ? (int)blueChannel : -1);

        _blitMaterial.SetFloat("_RedDefault", redDefault);
        _blitMaterial.SetFloat("_GreenDefault", greenDefault);
        _blitMaterial.SetFloat("_BlueDefault", blueDefault);

        _blitMaterial.SetInt("_RedInvert", redInvert ? 1 : 0);
        _blitMaterial.SetInt("_GreenInvert", greenInvert ? 1 : 0);
        _blitMaterial.SetInt("_BlueInvert", blueInvert ? 1 : 0);

        Graphics.Blit(redTex, blitRenderTexture, _blitMaterial, 0);
        // CopyTexture fails to encoding to jpg/png
        // Graphics.CopyTexture(blitRenderTexture, result);
        result.ReadPixels(new Rect(0, 0, blitRenderTexture.width, blitRenderTexture.height), 0, 0);
        result.Apply();

        blitRenderTexture.Release();

        return result;
    }

    private void SaveTexture(Texture2D texture, string path, bool isJPG)
    {
        byte[] bytes;
        if (isJPG)
        {
            bytes = ImageConversion.EncodeToJPG(texture);
        }
        else
        {
            bytes = ImageConversion.EncodeToPNG(texture);
        }

        if (path.Length != 0)
        {
            if (bytes != null)
            {
                File.WriteAllBytes(path, bytes);
            }
        }
        AssetDatabase.Refresh();

        Debug.Log("Texture Saved to: " + path);
    }
#endif
}
