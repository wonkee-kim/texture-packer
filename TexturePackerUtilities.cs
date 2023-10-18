using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TexturePackerUtilities
{
    private static readonly int PROP_ALBEDO_TEX = Shader.PropertyToID("_BaseMap");
    private static readonly int PROP_METALLIC_TEX = Shader.PropertyToID("_MetallicTex");
    private static readonly int PROP_SMOOTHNESS_TEX = Shader.PropertyToID("_SmoothnessTex");
    private static readonly int PROP_INVERT_SMOOTHNESS = Shader.PropertyToID("_InvertSmoothness");
    private static readonly int PROP_AMBIENTOCCLUSION_TEX = Shader.PropertyToID("_AmbientOcclusionTex");

    private static readonly int PROP_HAS_METALLIC = Shader.PropertyToID("_HasMetallic");
    private static readonly int PROP_HAS_SMOOTHNESS = Shader.PropertyToID("_HasSmoothness");
    private static readonly int PROP_HAS_AO = Shader.PropertyToID("_HasAO");

    private static readonly int PROP_METALLIC_DEFAULT = Shader.PropertyToID("_MetallicDefault");
    private static readonly int PROP_SMOOTHNESS_DEFAULT = Shader.PropertyToID("_SmoothnessDefault");

    private static readonly int PROP_RED_DATA = Shader.PropertyToID("_RedData");
    private static readonly int PROP_GREEN_DATA = Shader.PropertyToID("_GreenData");
    private static readonly int PROP_BLUE_DATA = Shader.PropertyToID("_BlueData");

    private const string PACKER_SHADER_NAME = "Hidden/TexturePacker";
    private const string TERRAIN_PACKER_SHADER_NAME = "Hidden/TexturePacker(Terrain)";
    private const string SUFFIX_ALBEDO = "_albedo";
    private const string SUFFIX_NORMAL = "_normal";
    private const string SUFFIX_MASK = "_mask";
    private const string SUFFIX_TERRAIN = "_terrain";

    public enum SurfaceData
    {
        Metallic = 0,
        Smoothness = 1,
        AmbientOcclusion = 2,
    }

    public enum Format
    {
        JPG,
        PNG,
    }

    [System.Serializable]
    public class PBRTexturesData
    {
        public string name = "Texture";
        public int width = 512;
        public int height = 512;

        public Texture2D albedoTexture = null;
        public Texture2D normalTexture = null;

        // MaskMap Settings
        public Texture2D metallicTexture = null;
        public Texture2D smoothnessTexture = null;
        public Texture2D ambientOcclusionTexture = null;
        public float metallicDefault = 0f;
        public float smoothnessDefault = 0.5f;
        public bool isRoughness = false;

        public SurfaceData redChannel = SurfaceData.Metallic;
        public SurfaceData greenChannel = SurfaceData.Smoothness;
        public SurfaceData blueChannel = SurfaceData.AmbientOcclusion;

        public PBRTexturesData() { }

        public PBRTexturesData(string name, int width, int height, SurfaceData redChannel, SurfaceData greenChannel, SurfaceData blueChannel)
        {
            this.name = name;
            this.width = width;
            this.height = height;
            this.redChannel = redChannel;
            this.greenChannel = greenChannel;
            this.blueChannel = blueChannel;
        }
    }

    [System.Serializable]
    public class ExportSettings
    {
        public Format format = Format.JPG;
        public string folderName = "Assets/TexturePacker/";
        public string fileName = "Texture";

        public bool generateMaterial = true;
        public Shader shader;
        public Material material;
        public string propNameAlbedo = "_BaseMap";
        public string propNameNormal = "_BumpMap";
        public string propNameMask = "_MaskMap";

        public float metallicIfMask = 0f;
        public float smoothnessIfMask = 1f;
        public string propNameMetallic = "_Metallic";
        public string propNameSmoothness = "_Smoothness";

        public TextureImporterFormat textureFormatStandalone = TextureImporterFormat.BC7;
        public TextureImporterFormat textureFormatAndroid = TextureImporterFormat.ASTC_8x8;
        public TextureImporterFormat textureFormatIOS = TextureImporterFormat.ASTC_8x8;
        public TextureImporterFormat textureFormatWebGL = TextureImporterFormat.DXT5Crunched;
    }

    private static Material _blitMaterial;
    private static Material _terrainBlitMaterial;

    // #if UNITY_EDITOR
    public static void GenerateAndSaveTextures(List<PBRTexturesData> pbrTexturesDatas, ExportSettings exportSettings)
    {
        for (int i = 0; i < pbrTexturesDatas.Count; i++)
        {
            Texture2D albedo = null;
            Texture2D normal = null;
            if (pbrTexturesDatas[i].albedoTexture != null)
            {
                albedo = pbrTexturesDatas[i].albedoTexture;
            }
            if (pbrTexturesDatas[i].normalTexture != null)
            {
                normal = pbrTexturesDatas[i].normalTexture;
            }
            Texture2D mask = GenerateMaskTexture(pbrTexturesDatas[i]);

            exportSettings.fileName = pbrTexturesDatas[i].name;
            SaveTexturesWithPath(exportSettings, albedo, normal, mask);
        }
    }

    public static Texture2D GenerateMaskTexture(PBRTexturesData pbrTexturesData)
    {
        int width = pbrTexturesData.width;
        int height = pbrTexturesData.height;
        RenderTexture blitRenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        blitRenderTexture.useMipMap = false;
        blitRenderTexture.filterMode = FilterMode.Bilinear;

        Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, mipCount: 1, linear: true);

        if (_blitMaterial == null)
        {
            _blitMaterial = new Material(Shader.Find(PACKER_SHADER_NAME));
        }

        _blitMaterial.SetTexture(PROP_METALLIC_TEX, pbrTexturesData.metallicTexture);
        _blitMaterial.SetTexture(PROP_SMOOTHNESS_TEX, pbrTexturesData.smoothnessTexture);
        _blitMaterial.SetTexture(PROP_AMBIENTOCCLUSION_TEX, pbrTexturesData.ambientOcclusionTexture);

        _blitMaterial.SetFloat(PROP_METALLIC_DEFAULT, pbrTexturesData.metallicDefault);
        _blitMaterial.SetFloat(PROP_SMOOTHNESS_DEFAULT, pbrTexturesData.smoothnessDefault);

        _blitMaterial.SetInt(PROP_HAS_METALLIC, pbrTexturesData.metallicTexture == null ? 0 : 1);
        _blitMaterial.SetInt(PROP_HAS_SMOOTHNESS, pbrTexturesData.smoothnessTexture == null ? 0 : 1);
        _blitMaterial.SetInt(PROP_HAS_AO, pbrTexturesData.ambientOcclusionTexture == null ? 0 : 1);

        _blitMaterial.SetInt(PROP_INVERT_SMOOTHNESS, pbrTexturesData.isRoughness ? 1 : 0);
        _blitMaterial.SetInt(PROP_RED_DATA, (int)pbrTexturesData.redChannel);
        _blitMaterial.SetInt(PROP_GREEN_DATA, (int)pbrTexturesData.greenChannel);
        _blitMaterial.SetInt(PROP_BLUE_DATA, (int)pbrTexturesData.blueChannel);

        Graphics.Blit(pbrTexturesData.smoothnessTexture, blitRenderTexture, _blitMaterial, 0);
        // CopyTexture fails to encoding to jpg/png
        // Graphics.CopyTexture(blitRenderTexture, result);
        result.ReadPixels(new Rect(0, 0, blitRenderTexture.width, blitRenderTexture.height), 0, 0);
        result.Apply();

        blitRenderTexture.Release();

        return result;
    }

    public static Texture2D GenerateTerrainTexture(PBRTexturesData pbrTexturesData)
    {
        int width = pbrTexturesData.width;
        int height = pbrTexturesData.height;
        RenderTexture blitRenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        blitRenderTexture.useMipMap = false;
        blitRenderTexture.filterMode = FilterMode.Bilinear;

        Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, mipCount: 1, linear: false);

        if (_terrainBlitMaterial == null)
        {
            _terrainBlitMaterial = new Material(Shader.Find(TERRAIN_PACKER_SHADER_NAME));
        }

        _terrainBlitMaterial.SetTexture(PROP_ALBEDO_TEX, pbrTexturesData.albedoTexture);
        _terrainBlitMaterial.SetTexture(PROP_SMOOTHNESS_TEX, pbrTexturesData.smoothnessTexture);

        _terrainBlitMaterial.SetInt(PROP_HAS_SMOOTHNESS, pbrTexturesData.smoothnessTexture == null ? 0 : 1);
        _terrainBlitMaterial.SetFloat(PROP_SMOOTHNESS_DEFAULT, pbrTexturesData.smoothnessDefault);
        _terrainBlitMaterial.SetInt(PROP_INVERT_SMOOTHNESS, pbrTexturesData.isRoughness ? 1 : 0);

        Graphics.Blit(pbrTexturesData.albedoTexture, blitRenderTexture, _terrainBlitMaterial, 0);
        // CopyTexture fails to encoding to jpg/png
        // Graphics.CopyTexture(blitRenderTexture, result);
        result.ReadPixels(new Rect(0, 0, blitRenderTexture.width, blitRenderTexture.height), 0, 0);
        result.Apply();

        blitRenderTexture.Release();

        return result;
    }

    public static void SaveTexturesWithPath(ExportSettings exportSettings, Texture2D albedo = null, Texture2D normal = null, Texture2D mask = null)
    {
        try
        {
            bool isJPG = exportSettings.format == Format.JPG;
            string extension = isJPG ? "jpg" : "png";
            string pathAlbedo = exportSettings.folderName + exportSettings.fileName + SUFFIX_ALBEDO + "." + extension;
            string pathNormal = exportSettings.folderName + exportSettings.fileName + SUFFIX_NORMAL + "." + extension;
            string pathMask = exportSettings.folderName + exportSettings.fileName + SUFFIX_MASK + "." + extension;

            if (albedo != null)
            {
                SaveTexture(albedo, pathAlbedo, exportSettings);
                SetTextureImportSettings(pathAlbedo, TextureImporterType.Default, sRGBTexture: true, exportSettings);
            }
            if (normal != null)
            {
                SaveTexture(normal, pathNormal, exportSettings);
                SetTextureImportSettings(pathNormal, TextureImporterType.NormalMap, sRGBTexture: false, exportSettings);
            }
            if (mask != null)
            {
                SaveTexture(mask, pathMask, exportSettings);
                SetTextureImportSettings(pathMask, TextureImporterType.Default, sRGBTexture: false, exportSettings);
            }

            if (exportSettings.generateMaterial && !(exportSettings.shader == null && exportSettings.material == null))
            {
                Material material = (exportSettings.material != null) ? exportSettings.material : new Material(exportSettings.shader);
                if (albedo != null)
                {
                    material.SetTexture(exportSettings.propNameAlbedo, (Texture2D)AssetDatabase.LoadAssetAtPath(pathAlbedo, typeof(Texture2D)));
                }
                if (normal != null)
                {
                    material.SetTexture(exportSettings.propNameNormal, (Texture2D)AssetDatabase.LoadAssetAtPath(pathNormal, typeof(Texture2D)));
                }
                if (mask != null)
                {
                    material.SetTexture(exportSettings.propNameMask, (Texture2D)AssetDatabase.LoadAssetAtPath(pathMask, typeof(Texture2D)));
                    material.SetFloat(exportSettings.propNameMetallic, exportSettings.metallicIfMask);
                    material.SetFloat(exportSettings.propNameSmoothness, exportSettings.smoothnessIfMask);
                }

                AssetDatabase.CreateAsset(material, exportSettings.folderName + exportSettings.fileName + ".mat");
                AssetDatabase.SaveAssets();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    public static void SaveTerrainTexturesWithPath(ExportSettings exportSettings, Texture2D texture)
    {
        try
        {
            string path = exportSettings.folderName + exportSettings.fileName + SUFFIX_TERRAIN + ".png";
            SaveTexture(texture, path, exportSettings, hasAlpha: true);
            SetTextureImportSettings(path, TextureImporterType.Default, sRGBTexture: true, exportSettings);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    public static Texture2D DuplicateTexture(Texture2D source, bool linear = false)
    {
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Texture2D result = new Texture2D(source.width, source.height);

        Graphics.Blit(source, rt);
        result.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        result.Apply();

        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    public static void SaveTextureWithPanel(Texture2D texture, ExportSettings exportSettings)
    {
        bool isJPG = exportSettings.format == Format.JPG;
        string extension = isJPG ? "jpg" : "png";
        // TODO: Panel adds extension 'jpeg' and fails to save. Changing it to 'jpg' fixes it.
        var path = EditorUtility.SaveFilePanelInProject("Save Texture To Diectory", "LitMask", extension, "Saved");
        SaveTexture(texture, path, exportSettings);
    }

    private static void SaveTexture(Texture2D texture, string path, ExportSettings exportSettings, bool hasAlpha = false)
    {
        byte[] bytes;
        if (exportSettings.format == Format.JPG && !hasAlpha)
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
                if (!Directory.Exists(exportSettings.folderName))
                {
                    Directory.CreateDirectory(exportSettings.folderName);
                }
                File.WriteAllBytes(path, bytes);
            }
        }
        AssetDatabase.Refresh();

        Debug.Log("Texture Saved to: " + path);
    }

    private static void SetTextureImportSettings(string path, TextureImporterType type, bool sRGBTexture, ExportSettings exportSettings)
    {
        TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(path);
        textureImporter.textureType = type;

        TextureImporterSettings settings = new TextureImporterSettings();
        textureImporter.ReadTextureSettings(settings);
        settings.sRGBTexture = sRGBTexture;
        textureImporter.SetTextureSettings(settings);

        textureImporter.textureCompression = TextureImporterCompression.CompressedLQ;
        textureImporter.crunchedCompression = true;
        // textureImporter.textureFormat = TextureImporterFormat.ASTC_8x8;
        textureImporter.SetPlatformTextureSettings("Standalone", maxTextureSize: 2048, exportSettings.textureFormatStandalone);
        textureImporter.SetPlatformTextureSettings("Android", maxTextureSize: 2048, exportSettings.textureFormatAndroid);
        textureImporter.SetPlatformTextureSettings("iPhone", maxTextureSize: 2048, exportSettings.textureFormatIOS);
        textureImporter.SetPlatformTextureSettings("WebGL", maxTextureSize: 2048, exportSettings.textureFormatWebGL, compressionQuality: 100, allowsAlphaSplit: false);

        EditorUtility.SetDirty(textureImporter);
        textureImporter.SaveAndReimport();
    }
    // #endif
}