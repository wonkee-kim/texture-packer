using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TextureSettingsUtilities : EditorWindow
{
    [System.Serializable]
    public class Settings
    {
        public int maxTextureSize = 2048;
        public TextureImporterFormat textureFormatStandalone = TextureImporterFormat.DXT5Crunched;
        public TextureImporterFormat textureFormatAndroid = TextureImporterFormat.ASTC_8x8;
        public TextureImporterFormat textureFormatIOS = TextureImporterFormat.ASTC_8x8;
        public TextureImporterFormat textureFormatWebGL = TextureImporterFormat.DXT5Crunched;
    }

    public string folderName = "Assets/TexturePacker/";
    public Settings settings;

    private static EditorWindow _editorWindow;
    private Vector2 _scrollPos;

#if UNITY_EDITOR
    [MenuItem("Tools/Texture Settings Utilities")]
    public static void ShowWindow()
    {
        _editorWindow = GetWindow(typeof(TextureSettingsUtilities), false);
    }

    [MenuItem("Assets/Texture Settings Utilities")]
    public static void ShowWindowRightClick()
    {
        _editorWindow = GetWindow(typeof(TextureSettingsUtilities), false);
    }

    private void OnInspectorUpdate()
    {
        if (!_editorWindow)
            _editorWindow = GetWindow(typeof(TextureSettingsUtilities), false);
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

        // Folder
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        folderName = EditorGUILayout.TextField("Folder Name", folderName);

        GUILayout.Space(10f);
        GUILayout.EndVertical();

        // Format
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        settings.maxTextureSize = EditorGUILayout.IntField("Max Texture Size", settings.maxTextureSize);

        GUILayout.Space(10f);
        settings.textureFormatStandalone = (TextureImporterFormat)EditorGUILayout.EnumPopup("Standalone", settings.textureFormatStandalone);
        settings.textureFormatAndroid = (TextureImporterFormat)EditorGUILayout.EnumPopup("Android", settings.textureFormatAndroid);
        settings.textureFormatIOS = (TextureImporterFormat)EditorGUILayout.EnumPopup("iOS", settings.textureFormatIOS);
        settings.textureFormatWebGL = (TextureImporterFormat)EditorGUILayout.EnumPopup("WebGL", settings.textureFormatWebGL);

        GUILayout.Space(10f);
        GUILayout.EndVertical();

        // Button
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        if (GUILayout.Button("Change Texture Settings"))
        {
            EditorUtility.DisplayProgressBar("Changing Textures, please wait...", "", 1f);
            ChangeTextureSettings(folderName, settings);
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


    private void ChangeTextureSettings(string folder, Settings settings)
    {
        // "keyword l: <label> t: <type>"
        // https://docs.unity3d.com/ScriptReference/AssetDatabase.FindAssets.html
        string[] guids = AssetDatabase.FindAssets("_ t: texture2D", new[] { folder });
        foreach (string guid in guids)
        {
            SetTextureSettings(AssetDatabase.GUIDToAssetPath(guid), settings);
        }
    }

    private void SetTextureSettings(string path, Settings settings)
    {
        TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(path);

        textureImporter.textureCompression = TextureImporterCompression.CompressedLQ;
        textureImporter.crunchedCompression = true;

        textureImporter.SetPlatformTextureSettings("Standalone", settings.maxTextureSize, settings.textureFormatStandalone, compressionQuality: 100, allowsAlphaSplit: false);
        textureImporter.SetPlatformTextureSettings("Android", settings.maxTextureSize, settings.textureFormatAndroid);
        textureImporter.SetPlatformTextureSettings("iPhone", settings.maxTextureSize, settings.textureFormatIOS);
        textureImporter.SetPlatformTextureSettings("WebGL", settings.maxTextureSize, settings.textureFormatWebGL, compressionQuality: 100, allowsAlphaSplit: false);

        EditorUtility.SetDirty(textureImporter);
        textureImporter.SaveAndReimport();
    }
#endif
}
