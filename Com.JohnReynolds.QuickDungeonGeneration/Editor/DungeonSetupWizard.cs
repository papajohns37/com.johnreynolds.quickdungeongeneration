#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class DungeonSetupWizard
{
    private const string RootName = "Dungeon Generator";

    [MenuItem("Tools/Quick Dungeon/Create Dungeon Generator")]
    public static void CreateDungeon()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            Debug.LogWarning("A Dungeon Generator already exists in the active scene.", existing);
            return;
        }

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Dungeon Generator");

        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        Undo.RegisterCreatedObjectUndo(gridObject, "Create Dungeon Grid");
        gridObject.transform.SetParent(root.transform, false);

        Tilemap floorTilemap = CreateTilemap(gridObject.transform, "FloorTilemap");
        Tilemap wallTilemap = CreateTilemap(gridObject.transform, "WallTilemap");

        GameObject propsObject = new GameObject("Props");
        Undo.RegisterCreatedObjectUndo(propsObject, "Create Dungeon Props");
        propsObject.transform.SetParent(root.transform, false);

        DungeonManager manager = root.AddComponent<DungeonManager>();
        DungeonPropSpawner spawner = root.AddComponent<DungeonPropSpawner>();
        Undo.RegisterCreatedObjectUndo(manager, "Add Dungeon Manager");
        Undo.RegisterCreatedObjectUndo(spawner, "Add Dungeon Prop Spawner");

        manager.Tilemap = floorTilemap;
        manager.WallTilemap = wallTilemap;
        manager.PropSpawner = spawner;
        manager.Config = CreateConfigAsset("Assets/QuickDungeonSamples/DungeonGenerationProfile.asset");
        spawner.Configure(
            CreateSpawnProfileAsset("Assets/QuickDungeonSamples/DungeonSpawnProfile.asset"),
            propsObject.transform);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(spawner);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("Dungeon Generator setup complete. Assign floor/wall tiles and press Generate from your own script.", root);
    }

    [MenuItem("Tools/Quick Dungeon/Create Sample Scene")]
    public static void CreateSampleScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateDungeon();

        string directory = "Assets/QuickDungeonSamples";
        EnsureFolder(directory);
        string scenePath = directory + "/QuickDungeonSample.unity";
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
        Debug.Log("Quick Dungeon sample scene saved to " + scenePath);
    }

    private static Tilemap CreateTilemap(Transform parent, string name)
    {
        GameObject tilemapObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        Undo.RegisterCreatedObjectUndo(tilemapObject, "Create Dungeon Tilemap");
        tilemapObject.transform.SetParent(parent, false);
        return tilemapObject.GetComponent<Tilemap>();
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; ++i)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private static DungeonGenerationConfig CreateConfigAsset(string assetPath)
    {
        EnsureFolder("Assets/QuickDungeonSamples");
        DungeonGenerationConfig config =
            AssetDatabase.LoadAssetAtPath<DungeonGenerationConfig>(assetPath);
        if (config != null)
        {
            return config;
        }

        config = ScriptableObject.CreateInstance<DungeonGenerationConfig>();
        AssetDatabase.CreateAsset(config, AssetDatabase.GenerateUniqueAssetPath(assetPath));
        AssetDatabase.SaveAssets();
        return config;
    }

    private static DungeonSpawnProfile CreateSpawnProfileAsset(string assetPath)
    {
        EnsureFolder("Assets/QuickDungeonSamples");
        DungeonSpawnProfile profile =
            AssetDatabase.LoadAssetAtPath<DungeonSpawnProfile>(assetPath);
        if (profile != null)
        {
            return profile;
        }

        profile = ScriptableObject.CreateInstance<DungeonSpawnProfile>();
        AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(assetPath));
        AssetDatabase.SaveAssets();
        return profile;
    }
}
#endif
