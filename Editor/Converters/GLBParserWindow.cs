#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VoyageVoyage
{
    public class GLBMeshAppenderWindow : EditorWindow
    {
        private MeshRenderer targetMeshRenderer;
        private string outputPath = "";

        [MenuItem("Tools/GLB Mesh Appender")]
        public static void ShowWindow()
        {
            GetWindow<GLBMeshAppenderWindow>("GLB Mesh Appender");
        }

        private void OnGUI()
        {
            GUILayout.Label("GLB Mesh Appender", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Target MeshRenderer
            GUILayout.Label("MeshRenderer to Append:");
            targetMeshRenderer = (MeshRenderer)EditorGUILayout.ObjectField(targetMeshRenderer, typeof(MeshRenderer), true);

            GUILayout.Space(10);

            // Output path
            GUILayout.Label("Output GLB File:");
            EditorGUILayout.BeginHorizontal();
            outputPath = EditorGUILayout.TextField(outputPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.SaveFilePanel("Save GLB File", "", "output", "glb");
                if (!string.IsNullOrEmpty(path))
                    outputPath = path;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            GUI.enabled = targetMeshRenderer != null && !string.IsNullOrEmpty(outputPath);
            if (GUILayout.Button("Append Mesh to GLB", GUILayout.Height(30)))
            {
                UnityToGLBExporter.ExportMeshRendererToGLB(targetMeshRenderer, outputPath);
            }
            GUI.enabled = true;
        }


    }
}
#endif