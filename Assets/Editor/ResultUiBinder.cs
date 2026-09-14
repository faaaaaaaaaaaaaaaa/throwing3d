using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

// Wires GameManager result-button refs from named children and logs missing critical UI.
// Safe to re-run — only fills empty serialized fields (or force-overwrite via menu).
public static class ResultUiBinder
{
    private const string NextName = "NextButton";
    private const string RetryName = "RetryButton";
    private const string ReviveName = "ReviveAdButton";
    private const string DoubleCoinsName = "DoubleCoinsAdButton";

    [MenuItem("Tools/Throwing3D/Wire Result Buttons (GameManager)")]
    public static void WireResultButtons()
    {
        var gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gm == null)
        {
            Debug.LogWarning("ResultUiBinder: no GameManager in open scenes.");
            return;
        }

        var so = new SerializedObject(gm);
        var panelProp = so.FindProperty("_resultPanel");
        Transform root = panelProp.objectReferenceValue is GameObject panel
            ? panel.transform
            : null;

        bool changed = false;
        changed |= AssignIfEmpty(so, "_nextButton", FindNamed(root, NextName));
        changed |= AssignIfEmpty(so, "_retryButton", FindNamed(root, RetryName));
        changed |= AssignIfEmpty(so, "_reviveButton", FindNamed(root, ReviveName));
        changed |= AssignIfEmpty(so, "_doubleCoinsButton", FindNamed(root, DoubleCoinsName));

        if (panelProp.objectReferenceValue == null)
        {
            var foundPanel = GameObject.Find("ResultPanel");
            if (foundPanel != null)
            {
                panelProp.objectReferenceValue = foundPanel;
                changed = true;
            }
        }

        var textProp = so.FindProperty("_resultText");
        if (textProp.objectReferenceValue == null)
        {
            Transform searchRoot = panelProp.objectReferenceValue is GameObject p
                ? p.transform
                : null;
            if (searchRoot != null)
            {
                foreach (var t in searchRoot.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.name != "ResultText") continue;
                    textProp.objectReferenceValue = t;
                    changed = true;
                    break;
                }
            }
        }

        if (changed)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gm);
            var scene = gm.gameObject.scene;
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("ResultUiBinder: wired missing GameManager result button refs.", gm);
        }
        else
        {
            Debug.Log("ResultUiBinder: GameManager result button refs already set.", gm);
        }
    }

    [MenuItem("Tools/Throwing3D/Validate Critical UI Refs")]
    public static void ValidateCriticalRefs()
    {
        var gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gm != null) gm.LogMissingCriticalRefs();
        else Debug.LogWarning("ValidateCriticalRefs: no GameManager found.");

        var ui = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
        if (ui != null) ui.LogMissingCriticalRefs();
        else Debug.LogWarning("ValidateCriticalRefs: no UIManager found.");

        Debug.Log("ValidateCriticalRefs: done (warnings above if anything missing).");
    }

    private static bool AssignIfEmpty(SerializedObject so, string field, GameObject value)
    {
        if (value == null) return false;
        var prop = so.FindProperty(field);
        if (prop == null || prop.objectReferenceValue != null) return false;
        prop.objectReferenceValue = value;
        return true;
    }

    private static GameObject FindNamed(Transform root, string objectName)
    {
        if (root != null)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child.gameObject;
            }
        }

        // Fallback: scene-wide search (includes inactive).
        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == objectName)
                return t.gameObject;
        }
        return null;
    }
}
