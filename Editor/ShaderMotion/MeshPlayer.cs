#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace VoyageVoyageShaderMotion
{
    public class MeshPlayer
    {
        static readonly Shader mToonVersionShader = Shader.Find("GLBLoader/ShaderMotion/MToon10MeshPlayer");

        public static MeshRenderer CreatePlayerDirect(GameObject go, Animator animator)
        {
            Renderer[] renderers = animator.gameObject.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogError($"No renderer found on {animator.gameObject.name} and its children");
                return null;
            }
            Mesh mesh = new();

            var mats = new List<Material>();
            foreach (var renderer in renderers)
            {
                foreach (var srcMat in renderer.sharedMaterials)
                {
                    mats.Add(Object.Instantiate(srcMat));
                    if (!(renderer is SkinnedMeshRenderer || renderer is MeshRenderer))
                    {
                        break; // only one material since it's treated as a quad
                    }
                        
                }
            }

            MeshRenderer player = go.AddComponent<MeshRenderer>();
            player.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            player.sharedMaterials = mats.ToArray();
            CopySettings(renderers[0], player);

            {
                (Texture2D bone, Texture2D shape) texs = new()
                {
                    bone = new Texture2D(1, 1),
                    shape = new Texture2D(1, 1)
                };
                texs.bone.name = "Bone";
                texs.shape.name = "Shape";

                foreach (var mat in player.sharedMaterials)
                {
                    mat.shader = mToonVersionShader;
                    mat.SetTexture("_Bone", texs.bone);
                    mat.SetTexture("_Shape", texs.shape);
                }

                var skel = new Skeleton(animator);
                var morph = new Morph(animator);
                var layout = new MotionLayout(skel, morph);
                var gen = new MeshPlayerGen { skel = skel, morph = morph, layout = layout };
                var sources = new (Mesh, Transform[])[renderers.Length];
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    sources[rendererIndex] = renderers[rendererIndex] switch
                    {
                        SkinnedMeshRenderer smr => (smr.sharedMesh, smr.bones),
                        MeshRenderer mr => (mr.GetComponent<MeshFilter>().sharedMesh, new[] { mr.transform }),
                        // treated as a quad
                        _ => (Resources.GetBuiltinResource<Mesh>("Quad.fbx"), new[] { renderers[rendererIndex].transform }),
                    };
                }

                gen.CreatePlayer(mesh, texs.bone, texs.shape, sources);

                // make bounds rotational invariant and extend by motion radius
                const float motionRadius = 4;
                var size = mesh.bounds.size;
                var sizeXZ = Mathf.Max(size.x, size.z) + 2 * motionRadius;
                mesh.bounds = new Bounds(mesh.bounds.center, new Vector3(sizeXZ, size.y, sizeXZ));
            }
            return player;
        }

        static void CopySettings(Renderer src, Renderer dst)
        {
            dst.lightProbeUsage = src.lightProbeUsage;
            dst.reflectionProbeUsage = src.reflectionProbeUsage;
            dst.shadowCastingMode = src.shadowCastingMode;
            dst.receiveShadows = src.receiveShadows;
            dst.motionVectorGenerationMode = src.motionVectorGenerationMode;
            dst.allowOcclusionWhenDynamic = src.allowOcclusionWhenDynamic;
        }
    }
}
#endif