#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MaterialProps = System.Collections.Generic.Dictionary<string, UnityEditor.MaterialProperty>;

namespace VoyageVoyage
{
    class VoyageLiltoonConverter
    {
        private static void GetReadableTexture(ref Texture2D tex)
        {
            if (tex == null) return;

#if UNITY_2018_3_OR_NEWER
            if (!tex.isReadable)
#endif
            {
                var bufRT = RenderTexture.active;
                var texR = RenderTexture.GetTemporary(tex.width, tex.height);
                Graphics.Blit(tex, texR);
                RenderTexture.active = texR;
                tex = new Texture2D(texR.width, texR.height);
                tex.ReadPixels(new Rect(0, 0, texR.width, texR.height), 0, 0);
                tex.Apply();
                RenderTexture.active = bufRT;
                RenderTexture.ReleaseTemporary(texR);
            }
        }

        public static void LoadTexture(ref Texture2D tex, string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            GetReadableTexture(ref tex);
            if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = File.ReadAllBytes(Path.GetFullPath(path));
                tex.LoadImage(bytes);
            }

            if (tex != null) tex.filterMode = FilterMode.Bilinear;
        }

        public static void RunBake(ref Texture2D outTexture, Texture2D srcTexture, Material material, Texture2D referenceTexture = null)
        {
            int width = 4096;
            int height = 4096;
            if (referenceTexture != null)
            {
                width = referenceTexture.width;
                height = referenceTexture.height;
            }
            else if (srcTexture != null)
            {
                width = srcTexture.width;
                height = srcTexture.height;
            }
            outTexture = new Texture2D(width, height);

            var bufRT = RenderTexture.active;
            var dstTexture = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(srcTexture, dstTexture, material);
            RenderTexture.active = dstTexture;
            outTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            outTexture.Apply();
            RenderTexture.active = bufRT;
            RenderTexture.ReleaseTemporary(dstTexture);
        }

        static private void CopyTextureSetting(Texture2D fromTexture, Texture2D toTexture)
        {
            if (fromTexture == null || toTexture == null) return;
            string fromPath = AssetDatabase.GetAssetPath(fromTexture);
            string toPath = AssetDatabase.GetAssetPath(toTexture);
            var fromTextureImporter = (TextureImporter)AssetImporter.GetAtPath(fromPath);
            var toTextureImporter = (TextureImporter)AssetImporter.GetAtPath(toPath);
            if (fromTextureImporter == null || toTextureImporter == null) return;

            var fromTextureImporterSettings = new TextureImporterSettings();
            fromTextureImporter.ReadTextureSettings(fromTextureImporterSettings);
            toTextureImporter.SetTextureSettings(fromTextureImporterSettings);
            toTextureImporter.SetPlatformTextureSettings(fromTextureImporter.GetDefaultPlatformTextureSettings());
            AssetDatabase.ImportAsset(toPath);
        }

