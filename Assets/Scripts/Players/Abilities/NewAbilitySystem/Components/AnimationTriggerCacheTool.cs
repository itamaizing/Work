#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AnimationTriggerCacheTool
{
    [MenuItem("Tools/Skills/Cache All Animation Trigger Durations")]
    public static void CacheAllHeroPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int scannedCount = 0;
        int changedCount = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var skillManager = prefab.GetComponent<SkillManager>() ?? prefab.GetComponentInChildren<SkillManager>();
            if (skillManager == null) continue;

            var heroComponent = prefab.GetComponent<HeroComponent>() ?? prefab.GetComponentInChildren<HeroComponent>();
            if(heroComponent == null) continue;

            var animator = prefab.GetComponentInChildren<Animator>();
            if (animator == null) continue;

            scannedCount++;
            bool prefabChanged = false;

            foreach (var skill in skillManager.Skills)
            {
                if (skill == null) continue;
                if (skill.Animation.CacheAnimationTriggersEditor(animator))
                {
                    EditorUtility.SetDirty(skill);
                    prefabChanged = true;
                }
            }

            if (prefabChanged)
            {
                EditorUtility.SetDirty(skillManager);
                changedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[AnimationTriggerCacheTool] Scanned {scannedCount} prefabs with SkillManager, updated {changedCount}.");
    }
}
#endif