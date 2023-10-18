using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using PBRTexturesData = TexturePackerUtilities.PBRTexturesData;
using ExportSettings = TexturePackerUtilities.ExportSettings;
using SurfaceData = TexturePackerUtilities.SurfaceData;

public class TexturePackerBatch : EditorWindow
{
    public enum TextureType
    {
        Albedo,
        Normal,
        Metallic,
        Smoothness,
        Roughness,
        AmbientOcclusion,
    }

    public ExportSettings exportSettings = new ExportSettings();

    public string texturesFolder = "Assets/Textures";
    public string suffixAlbedo = "_albedo";
    public string suffixNormal = "_normal";
    public string suffixMetallic = "_metallic";
    public string suffixSmoothness = "_smoothness";
    public string suffixRoughness = "_roughness";
    public string suffixAO = "_ao";
    private string[] _suffixes => new string[] { suffixAlbedo, suffixNormal, suffixMetallic, suffixSmoothness, suffixRoughness, suffixAO };
    private Dictionary<TextureType, string> _suffixesDict => new Dictionary<TextureType, string>()
    {
        { TextureType.Albedo, suffixAlbedo },
        { TextureType.Normal, suffixNormal },
        { TextureType.Metallic, suffixMetallic },
        { TextureType.Smoothness, suffixSmoothness },
        { TextureType.Roughness, suffixRoughness },
        { TextureType.AmbientOcclusion, suffixAO },
    };

    public float metallicDefault = 0f;
    public float smoothnessDefault = 0.5f;

    public SurfaceData redChannel = SurfaceData.Metallic;
    public SurfaceData greenChannel = SurfaceData.Smoothness;
    public SurfaceData blueChannel = SurfaceData.AmbientOcclusion;

    private static EditorWindow _editorWindow;
    private Vector2 _scrollPos;

#if UNITY_EDITOR
    [MenuItem("Tools/Texture Packer/Texture Packer (Batch)")]
    public static void ShowWindow()
    {
        _editorWindow = GetWindow(typeof(TexturePackerBatch), false);
    }

    [MenuItem("Assets/Texture Packer/Texture Packer (Batch)")]
    public static void ShowWindowRightClick()
    {
        _editorWindow = GetWindow(typeof(TexturePackerBatch), false);
    }

    private void OnInspectorUpdate()
    {
        if (!_editorWindow)
            _editorWindow = GetWindow(typeof(TexturePackerBatch), false);
    }

    private void OnGUI()
    {
        if (_editorWindow)
        {
            GUILayout.BeginArea(new Rect(0, 0, _editorWindow.position.size.x, _editorWindow.position.size.y));
            GUILayout.BeginVertical();
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, true, GUILayout.ExpandHeight(true));
        }

        GUIStyle BigBold = new GUIStyle();
        BigBold.fontSize = 16;
        BigBold.fontStyle = FontStyle.Bold;
        BigBold.wordWrap = true;
        BigBold.alignment = TextAnchor.MiddleCenter;

        GUIStyle Wrap = new GUIStyle();
        Wrap.wordWrap = true;
        Wrap.alignment = TextAnchor.MiddleCenter;

        GUIStyle warn = new GUIStyle();
        warn.richText = true;
        warn.wordWrap = true;
        warn.fontStyle = FontStyle.Bold;
        warn.alignment = TextAnchor.MiddleCenter;
        warn.normal.textColor = new Color(0.7f, 0, 0);

        GUIStyle preview = new GUIStyle();
        preview.alignment = TextAnchor.UpperCenter;

