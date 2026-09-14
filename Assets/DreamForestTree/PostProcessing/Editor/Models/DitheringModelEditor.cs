using PPSEngine_one;
using UnityEditor;

namespace PPS_one
{
    [PostProcessingModelEditor(typeof(DitheringModel))]
    public class DitheringModelEditor : PostProcessingModelEditor
    {
        public override void OnInspectorGUI()
        {
            if (profile.grain.enabled && target.enabled)
                EditorGUILayout.HelpBox("Grain is enabled, you probably don't need dithering !", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("Nothing to configure !", MessageType.Info);
        }
    }
}