        static Texture2D SaveTextureToPng(Material material, Texture2D tex, string texname, string customName = "")
        {
            string path = AssetDatabase.GetAssetPath(material.GetTexture(texname));
            if (string.IsNullOrEmpty(path)) path = AssetDatabase.GetAssetPath(material);
            path += "_1.png";

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.Refresh();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path.Substring(path.IndexOf("Assets")));
            }
            else
            {
                return (Texture2D)material.GetTexture(texname);
            }
        }

        static public Texture2D AutoBakeMainTexture(Material material, MaterialProps props)
        {

            bool bake2nd = props["_UseMain2ndTex"].floatValue != 0.0;
            bool bake3rd = props["_UseMain3rdTex"].floatValue != 0.0;
            // run bake
            var mainTex = props["_MainTex"];
            var bufMainTexture = mainTex.textureValue as Texture2D;

            var hsvgMaterial = new Material(Shader.Find("Hidden/ltsother_baker"));

            string path;

            var srcTexture = new Texture2D(2, 2);
            var srcMain2 = new Texture2D(2, 2);
            var srcMain3 = new Texture2D(2, 2);
            var srcMask2 = new Texture2D(2, 2);
            var srcMask3 = new Texture2D(2, 2);

            MaterialProperty materialProperty = new MaterialProperty();

            hsvgMaterial.SetFollowingProperties(props, "_Color", "_MainTexHSVG", "_MainGradationStrength", "_MainGradationTex", "_MainColorAdjustMask");

            path = AssetDatabase.GetAssetPath(bufMainTexture);
            if (!string.IsNullOrEmpty(path))
            {
                LoadTexture(ref srcTexture, path);
                hsvgMaterial.SetTexture("_MainTex", srcTexture);
            }
            else
            {
                hsvgMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
            }

            if (bake2nd)
            {
                var main2ndTex = props["_Main2ndTex"].textureValue;
                var main2ndBlendMask = props["_Main2ndBlendMask"].floatValue;

                hsvgMaterial.SetFollowingProperties(props,
                    "_UseMain2ndTex",
                    "_Color2nd",
                    "_Main2ndTexAngle",
                    "_Main2ndTexDecalAnimation",
                    "_Main2ndTexDecalSubParam",
                    "_Main2ndTexIsDecal",
                    "_Main2ndTexIsLeftOnly",
                    "_Main2ndTexIsRightOnly",
                    "_Main2ndTexShouldCopy",
                    "_Main2ndTexShouldFlipMirror",
                    "_Main2ndTexShouldFlipCopy",
                    "_Main2ndTexIsMSDF",
                    "_Main2ndTexBlendMode",
                    "_Main2ndTexAlphaMode");


                hsvgMaterial.SetTextureOffset("_Main2ndTex", material.GetTextureOffset("_Main2ndTex"));
                hsvgMaterial.SetTextureScale("_Main2ndTex", material.GetTextureScale("_Main2ndTex"));
                hsvgMaterial.SetTextureOffset("_Main2ndBlendMask", material.GetTextureOffset("_Main2ndBlendMask"));
                hsvgMaterial.SetTextureScale("_Main2ndBlendMask", material.GetTextureScale("_Main2ndBlendMask"));

                path = AssetDatabase.GetAssetPath(material.GetTexture("_Main2ndTex"));
                if (!string.IsNullOrEmpty(path))
                {
                    LoadTexture(ref srcMain2, path);
                    hsvgMaterial.SetTexture("_Main2ndTex", srcMain2);
                }
                else
                {
                    hsvgMaterial.SetTexture("_Main2ndTex", Texture2D.whiteTexture);
                }

                path = AssetDatabase.GetAssetPath(material.GetTexture("_Main2ndBlendMask"));
                if (!string.IsNullOrEmpty(path))
                {
                    LoadTexture(ref srcMask2, path);
                    hsvgMaterial.SetTexture("_Main2ndBlendMask", srcMask2);
                }
                else
                {
                    hsvgMaterial.SetTexture("_Main2ndBlendMask", Texture2D.whiteTexture);
                }
            }

            if (bake3rd)
            {
                var main3rdTex = props["_Main3rdTex"].textureValue;
                var main3rdBlendMask = props["_Main3rdBlendMask"].floatValue;

                hsvgMaterial.SetFollowingProperties(props,
                    "_UseMain3rdTex",
                    "_Color3rd",
                    "_Main3rdTexAngle",
                    "_Main3rdTexDecalAnimation",
                    "_Main3rdTexDecalSubParam",
                    "_Main3rdTexIsDecal",
                    "_Main3rdTexIsLeftOnly",
                    "_Main3rdTexIsRightOnly",
                    "_Main3rdTexShouldCopy",
                    "_Main3rdTexShouldFlipMirror",
                    "_Main3rdTexShouldFlipCopy",
                    "_Main3rdTexIsMSDF",
                    "_Main3rdTexBlendMode",
                    "_Main3rdTexAlphaMode");


                hsvgMaterial.SetTextureOffset("_Main3rdTex", material.GetTextureOffset("_Main3rdTex"));
                hsvgMaterial.SetTextureScale("_Main3rdTex", material.GetTextureScale("_Main3rdTex"));
                hsvgMaterial.SetTextureOffset("_Main3rdBlendMask", material.GetTextureOffset("_Main3rdBlendMask"));
                hsvgMaterial.SetTextureScale("_Main3rdBlendMask", material.GetTextureScale("_Main3rdBlendMask"));

                path = AssetDatabase.GetAssetPath(material.GetTexture("_Main3rdTex"));
                if (!string.IsNullOrEmpty(path))
                {
                    LoadTexture(ref srcMain2, path);
                    hsvgMaterial.SetTexture("_Main3rdTex", srcMain2);
                }
                else
                {
                    hsvgMaterial.SetTexture("_Main3rdTex", Texture2D.whiteTexture);
                }

                path = AssetDatabase.GetAssetPath(material.GetTexture("_Main3rdBlendMask"));
                if (!string.IsNullOrEmpty(path))
                {
                    LoadTexture(ref srcMask2, path);
                    hsvgMaterial.SetTexture("_Main3rdBlendMask", srcMask2);
                }
                else
                {
                    hsvgMaterial.SetTexture("_Main3rdBlendMask", Texture2D.whiteTexture);
                }
            }

            Texture2D outTexture = null;
            RunBake(ref outTexture, srcTexture, hsvgMaterial);

            outTexture = SaveTextureToPng(material, outTexture, "_MainTex");
            if (outTexture != mainTex.textureValue)
            {
                CopyTextureSetting(bufMainTexture, outTexture);
            }

            UnityEngine.Object.DestroyImmediate(hsvgMaterial);
            UnityEngine.Object.DestroyImmediate(srcTexture);
            UnityEngine.Object.DestroyImmediate(srcMain2);
            UnityEngine.Object.DestroyImmediate(srcMain3);
            UnityEngine.Object.DestroyImmediate(srcMask2);
            UnityEngine.Object.DestroyImmediate(srcMask3);

            return outTexture;
        }

        static private Texture AutoBakeShadowTexture(Material material, MaterialProps props, Texture bakedMainTex, int shadowType = 0, bool shouldShowDialog = true)
        {

            bool shouldNotBakeAll = props["_UseShadow"].floatValue == 0.0 && props["_ShadowColor"].colorValue == Color.white && props["_ShadowColorTex"].textureValue == null && props["_ShadowStrengthMask"].textureValue == null;
            bool shouldBake = true;
            if (!shouldNotBakeAll && shouldBake)
            {
                // run bake
                var bufMainTexture = bakedMainTex as Texture2D;
                var hsvgMaterial = new Material(Shader.Find("Hidden/ltsother_baker"));

                string path;

                var srcTexture = new Texture2D(2, 2);
                var srcMain2 = new Texture2D(2, 2);
                var srcMask2 = new Texture2D(2, 2);

                hsvgMaterial.SetColor("_Color", Color.white);
                hsvgMaterial.SetVector("_MainTexHSVG", new Vector4(0.0f, 1.0f, 1.0f, 1.0f));
                hsvgMaterial.SetFloat("_UseMain2ndTex", 1.0f);
                hsvgMaterial.SetFloat("_UseMain3rdTex", 1.0f);
                hsvgMaterial.SetColor("_Color3rd", new Color(1.0f, 1.0f, 1.0f, props["_ShadowMainStrength"].floatValue));
                hsvgMaterial.SetFloat("_Main3rdTexBlendMode", 3.0f);
                if (shadowType == 2)
                {
                    Color shadow2ndColor = props["_Shadow2ndColor"].colorValue;
                    hsvgMaterial.SetColor("_Color2nd", new Color(shadow2ndColor.r, shadow2ndColor.g, shadow2ndColor.b, shadow2ndColor.a * props["_ShadowStrength"].floatValue));
                    hsvgMaterial.SetFloat("_Main2ndTexBlendMode", 0.0f);
                    hsvgMaterial.SetFloat("_Main2ndTexAlphaMode", 0.0f);
                    path = AssetDatabase.GetAssetPath(material.GetTexture("_Shadow2ndColorTex"));
                }
                else if (shadowType == 3)
                {
                    Color shadow3rdColor = props["_Shadow3rdColor"].colorValue;
                    hsvgMaterial.SetColor("_Color3rd", new Color(shadow3rdColor.r, shadow3rdColor.g, shadow3rdColor.b, shadow3rdColor.a * props["_ShadowStrength"].floatValue));
                    hsvgMaterial.SetFloat("_Main3rdTexBlendMode", 0.0f);
                    hsvgMaterial.SetFloat("_Main3rdTexAlphaMode", 0.0f);
                    path = AssetDatabase.GetAssetPath(material.GetTexture("_Shadow3rdColorTex"));
                }
                else
                {
                    Color shadowColor = props["_ShadowColor"].colorValue;
                    hsvgMaterial.SetColor("_Color2nd", new Color(shadowColor.r, shadowColor.g, shadowColor.b, props["_ShadowStrength"].floatValue));
                    hsvgMaterial.SetFloat("_Main2ndTexBlendMode", 0.0f);
                    hsvgMaterial.SetFloat("_Main2ndTexAlphaMode", 0.0f);
                    path = AssetDatabase.GetAssetPath(material.GetTexture("_ShadowColorTex"));
                }

                bool existsShadowTex = !string.IsNullOrEmpty(path);
                if (existsShadowTex)
                {
                    LoadTexture(ref srcMain2, path);
                    hsvgMaterial.SetTexture("_Main2ndTex", srcMain2);
                }

                path = AssetDatabase.GetAssetPath(bakedMainTex);
                if (!string.IsNullOrEmpty(path))
                {
                    LoadTexture(ref srcTexture, path);
                    hsvgMaterial.SetTexture("_MainTex", srcTexture);
                    hsvgMaterial.SetTexture("_Main3rdTex", srcTexture);
                    if (!existsShadowTex) hsvgMaterial.SetTexture("_Main2ndTex", srcTexture);
                }
                else
                {
                    hsvgMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
                    hsvgMaterial.SetTexture("_Main3rdTex", Texture2D.whiteTexture);
                    if (!existsShadowTex) hsvgMaterial.SetTexture("_Main2ndTex", Texture2D.whiteTexture);
                }

                path = AssetDatabase.GetAssetPath(material.GetTexture("_ShadowStrengthMask"));
                if (!string.IsNullOrEmpty(path))
                {
                    LoadTexture(ref srcMask2, path);
                    hsvgMaterial.SetTexture("_Main2ndBlendMask", srcMask2);
                    hsvgMaterial.SetTexture("_Main3rdBlendMask", srcMask2);
                }
                else
                {
                    hsvgMaterial.SetTexture("_Main2ndBlendMask", Texture2D.whiteTexture);
                    hsvgMaterial.SetTexture("_Main3rdBlendMask", Texture2D.whiteTexture);
                }

                Texture2D outTexture = null;
                RunBake(ref outTexture, srcTexture, hsvgMaterial);

                if (shadowType == 0) outTexture = SaveTextureToPng(material, outTexture, "_MainTex");
                if (shadowType == 1) outTexture = SaveTextureToPng(material, outTexture, "_MainTex", "_shadow_1st");
                if (shadowType == 2) outTexture = SaveTextureToPng(material, outTexture, "_MainTex", "_shadow_2nd");
                if (outTexture != props["_MainTex"].textureValue)
                {
                    CopyTextureSetting(bufMainTexture, outTexture);
                }

                UnityEngine.Object.DestroyImmediate(hsvgMaterial);
                UnityEngine.Object.DestroyImmediate(srcTexture);
                UnityEngine.Object.DestroyImmediate(srcMain2);
                UnityEngine.Object.DestroyImmediate(srcMask2);

                return outTexture;
            }
            else
            {
                return (Texture2D)props["_MainTex"].textureValue;
            }
        }

        public static bool IsOutlineShaderName(string shaderName)
        {
            var separatorIndex = shaderName.LastIndexOf('/');
            if (separatorIndex == -1 || separatorIndex + 1 == shaderName.Length)
            {
                return false;
            }

            if (shaderName.IndexOf("Outline", separatorIndex + 1) != -1)
            {
                return true;
            }

            // For following custom shader names.
            // - *LIL_SHADER_NAME*/[Optional] OutlineOnly/Opaque
            // - *LIL_SHADER_NAME*/[Optional] OutlineOnly/Cutout
            // - *LIL_SHADER_NAME*/[Optional] OutlineOnly/Transparent
            var partIndex = shaderName.LastIndexOf("/[Optional] OutlineOnly/");
            if (partIndex != -1 && partIndex + 23 == separatorIndex)
            {
                return true;
            }

            return false;
        }

        static private Texture AutoBakeMatCap(Material material, MaterialProps props)
        {
            bool shouldNotBakeAll = props["_MatCapColor"].colorValue == Color.white;
            if (!shouldNotBakeAll)
            {
                // run bake
                var bufMainTexture = props["_MatCapTex"].textureValue as Texture2D;
                var hsvgMaterial = new Material(Shader.Find("Hidden/ltsother_baker"));

                string path;

                var srcTexture = new Texture2D(2, 2);

                hsvgMaterial.SetColor("_Color", props["_MatCapColor"].colorValue);
                hsvgMaterial.SetVector("_MainTexHSVG", new Vector4(0.0f, 1.0f, 1.0f, 1.0f));

                path = AssetDatabase.GetAssetPath(material.GetTexture(props["_MatCapTex"].name));
                if (!string.IsNullOrEmpty(path))
                {
                    LoadTexture(ref srcTexture, path);
                    hsvgMaterial.SetTexture("_MainTex", srcTexture);
                }
                else
                {
                    hsvgMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
                }

                Texture2D outTexture = null;
                RunBake(ref outTexture, srcTexture, hsvgMaterial);

                outTexture = SaveTextureToPng(material, outTexture, props["_MatCapTex"].name);
                if (outTexture != props["_MatCapTex"].textureValue)
                {
                    CopyTextureSetting(bufMainTexture, outTexture);
                }

                UnityEngine.Object.DestroyImmediate(hsvgMaterial);
                UnityEngine.Object.DestroyImmediate(srcTexture);

                return outTexture;
            }
            else
            {
                return props["_MatCapTex"].textureValue;
            }
        }

        static public Material LiltoonToMToonMaterial(Material original)
        {
            var shaderName = original.shader.name.ToLower();
            var mtoonMaterial = new Material(Shader.Find("GLBLoader/MToon10"));
            mtoonMaterial.name = original.name;
            MaterialProps props = original.GetPropertiesAsDictionary();

            bool isCutout = shaderName.Contains("cutout");
            bool isTransparent = shaderName.Contains("trans");
            bool isOutline = IsOutlineShaderName(shaderName);

            Debug.Log($"[LiltoonToMToonMaterial] Shader : {original.shader.name} - Cutout : {isCutout} - Transparent : {isTransparent}");

            /*string matPath = AssetDatabase.GetAssetPath(original);
            if (!string.IsNullOrEmpty(matPath)) matPath = EditorUtility.SaveFilePanel("Save Material", Path.GetDirectoryName(matPath), Path.GetFileNameWithoutExtension(matPath) + "_mtoon", "mat");
            else matPath = EditorUtility.SaveFilePanel("Save Material", "Assets", original.name + ".mat", "mat");
            if (!string.IsNullOrEmpty(matPath)) AssetDatabase.CreateAsset(mtoonMaterial, FileUtil.GetProjectRelativePath(matPath));*/

            mtoonMaterial.SetColor("_Color", props["_Color"].colorValue.Clamp01());
            mtoonMaterial.SetFloat("_LightColorAttenuation", 0.0f);
            mtoonMaterial.SetFloat("_IndirectLightIntensity", 0.0f);

            Vector4 mainTex_ScrollRotate = props["_MainTex_ScrollRotate"].vectorValue;
            mtoonMaterial.SetFloat("_UvAnimScrollX", mainTex_ScrollRotate.x);
            mtoonMaterial.SetFloat("_UvAnimScrollY", mainTex_ScrollRotate.y);
            mtoonMaterial.SetFloat("_UvAnimRotation", mainTex_ScrollRotate.w / Mathf.PI * 0.5f);
            //mtoonMaterial.SetFloat("_MToonVersion", 35.0f);
            mtoonMaterial.SetFloat("_DebugMode", 0.0f);

            mtoonMaterial.SetProperty("_CullMode", props["_Cull"]);

            var bakedMainTex = AutoBakeMainTexture(original, props);
            mtoonMaterial.SetTexture("_MainTex", bakedMainTex);

            var mainScale = original.GetTextureScale("_MainTex");
            var mainOffset = original.GetTextureOffset("_MainTex");
            mtoonMaterial.SetTextureScale("_MainTex", mainScale);
            mtoonMaterial.SetTextureOffset("_MainTex", mainOffset);

            var bumpMap = props["_BumpMap"].textureValue;
            if (props["_UseBumpMap"].floatValue == 1.0f && bumpMap != null)
            {
                mtoonMaterial.SetFloat("_BumpScale", props["_BumpScale"].floatValue);
                mtoonMaterial.SetTexture("_BumpMap", bumpMap);
                mtoonMaterial.EnableKeyword("_NORMALMAP");
            }

            if (props["_UseShadow"].floatValue == 1.0f)
            {
                float shadowBorder = props["_ShadowBorder"].floatValue;
                float shadowBlur = props["_ShadowBlur"].floatValue;
                Color shadowColor = props["_ShadowColor"].colorValue;
                float shadowMainStrength = props["_ShadowMainStrength"].floatValue;
                float shadowStrength = props["_ShadowStrength"].floatValue;


                float shadeShift = (Mathf.Clamp01(shadowBorder - (shadowBlur * 0.5f)) * 2.0f) - 1.0f;
                float shadeToony = shadeShift == 1.0f ? 1.0f : (2.0f - (Mathf.Clamp01(shadowBorder + (shadowBlur * 0.5f)) * 2.0f)) / (1.0f - shadeShift);


                if (props["_ShadowStrengthMask"].textureValue != null || shadowMainStrength != 0.0f)
                {
                    var bakedShadowTex = AutoBakeShadowTexture(original, props, bakedMainTex);
                    mtoonMaterial.SetColor("_ShadeColor", Color.white);
                    mtoonMaterial.SetTexture("_ShadeTexture", bakedShadowTex);
                }
                else
                {
                    var shadeColorStrength = new Color(
                        1.0f - ((1.0f - shadowColor.r) * shadowStrength),
                        1.0f - ((1.0f - shadowColor.g) * shadowStrength),
                        1.0f - ((1.0f - shadowColor.b) * shadowStrength),
                        1.0f
                    );
                    mtoonMaterial.SetColor("_ShadeColor", shadeColorStrength);
                    Texture shadowColorTex = props["_ShadowColorTex"].textureValue;
                    if (shadowColorTex != null)
                    {
                        mtoonMaterial.SetTexture("_ShadeTexture", shadowColorTex);
                    }
                    else
                    {
                        mtoonMaterial.SetTexture("_ShadeTexture", bakedMainTex);
                    }
                }
                mtoonMaterial.SetFloat("_ReceiveShadowRate", 1.0f);
                mtoonMaterial.SetTexture("_ReceiveShadowTexture", null);
                mtoonMaterial.SetFloat("_ShadingGradeRate", 1.0f);
                mtoonMaterial.SetTexture("_ShadingGradeTexture", props["_ShadowBorderMask"].textureValue);
                mtoonMaterial.SetFloat("_ShadeShift", shadeShift);
                mtoonMaterial.SetFloat("_ShadeToony", shadeToony);
            }
            else
            {
                mtoonMaterial.SetColor("_ShadeColor", Color.white);
                mtoonMaterial.SetTexture("_ShadeTexture", bakedMainTex);
            }

            if (props["_UseEmission"].floatValue == 1.0f && props["_EmissionMap"].textureValue != null)
            {
                mtoonMaterial.SetColor("_EmissionColor", props["_EmissionColor"].colorValue);
                mtoonMaterial.SetTexture("_EmissionMap", props["_EmissionMap"].textureValue);
            }

            if (props["_UseRim"].floatValue == 1.0f)
            {
                mtoonMaterial.SetColor("_RimColor", props["_RimColor"].colorValue);
                mtoonMaterial.SetTexture("_RimTexture", props["_RimColorTex"].textureValue);
                mtoonMaterial.SetFloat("_RimLightingMix", props["_RimEnableLighting"].floatValue);

                float rimBlur = props["_RimBlur"].floatValue;
                float rimFresnelPower = props["_RimFresnelPower"].floatValue;
                float rimBorder = props["_RimBorder"].floatValue;

                float rimFP = rimFresnelPower / Mathf.Max(0.001f, rimBlur);
                float rimLift = Mathf.Pow(1.0f - rimBorder, rimFresnelPower) * (1.0f - rimBlur);
                mtoonMaterial.SetFloat("_RimFresnelPower", rimFP);
                mtoonMaterial.SetFloat("_RimLift", rimLift);
            }
            else
            {
                mtoonMaterial.SetColor("_RimColor", Color.black);
            }

            if (props["_UseMatCap"].floatValue == 1.0f && props["_MatCapBlendMode"].floatValue != 3.0f && props["_MatCapTex"].textureValue != null)
            {
                var bakedMatCap = AutoBakeMatCap(original, props);
                mtoonMaterial.SetTexture("_SphereAdd", bakedMatCap);
            }

            if (isOutline)
            {
                mtoonMaterial.SetTexture("_OutlineWidthTexture", props["_OutlineWidthMask"].textureValue);
                mtoonMaterial.SetFloat("_OutlineWidth", props["_OutlineWidth"].floatValue);
                mtoonMaterial.SetColor("_OutlineColor", props["_OutlineColor"].colorValue);
                mtoonMaterial.SetFloat("_OutlineLightingMix", 1.0f);
                mtoonMaterial.SetFloat("_OutlineWidthMode", 1.0f);
                mtoonMaterial.SetFloat("_OutlineColorMode", 1.0f);
                mtoonMaterial.SetFloat("_OutlineCullMode", 1.0f);
                mtoonMaterial.EnableKeyword("MTOON_OUTLINE_WIDTH_WORLD");
                mtoonMaterial.EnableKeyword("MTOON_OUTLINE_COLOR_MIXED");
            }

            if (isCutout)
            {
                Debug.Log("WRITING ALPHA MODE TO CUTOUT !");
                mtoonMaterial.SetFloat("_AlphaMode", 1.0f);
                mtoonMaterial.SetFloat("_Cutoff", props["_Cutoff"].floatValue);
                mtoonMaterial.SetFloat("_BlendMode", 1.0f);
                mtoonMaterial.SetOverrideTag("RenderType", "TransparentCutout");
                mtoonMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mtoonMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                mtoonMaterial.SetFloat("_ZWrite", 1.0f);
                mtoonMaterial.SetFloat("_AlphaToMask", 1.0f);
                mtoonMaterial.EnableKeyword("_ALPHATEST_ON");
                mtoonMaterial.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else if (isTransparent && props["_ZWrite"].floatValue == 0.0f)
            {
                Debug.Log("WRITING ALPHA MODE TO TRANSPARENT !");
                mtoonMaterial.SetFloat("_AlphaMode", 2.0f);
                mtoonMaterial.SetFloat("_BlendMode", 2.0f);
                mtoonMaterial.SetOverrideTag("RenderType", "TransparentCutout");
                mtoonMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mtoonMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mtoonMaterial.SetFloat("_ZWrite", 0.0f);
                mtoonMaterial.SetFloat("_AlphaToMask", 0.0f);
                mtoonMaterial.EnableKeyword("_ALPHABLEND_ON");
                mtoonMaterial.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (isTransparent && props["_ZWrite"].floatValue != 0.0f)
            {
                mtoonMaterial.SetFloat("_AlphaMode", 2.0f);
                mtoonMaterial.SetFloat("_BlendMode", 3.0f);
                mtoonMaterial.SetOverrideTag("RenderType", "TransparentCutout");
                mtoonMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mtoonMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mtoonMaterial.SetFloat("_ZWrite", 1.0f);
                mtoonMaterial.SetFloat("_AlphaToMask", 0.0f);
                mtoonMaterial.EnableKeyword("_ALPHABLEND_ON");
                mtoonMaterial.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                mtoonMaterial.SetFloat("_AlphaMode", 0.0f);
                mtoonMaterial.SetFloat("_BlendMode", 0.0f);
                mtoonMaterial.SetOverrideTag("RenderType", "Opaque");
                mtoonMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mtoonMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                mtoonMaterial.SetFloat("_ZWrite", 1.0f);
                mtoonMaterial.SetFloat("_AlphaToMask", 0.0f);
                mtoonMaterial.renderQueue = -1;
            }
            return mtoonMaterial;
        }

    }
}
#endif