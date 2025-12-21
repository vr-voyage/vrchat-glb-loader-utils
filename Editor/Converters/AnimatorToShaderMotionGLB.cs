#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VoyageVoyage
{
    class AnimatorToShaderMotionGLB
    {
        [MenuItem("CONTEXT/Animator/Generate ShaderMotion GLB from this model")]
        static void CreatePlayer_FromAnimator(MenuCommand command)
        {
            Animator animator = (Animator)command.context;
            string path = EditorUtility.SaveFilePanel("Save GLB File", "", "output", "glb");
            if (path == null)
            {
                return;
            }

            GameObject gameObject = new GameObject();

            MeshRenderer renderer = VoyageVoyageShaderMotion.MeshPlayer.CreatePlayerDirect(gameObject, animator);
            if (renderer == null)
            {
                Object.DestroyImmediate(gameObject);
                return;
            }

            UnityToGLBExporter.ExportMeshRendererToGLB(renderer, path);
            Object.DestroyImmediate(gameObject);
        }
    }
}
#endif