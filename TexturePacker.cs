using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using PBRTexturesData = TexturePackerUtilities.PBRTexturesData;
using ExportSettings = TexturePackerUtilities.ExportSettings;

public class TexturePacker : EditorWindow
{
    public PBRTexturesData pbrTexturesData = new PBRTexturesData();
    public ExportSettings exportSettings = new ExportSettings();

    private static EditorWindow _editorWindow;
    private Vector2 _scrollPos;

#if UNITY_EDITOR
    [MenuItem("Tools/Texture Packer")]
    public static void ShowWindow()
    {
        _editorWindow = GetWindow(typeof(TexturePacker), false);
    }

    [MenuItem("Assets/Texture Packer")]
    public static void ShowWindowRightClick()
    {
        _editorWindow = GetWindow(typeof(TexturePacker), false);
    }

    private void OnInspectorUpdate()
    {
        if (!_editorWindow)
            _editorWindow = GetWindow(typeof(TexturePacker), false);
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

        // MaskMap
        GUILayout.BeginVertical(EditorStyles.helpBox);
        // Metallic
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        pbrTexturesData.metallicTexture = (Texture2D)EditorGUILayout.ObjectField("Metallic", pbrTexturesData.metallicTexture, typeof(Texture2D), false);
        if (!pbrTexturesData.metallicTexture)
        {
            GUILayout.Label("No Metallic image found, use slider to set value", Wrap);
            pbrTexturesData.metallicDefault = EditorGUILayout.Slider(pbrTexturesData.metallicDefault, 0f, 1f);
        }
        else
        {
            pbrTexturesData.width = pbrTexturesData.metallicTexture.width;
            pbrTexturesData.height = pbrTexturesData.metallicTexture.height;
        }

        GUILayout.Space(10f);
        GUILayout.EndVertical();

        // Roughness/Smoothness
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        if (pbrTexturesData.isRoughness = EditorGUILayout.Toggle("Input Is Roughness Map", pbrTexturesData.isRoughness))
        {
            pbrTexturesData.smoothnessTexture = (Texture2D)EditorGUILayout.ObjectField("Rough Map (A)", pbrTexturesData.smoothnessTexture, typeof(Texture2D), false);
        }
        else
        {
            pbrTexturesData.smoothnessTexture = (Texture2D)EditorGUILayout.ObjectField("Smoothness Map (A)", pbrTexturesData.smoothnessTexture, typeof(Texture2D), false);
        }
        if (!pbrTexturesData.smoothnessTexture)
        {
            GUILayout.Label("No Smoothness or Roughness image found, use slider to set value", Wrap);
            pbrTexturesData.smoothnessDefault = EditorGUILayout.Slider(pbrTexturesData.smoothnessDefault, 0f, 1f);
            if (pbrTexturesData.smoothnessDefault == 0)
                GUILayout.Label("Slider set to 0, preview image will display alpha of 1", Wrap);
        }
        else
        {
            pbrTexturesData.width = pbrTexturesData.smoothnessTexture.width;
            pbrTexturesData.height = pbrTexturesData.smoothnessTexture.height;
        }

        GUILayout.Space(10f);
        GUILayout.EndVertical();

        // Ambient Occlusion
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        pbrTexturesData.ambientOcclusionTexture = (Texture2D)EditorGUILayout.ObjectField("Ambient Occlusion (G)", pbrTexturesData.ambientOcclusionTexture, typeof(Texture2D), false);
        if (!pbrTexturesData.ambientOcclusionTexture)
        {
            GUILayout.Label("No Ambient Occlusion image found, use slider to set value", Wrap);
        }
        else
        {
            pbrTexturesData.width = pbrTexturesData.ambientOcclusionTexture.width;
            pbrTexturesData.height = pbrTexturesData.ambientOcclusionTexture.height;
        }

        GUILayout.Space(10f);
        GUILayout.EndVertical();
        GUILayout.EndVertical(); // Mask Map

        // Albedo
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        pbrTexturesData.albedoTexture = (Texture2D)EditorGUILayout.ObjectField("Albedo", pbrTexturesData.albedoTexture, typeof(Texture2D), false);

        GUILayout.Space(10f);
        GUILayout.EndVertical();
        // Normal
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        pbrTexturesData.normalTexture = (Texture2D)EditorGUILayout.ObjectField("Normal Map", pbrTexturesData.normalTexture, typeof(Texture2D), false);

        GUILayout.Space(10f);
        GUILayout.EndVertical();

        // Channel Selection
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        exportSettings.folderName = EditorGUILayout.TextField("Folder Name", exportSettings.folderName);
        exportSettings.fileName = EditorGUILayout.TextField("File Name", exportSettings.fileName);

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
        pbrTexturesData.redChannel = (TexturePackerUtilities.SurfaceData)EditorGUILayout.EnumPopup("Red:", pbrTexturesData.redChannel);
        pbrTexturesData.greenChannel = (TexturePackerUtilities.SurfaceData)EditorGUILayout.EnumPopup("Green:", pbrTexturesData.greenChannel);
        pbrTexturesData.blueChannel = (TexturePackerUtilities.SurfaceData)EditorGUILayout.EnumPopup("Blue:", pbrTexturesData.blueChannel);
        GUILayout.Space(10f);
        exportSettings.format = (TexturePackerUtilities.Format)EditorGUILayout.EnumPopup("Format:", exportSettings.format);

        GUILayout.Space(10f);
        exportSettings.textureFormatStandalone = (TextureImporterFormat)EditorGUILayout.EnumPopup("Standalone", exportSettings.textureFormatStandalone);
        exportSettings.textureFormatAndroid = (TextureImporterFormat)EditorGUILayout.EnumPopup("Android", exportSettings.textureFormatAndroid);
        exportSettings.textureFormatIOS = (TextureImporterFormat)EditorGUILayout.EnumPopup("iOS", exportSettings.textureFormatIOS);
        exportSettings.textureFormatWebGL = (TextureImporterFormat)EditorGUILayout.EnumPopup("WebGL", exportSettings.textureFormatWebGL);

        GUILayout.Space(10f);
        GUILayout.EndVertical();

        if (!pbrTexturesData.metallicTexture && !pbrTexturesData.ambientOcclusionTexture && !pbrTexturesData.smoothnessTexture && !pbrTexturesData.albedoTexture && !pbrTexturesData.normalTexture)
        {
            GUILayout.Label("No Textures selected", Wrap);
        }
        else
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(10f);
            if (GUILayout.Button("Generate PBR Textures"))
            {
                EditorUtility.DisplayProgressBar("Packing Textures, please wait...", "", 1f);
                Texture2D albedo = null;
                Texture2D normal = null;
                Texture2D mask = TexturePackerUtilities.GenerateMaskTexture(pbrTexturesData);
                if (pbrTexturesData.albedoTexture)
                {
                    albedo = TexturePackerUtilities.DuplicateTexture(pbrTexturesData.albedoTexture, linear: false);
                }
                if (pbrTexturesData.normalTexture)
                {
                    normal = TexturePackerUtilities.DuplicateTexture(pbrTexturesData.normalTexture, linear: true);
                }
                TexturePackerUtilities.SaveTexturesWithPath(exportSettings, albedo, normal, mask);
                EditorUtility.ClearProgressBar();
            }

            GUILayout.Space(5f);
            if (GUILayout.Button("Generate Mask Texture"))
            {
                EditorUtility.DisplayProgressBar("Packing Textures, please wait...", "", 1f);
                Texture2D mask = TexturePackerUtilities.GenerateMaskTexture(pbrTexturesData);
                TexturePackerUtilities.SaveTexturesWithPath(exportSettings, mask: mask);
                EditorUtility.ClearProgressBar();
            }

            GUILayout.Space(5f);
            if (GUILayout.Button("Clear All"))
            {
                pbrTexturesData.albedoTexture = pbrTexturesData.normalTexture = pbrTexturesData.metallicTexture = pbrTexturesData.ambientOcclusionTexture = pbrTexturesData.smoothnessTexture = null;
            }
            GUILayout.Space(10f);
            GUILayout.EndVertical();
        }

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
