#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace VoyageVoyageShaderMotion
{
    public class MeshUtil
    {

        public static void WriteToCSV(BoneWeight[] array, string fileName)
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write header
                writer.WriteLine("index0,weight0,index1,weight1,index2,weight2,index3,weight3");

                foreach (BoneWeight weight in array)
                {

                    string line = $"{weight.boneIndex0},{weight.weight0},{weight.boneIndex1},{weight.weight1},{weight.boneIndex2},{weight.weight2},{weight.boneIndex3},{weight.weight3}";
                    writer.WriteLine(line);
                }
            }

        }
        public static void WriteToCSV(Vector4[] array, string fileName)
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write header
                writer.WriteLine("x,y,z,w");

                foreach (Vector4 v in array)
                {
                    writer.WriteLine($"{v.x}|{v.y}|{v.z}|{v.w}");
                }
            }

        }

        public static void WriteToCSV(List<Vector4> array, string fileName)
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write header
                writer.WriteLine("x,y,z,w");

                foreach (Vector4 v in array)
                {
                    writer.WriteLine($"{v.x}|{v.y}|{v.z}|{v.w}");
                }
            }

        }

        public static void WriteToCSV(int[] values, string fileName)
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    writer.WriteLine($"{i},{values[i]}");
                }
            }

        }

        public static void WriteToCSV(Transform[] values, string filename)
        {
            string filePath = Path.Combine(Application.persistentDataPath, filename);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    Transform t = values[i];
                    if (t == null)
                    {
                        writer.WriteLine($"{i},null");
                        continue;
                    }

                    writer.WriteLine($"{i},{t.position},{t.rotation}");
                }
            }
        }

        static (int index, float weight)[] UnpackBoneWeight(BoneWeight bw)
        {
            return new[]{
            (bw.boneIndex0, bw.weight0),
            (bw.boneIndex1, bw.weight1),
            (bw.boneIndex2, bw.weight2),
            (bw.boneIndex3, bw.weight3)};
        }
        static BoneWeight PackBoneWeight((int index, float weight)[] bw)
        {
            return new BoneWeight
            {
                boneIndex0 = bw[0].index,
                weight0 = bw[0].weight,
                boneIndex1 = bw[1].index,
                weight1 = bw[1].weight,
                boneIndex2 = bw[2].index,
                weight2 = bw[2].weight,
                boneIndex3 = bw[3].index,
                weight3 = bw[3].weight
            };
        }
        public static int[] RetargetBones(Transform[] srcBones, Transform[] dstBones)
        {
            /* Initialize an array of 'srcBones.Length' to -1 */
            int[] boneMap = Enumerable.Repeat(-1, srcBones.Length).ToArray();

            for (int srcBoneIndex = 0; srcBoneIndex < srcBones.Length; srcBoneIndex++)
            {
                for (Transform boneOrParent = srcBones[srcBoneIndex];
                    boneOrParent != null && boneMap[srcBoneIndex] < 0;
                    boneOrParent = boneOrParent.parent)
                {
                    boneMap[srcBoneIndex] = System.Array.LastIndexOf(dstBones, boneOrParent);
                }
            }
            return boneMap;
        }
        static void RetargetBindposes(Transform[] srcBones, Transform[] dstBones,
                                        Matrix4x4[] srcBindposes, Matrix4x4[] dstBindposes, int[] boneMap)
        {
            for (int k = 0; k < 2; k++)
                for (int i = 0; i < srcBones.Length; i++)
                {
                    var j = boneMap[i];
                    if (j >= 0 && dstBindposes[j][3, 3] == 0)
                        if (k == 1)
                        {
                            dstBindposes[j] = (dstBones[j].worldToLocalMatrix * srcBones[i].localToWorldMatrix) * srcBindposes[i];
                            Debug.Log($"[Retarget] bindpose[{(HumanBodyBones)j}] = MAT * bindpose[{srcBones[i]?.name}]", srcBones[i]);
                        }
                        else if (dstBones[j] == srcBones[i])
                            dstBindposes[j] = srcBindposes[i];
                }
        }
        static Matrix4x4[] RetargetBoneWeights(
            Transform[] srcBones,
            Transform[] dstBones,
            Matrix4x4[] srcBindposes,
            Matrix4x4[] dstBindposes,
            BoneWeight[] boneWeights,
            int[] boneMap)
        {
            string filePath = Path.Combine(Application.persistentDataPath, $"SM_{boneWeights.Length}_test.csv");
            StreamWriter writer = new StreamWriter(filePath);
            Debug.Assert(srcBones.Length == srcBindposes.Length && dstBones.Length == dstBindposes.Length);
            var vertMatrices = new Matrix4x4[boneWeights.Length];



            for (int v = 0; v < boneWeights.Length; v++)
            {
                var bw = UnpackBoneWeight(boneWeights[v]);
                var weights = new float[dstBones.Length];
                var srcMatSum = new Matrix4x4();
                var dstMatSum = new Matrix4x4();
                foreach (var (i, wt) in bw)
                {

                    var j = boneMap[i];
                    if (wt != 0)
                    {
                        var srcMat = srcBones[i].localToWorldMatrix * srcBindposes[i];
                        var dstMat = dstBones[j].localToWorldMatrix * dstBindposes[j];



                        for (int k = 0; k < 16; k++)
                        {
                            srcMatSum[k] += srcMat[k] * wt;
                            dstMatSum[k] += dstMat[k] * wt;
                        }
                        weights[j] += wt;

                    }


                }


                if (srcMatSum != dstMatSum)
                {
                    var diffm = dstMatSum.inverse * srcMatSum;
                    var diffv = +(diffm.GetColumn(0) - new Vector4(1, 0, 0, 0)).sqrMagnitude
                                + (diffm.GetColumn(1) - new Vector4(0, 1, 0, 0)).sqrMagnitude
                                + (diffm.GetColumn(2) - new Vector4(0, 0, 1, 0)).sqrMagnitude
                                + (diffm.GetColumn(3) - new Vector4(0, 0, 0, 1)).sqrMagnitude;
                    if (diffv > 1e-8)
                        Debug.Log($"[Retarget] vertex = MAT * vertex, bones == {{{srcBones[boneWeights[v].boneIndex0]?.name}, {srcBones[boneWeights[v].boneIndex1]?.name}}}", srcBones[boneWeights[v].boneIndex0]);
                }


                System.Array.Clear(bw, 0, bw.Length);

                var idx = 0;
                var a = Enumerable.Range(0, dstBones.Length).OrderBy(i => -weights[i]).Take(4).ToArray();
                //writer.WriteLine($"{a[0]},{a[1]},{a[2]},{a[3]}");
                foreach (var dstBone in Enumerable.Range(0, dstBones.Length).OrderBy(i => -weights[i]).Take(4))
                    bw[idx++] = (dstBone, weights[dstBone]);

                vertMatrices[v] = srcMatSum == dstMatSum ? Matrix4x4.identity : dstMatSum.inverse * srcMatSum;
                var newBoneWeight = PackBoneWeight(bw);
                boneWeights[v] = newBoneWeight;

                //writer.WriteLine($"{v},{newBoneWeight.boneIndex0},{newBoneWeight.weight0},{newBoneWeight.boneIndex1},{newBoneWeight.weight1},{newBoneWeight.boneIndex2},{newBoneWeight.weight2},{newBoneWeight.boneIndex3},{newBoneWeight.weight3}");
            }
            writer.Close();

            return vertMatrices;
        }



        public static Matrix4x4[] RetargetBindposesBoneWeights(
            Transform[] srcBones,
            Transform[] dstBones,
            Matrix4x4[] srcBindposes,
            Matrix4x4[] dstBindposes,
            BoneWeight[] boneWeights)
        {
            int[] boneMap = RetargetBones(srcBones, dstBones);

            // unmap unused srcBones
            var used = new bool[srcBones.Length];
            foreach (var bw in boneWeights)
                foreach (var (index, weight) in UnpackBoneWeight(bw))
                    if (weight != 0)
                        used[index] = true;
            for (int i = 0; i < srcBones.Length; i++)
                if (!used[i])
                    boneMap[i] = -1;

            // retarget bindposes for mapped srcBones
            RetargetBindposes(srcBones, dstBones, srcBindposes, dstBindposes, boneMap);
            // map unmapped srcBones to first mapped bone
            var defaultBoneIndex = boneMap.Where(x => x >= 0).FirstOrDefault();
            for (int i = 0; i < boneMap.Length; i++)
                if (boneMap[i] < 0 && used[i])
                {
                    boneMap[i] = defaultBoneIndex;
                    Debug.LogWarning($"[Retarget] boneMap[{srcBones[i]?.name}] = {(HumanBodyBones)defaultBoneIndex} (default)", srcBones[i]);
                }
            return RetargetBoneWeights(srcBones, dstBones, srcBindposes, dstBindposes, boneWeights, boneMap);
        }
    }
}
#endif