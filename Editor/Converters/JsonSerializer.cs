#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace VoyageVoyage
{
    /**
     * Vibe coded JSON Serializer since I run into various trouble with Newton JSON Serializer
     * This just does what's needed for filling GLTF JSON data.
     * Objects are expected to be Dictionary<string, object>
     * Array are expected to be object[]
     */
    public class JsonSerializer
    {
        public string Serialize(object obj)
        {
            return SerializeValue(obj);
        }

        private string SerializeValue(object value)
        {
            switch (value)
            {
                case null:
                    return "null";
                case string s:
                    return SerializeString(s);
                case double d:
                    return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case int i:
                    return i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case float[] floatArray:
                    return SerializeArray(floatArray);
                case bool b:
                    return b ? "true" : "false";
                case List<object> arr:
                    return SerializeArray(arr);
                case Dictionary<string, object> dict:
                    return SerializeObject(dict);
                default:
                    throw new InvalidOperationException($"Unsupported type: {value.GetType().Name}");
            }
        }

        private string SerializeString(string s)
        {
            var sb = new StringBuilder();
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (char.IsControl(c))
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private string SerializeArray(List<object> array)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < array.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append(SerializeValue(array[i]));
            }
            sb.Append(']');
            return sb.ToString();
        }

        private string SerializeArray(Array array)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < array.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append(SerializeValue(array.GetValue(i)));
            }
            sb.Append(']');
            return sb.ToString();
        }

        private string SerializeObject(Dictionary<string, object> obj)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            bool first = true;
            foreach (var kvp in obj)
            {
                if (!first)
                    sb.Append(',');
                first = false;

                sb.Append(SerializeString(kvp.Key));
                sb.Append(':');
                sb.Append(SerializeValue(kvp.Value));
            }
            sb.Append('}');
            return sb.ToString();
        }
    }

    public static class UnityHelpers
    {
        public static float[] AsFloat(this Vector2 vector2)
        {
            return new float[] { vector2.x, vector2.y };
        }

        public static float[] AsFloat(this Vector3 vector3)
        {
            return new float[] { vector3.x, vector3.y, vector3.z };
        }

        public static float[] AsFloat(this Vector4 vector4)
        {
            return new float[] { vector4.x, vector4.y, vector4.z, vector4.w };
        }

        public static float[][] AsFloat(this Vector2[] vector2)
        {
            int nVectors = vector2.Length;
            float[][] ret = new float[nVectors][];
            for (int i = 0; i < nVectors; i++)
            {
                ret[i] = AsFloat(vector2[i]);
            }
            return ret;
        }

        public static float[][] AsFloat(this Vector3[] vector3)
        {
            int nVectors = vector3.Length;
            float[][] ret = new float[nVectors][];
            for (int i = 0; i < nVectors; i++)
            {
                ret[i] = AsFloat(vector3[i]);
            }
            return ret;
        }

        public static float[][] AsFloat(this Vector4[] vector4)
        {
            int nVectors = vector4.Length;
            float[][] ret = new float[nVectors][];
            for (int i = 0; i < nVectors; i++)
            {
                ret[i] = AsFloat(vector4[i]);
            }
            return ret;
        }

    }

}

#endif