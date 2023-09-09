#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DuplicateTexturesWindow : EditorWindow
{
    private DuplicateData _data = new DuplicateData();
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Duplicate Textures")]
    private static void Init()
    {
        var window = GetWindow<DuplicateTexturesWindow>();
        window.titleContent = new GUIContent("Duplicate Textures");
        window.minSize = new Vector2(500f, 300f);
        window.Show();
    }

    private void Awake()
    {
        _data.Load();
        Repaint();
    }

    private void OnGUI()
    {
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
        if (GUILayout.Button("Find"))
        {
            _data.Textures.Clear();
            _data.Textures = FindDuplicateTextures();
            _data.Save();
        }

        if (_data.Textures.Count > 0)
            for (var i = 0; i < _data.Textures.Count; i++)
            {
                var paths = _data.Textures[i].Paths;
                GUI.DrawTexture(GUILayoutUtility.GetRect(32f, 32f), AssetDatabase.GetCachedIcon(paths[0]), ScaleMode.ScaleToFit);
                for (var j = 0; j < paths.Count; j++)
                    if (GUILayout.Button(paths[j]))
                        EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(paths[j]));
            }

        GUILayout.EndScrollView();
    }

    private static List<DuplicateTexture> FindDuplicateTextures()
    {
        var guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
        var textures = new Dictionary<long, DuplicateTexture>(guids.Length);
        for (var i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path))
                continue;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                continue;

            var hash = texture.Copy().GetPixels32().Hash();
            if (!textures.ContainsKey(hash))
                textures[hash] = new DuplicateTexture();

            textures[hash].Paths.Add(path);
        }

        return textures.Values.Where(x => x.Paths.Count > 1).ToList();
    }
}

[Serializable]
public class DuplicateTexture
{
    [SerializeField] public List<string> Paths = new List<string>();
}

[Serializable]
public class DuplicateData
{
    [SerializeField] public List<DuplicateTexture> Textures = new List<DuplicateTexture>();
}

public static class DuplicateTexturesExtension
{
    private const string DuplicateTexturesJsonPath = "Library/DuplicateTextures.json";
    private const int TextureWidth = 8;
    private const int TextureHeight = 8;

    public static Texture2D Copy(this Texture2D texture)
    {
        var renderTexture = new RenderTexture(TextureWidth, TextureHeight, 0);
        RenderTexture.active = renderTexture;
        Graphics.Blit(texture, renderTexture);
        var newTexture = new Texture2D(TextureWidth, TextureHeight);
        newTexture.ReadPixels(new Rect(0, 0, TextureWidth, TextureHeight), 0, 0);
        newTexture.Apply();
        RenderTexture.active = null;

        return newTexture;
    }

    public static long Hash(this Color32[] colors)
    {
        const long prime = 31;
        unchecked
        {
            long hash = 17;
            for (var i = 0; i < colors.Length; i++)
            {
                long pixel = colors[i].r << 24 | colors[i].g << 16 | colors[i].b << 8 | colors[i].a;
                hash = hash * prime + pixel;
            }

            return hash;
        }
    }

    public static void Save(this DuplicateData data)
    {
        File.WriteAllText(DuplicateTexturesJsonPath, JsonUtility.ToJson(data));
    }

    public static void Load(this DuplicateData data)
    {
        if (!File.Exists(DuplicateTexturesJsonPath))
            return;

        data.Textures = JsonUtility.FromJson<DuplicateData>(File.ReadAllText(DuplicateTexturesJsonPath)).Textures;
    }
}
#endif