        // Batch data
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);

        texturesFolder = EditorGUILayout.TextField("Textures Folder", texturesFolder);
        GUILayout.Space(5f);
        suffixAlbedo = EditorGUILayout.TextField("Albedo Suffix", suffixAlbedo);
        suffixNormal = EditorGUILayout.TextField("Normal Suffix", suffixNormal);
        suffixMetallic = EditorGUILayout.TextField("Metallic Suffix", suffixMetallic);
        suffixSmoothness = EditorGUILayout.TextField("Smoothness Suffix", suffixSmoothness);
        suffixRoughness = EditorGUILayout.TextField("Roughness Suffix", suffixRoughness);
        suffixAO = EditorGUILayout.TextField("AmbientOcclusion Suffix", suffixAO);

        metallicDefault = EditorGUILayout.Slider("Metallic Default", metallicDefault, 0f, 1f);
        smoothnessDefault = EditorGUILayout.Slider("Smoothness Default", smoothnessDefault, 0f, 1f);

        GUILayout.Space(10f);
        GUILayout.EndVertical();

        // Channel Selection
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        exportSettings.folderName = EditorGUILayout.TextField("Folder Name", exportSettings.folderName);

        GUILayout.Space(10f);

        exportSettings.generateMaterial = EditorGUILayout.Toggle("Generate Material", exportSettings.generateMaterial);
        if (exportSettings.generateMaterial)
        {
            exportSettings.shader = (Shader)EditorGUILayout.ObjectField("Shader", exportSettings.shader, typeof(Shader), false);
            exportSettings.material = (Material)EditorGUILayout.ObjectField("Material", exportSettings.material, typeof(Material), false);
            exportSettings.propNameAlbedo = EditorGUILayout.TextField("Albedo property name: ", exportSettings.propNameAlbedo);
            exportSettings.propNameNormal = EditorGUILayout.TextField("Normal property name: ", exportSettings.propNameNormal);
            exportSettings.propNameMask = EditorGUILayout.TextField("Mask property name: ", exportSettings.propNameMask);

            exportSettings.metallicIfMask = EditorGUILayout.Slider("Metallic value if there's mask", exportSettings.metallicIfMask, 0f, 1f);
            exportSettings.smoothnessIfMask = EditorGUILayout.Slider("Smoothness value if there's mask", exportSettings.smoothnessIfMask, 0f, 1f);
            exportSettings.propNameMetallic = EditorGUILayout.TextField("Metallic property name: ", exportSettings.propNameMetallic);
            exportSettings.propNameSmoothness = EditorGUILayout.TextField("Smoothness property name: ", exportSettings.propNameSmoothness);
        }

        GUILayout.Space(10f);
        redChannel = (TexturePackerUtilities.SurfaceData)EditorGUILayout.EnumPopup("Red:", redChannel);
        greenChannel = (TexturePackerUtilities.SurfaceData)EditorGUILayout.EnumPopup("Green:", greenChannel);
        blueChannel = (TexturePackerUtilities.SurfaceData)EditorGUILayout.EnumPopup("Blue:", blueChannel);
        GUILayout.Space(10f);
        exportSettings.format = (TexturePackerUtilities.Format)EditorGUILayout.EnumPopup("Format:", exportSettings.format);

        GUILayout.Space(10f);
        exportSettings.textureFormatStandalone = (TextureImporterFormat)EditorGUILayout.EnumPopup("Standalone", exportSettings.textureFormatStandalone);
        exportSettings.textureFormatAndroid = (TextureImporterFormat)EditorGUILayout.EnumPopup("Android", exportSettings.textureFormatAndroid);
        exportSettings.textureFormatIOS = (TextureImporterFormat)EditorGUILayout.EnumPopup("iOS", exportSettings.textureFormatIOS);
        exportSettings.textureFormatWebGL = (TextureImporterFormat)EditorGUILayout.EnumPopup("WebGL", exportSettings.textureFormatWebGL);

        GUILayout.Space(10f);
        GUILayout.EndVertical();

        // Button
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        if (GUILayout.Button("Generate PBR Textures"))
        {
            EditorUtility.DisplayProgressBar("Packing Textures, please wait...", "", 1f);
            try
            {
                List<PBRTexturesData> pbrTexturesDatas = new List<PBRTexturesData>();

                // "keyword l: <label> t: <type>"
                // https://docs.unity3d.com/ScriptReference/AssetDatabase.FindAssets.html
                string[] guids = AssetDatabase.FindAssets("_ t: texture2D", new[] { texturesFolder });
                foreach (string guid in guids)
                {
                    Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Texture2D));
                    if (tex == null)
                    {
                        continue;
                    }

                    // Get Texture Type
                    bool hasSuffixInName = false;
                    string suffix = "";
                    string textureName = "";
                    TextureType textureType = TextureType.Albedo;
                    foreach (string s in _suffixes)
                    {
                        if (tex.name.Contains(s))
                        {
                            hasSuffixInName = true;
                            suffix = s;
                            textureName = tex.name.Replace(suffix, "");
                            textureType = _suffixesDict.FirstOrDefault(x => x.Value == suffix).Key;
                        }
                    }

                    if (!hasSuffixInName)
                    {
                        continue;
                    }

                    // Duplicate texture because it's not 'Readable'
                    bool linear = textureType != TextureType.Albedo;
                    Texture2D texture = TexturePackerUtilities.DuplicateTexture(tex, linear);

                    // If there's existing data for this texture
                    int currentIndex = -1;
                    for (int i = 0; i < pbrTexturesDatas.Count; i++)
                    {
                        if (pbrTexturesDatas[i].name == textureName)
                        {
                            currentIndex = i;
                            break;
                        }
                    }

                    PBRTexturesData data;
                    if (currentIndex != -1)
                    {
                        data = pbrTexturesDatas[currentIndex];
                    }
                    else
                    {
                        data = new PBRTexturesData(textureName, texture.width, texture.height, redChannel, greenChannel, blueChannel);
                        pbrTexturesDatas.Add(data);
                    }

                    switch (textureType)
                    {
                        case TextureType.Albedo:
                            data.albedoTexture = texture;
                            break;
                        case TextureType.Normal:
                            data.normalTexture = texture;
                            break;
                        case TextureType.Metallic:
                            data.metallicTexture = texture;
                            break;
                        case TextureType.Smoothness:
                            data.smoothnessTexture = texture;
                            data.isRoughness = false;
                            break;
                        case TextureType.Roughness:
                            data.smoothnessTexture = texture;
                            data.isRoughness = true;
                            break;
                        case TextureType.AmbientOcclusion:
                            data.ambientOcclusionTexture = texture;
                            break;
                    }
                }

                if (pbrTexturesDatas != null && pbrTexturesDatas.Count > 0)
                {
                    TexturePackerUtilities.GenerateAndSaveTextures(pbrTexturesDatas, exportSettings);
                }

                pbrTexturesDatas.Clear();
            }
            catch (Exception e)
            {
                Debug.Log($"!!! Exception: {e}");
            }

            EditorUtility.ClearProgressBar();
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
#endif
}
