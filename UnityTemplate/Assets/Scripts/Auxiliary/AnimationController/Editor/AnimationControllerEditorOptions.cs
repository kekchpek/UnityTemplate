using UnityEditor;
using UnityEngine;

namespace kekchpek.Auxiliary.AnimationControllerTool.Editor
{
    public static class AnimationControllerEditorOptions
    {
        [MenuItem("Tools/Animation Controller/Resave All Prefabs For AnimationData Migration")]
        private static void ResaveAllPrefabsForAnimationDataMigration()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            int totalCount = prefabGuids.Length;
            int savedCount = 0;

            try
            {
                for (int i = 0; i < totalCount; i++)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    if (!IsMutablePrefabPath(prefabPath))
                    {
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "AnimationData Migration",
                        $"Resaving prefab: {prefabPath}",
                        totalCount == 0 ? 1f : (float)(i + 1) / totalCount);

                    GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                    try
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                        savedCount++;
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"AnimationData migration completed. Resaved {savedCount} prefab(s).");
        }

        private static bool IsMutablePrefabPath(string prefabPath)
        {
            return prefabPath.StartsWith("Assets/");
        }
    }
}
