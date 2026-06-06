using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RuntimeReferenceFinder
{
    public static Transform FindDeepTransform(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName)) return null;

        if (root.name == targetName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepTransform(root.GetChild(i), targetName);
            if (found != null) return found;
        }

        return null;
    }

    public static GameObject FindDeepGameObject(Transform root, string targetName)
    {
        Transform found = FindDeepTransform(root, targetName);
        return found != null ? found.gameObject : null;
    }

    public static GameObject FindDeepGameObject(Transform root, params string[] targetNames)
    {
        if (root == null || targetNames == null) return null;

        foreach (string targetName in targetNames)
        {
            GameObject found = FindDeepGameObject(root, targetName);
            if (found != null) return found;
        }

        return null;
    }

    public static T FindDeepComponent<T>(Transform root, string targetName) where T : Component
    {
        GameObject found = FindDeepGameObject(root, targetName);
        if (found == null) return null;

        T component = found.GetComponent<T>();
        if (component != null) return component;

        return found.GetComponentInChildren<T>(true);
    }

    public static T FindDeepComponent<T>(Transform root, params string[] targetNames) where T : Component
    {
        if (root == null || targetNames == null) return null;

        foreach (string targetName in targetNames)
        {
            T component = FindDeepComponent<T>(root, targetName);
            if (component != null) return component;
        }

        return null;
    }

    public static List<Transform> FindChildrenMatching(Transform root, Func<Transform, bool> predicate)
    {
        List<Transform> result = new List<Transform>();
        if (root == null || predicate == null) return result;

        CollectChildren(root, predicate, result);
        return result;
    }

    private static void CollectChildren(Transform current, Func<Transform, bool> predicate, List<Transform> result)
    {
        if (predicate(current))
        {
            result.Add(current);
        }

        for (int i = 0; i < current.childCount; i++)
        {
            CollectChildren(current.GetChild(i), predicate, result);
        }
    }

    public static GameObject FindGameObjectInLoadedScenes(string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;

        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid()) continue;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var found = FindDeepGameObject(root.transform, targetName);
                if (found != null) return found;
            }
        }

        return null;
    }

    public static GameObject FindGameObjectEverywhere(string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;

        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject candidate in objects)
        {
            if (candidate == null) continue;
            if (candidate.name != targetName) continue;
            if (!candidate.scene.IsValid()) continue;

            return candidate;
        }

        return null;
    }

    public static T FindComponentEverywhere<T>() where T : Component
    {
        T[] components = Resources.FindObjectsOfTypeAll<T>();
        foreach (T component in components)
        {
            if (component == null) continue;
            if (!component.gameObject.scene.IsValid()) continue;

            return component;
        }

        return null;
    }

    public static T FindComponentInLoadedScenes<T>() where T : Component
    {
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid()) continue;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
        }

        return null;
    }
}