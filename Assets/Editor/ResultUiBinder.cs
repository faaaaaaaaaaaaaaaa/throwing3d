using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

// Wires GameManager result-button refs from named children under ResultPanel and validates
// that win/lose/floor-20 layouts toggle the same GameObjects the player sees.
public static class ResultUiBinder
{
    private const string NextName = "NextButton";
    private const string RetryName = "RetryButton";
    private const string ReviveName = "ReviveAdButton";
    private const string DoubleCoinsName = "DoubleCoinsAdButton";

    [MenuItem("Tools/Throwing3D/Wire Result Buttons (GameManager)")]
    public static void WireResultButtons()
    {
        Wire(forceOverwrite: false);
    }

    [MenuItem("Tools/Throwing3D/Wire Result Buttons (Force Rebind)")]
    public static void WireResultButtonsForce()
    {
        Wire(forceOverwrite: true);
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

    [MenuItem("Tools/Throwing3D/Validate Result Button Visibility")]
    public static void ValidateResultVisibility()
    {
        var gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gm == null)
        {
            Debug.LogWarning("ValidateResultVisibility: no GameManager found.");
            return;
        }

        // Force-bind to ResultPanel children first so we test the visible set.
        Wire(forceOverwrite: true);

        bool ok = gm.ValidateAllResultLayoutsSilent();
        if (ok)
            Debug.Log("ValidateResultVisibility: win/lose/floor-20 activeSelf matches layout flags.", gm);
        else
            Debug.LogError("ValidateResultVisibility: FAILED — button refs may point at the wrong objects.", gm);
    }

    private static void Wire(bool forceOverwrite)
    {
        var gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gm == null)
        {
            Debug.LogWarning("ResultUiBinder: no GameManager in open scenes.");
            return;
        }

        var so = new SerializedObject(gm);
        var panelProp = so.FindProperty("_resultPanel");
        if (panelProp.objectReferenceValue == null)
        {
            var foundPanel = FindNamed(null, "ResultPanel");
            if (foundPanel != null)
                panelProp.objectReferenceValue = foundPanel;
        }

        Transform root = panelProp.objectReferenceValue is GameObject panel
            ? panel.transform
            : null;

        bool changed = false;
        changed |= Assign(so, "_nextButton", FindNamed(root, NextName), forceOverwrite);
        changed |= Assign(so, "_retryButton", FindNamed(root, RetryName), forceOverwrite);
        changed |= Assign(so, "_reviveButton", FindNamed(root, ReviveName), forceOverwrite);
        changed |= Assign(so, "_doubleCoinsButton", FindNamed(root, DoubleCoinsName), forceOverwrite);

        var textProp = so.FindProperty("_resultText");
        if (textProp != null && (forceOverwrite || textProp.objectReferenceValue == null) && root != null)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.name != "ResultText") continue;
                textProp.objectReferenceValue = t;
                changed = true;
                break;
            }
        }

        if (changed)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gm);
            var scene = gm.gameObject.scene;
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(
                forceOverwrite
                    ? "ResultUiBinder: force-rebound GameManager result button refs to ResultPanel children."
                    : "ResultUiBinder: wired missing GameManager result button refs.",
                gm);
        }
        else
        {
            Debug.Log("ResultUiBinder: GameManager result button refs already set.", gm);
        }
    }

    private static bool Assign(SerializedObject so, string field, GameObject value, bool force)
    {
        if (value == null) return false;
        var prop = so.FindProperty(field);
        if (prop == null) return false;
        if (!force && prop.objectReferenceValue != null) return false;
        if (prop.objectReferenceValue == value) return false;
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

        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == objectName)
                return t.gameObject;
        }
        return null;
    }
}
