// C#
using UnityEngine;
using System.Collections.Generic;

namespace VRGIN.Helpers
{
    public static class MeshExtension
    {
        public static Vector3 GetBarycentric(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 p)
        {
            Vector3 B = new Vector3();
            B.x = ((v2.y - v3.y) * (p.x - v3.x) + (v3.x - v2.x) * (p.y - v3.y)) /
                ((v2.y - v3.y) * (v1.x - v3.x) + (v3.x - v2.x) * (v1.y - v3.y));
            B.y = ((v3.y - v1.y) * (p.x - v3.x) + (v1.x - v3.x) * (p.y - v3.y)) /
                ((v3.y - v1.y) * (v2.x - v3.x) + (v1.x - v3.x) * (v2.y - v3.y));
            B.z = 1 - B.x - B.y;
            return B;
        }

        public static bool InTriangle(Vector3 barycentric)
        {
            return (barycentric.x >= 0.0f) && (barycentric.x <= 1.0f)
                && (barycentric.y >= 0.0f) && (barycentric.y <= 1.0f)
                && (barycentric.z >= 0.0f); //(barycentric.z <= 1.0f)
        }

        public static Vector3[] GetMappedPoints(this Mesh aMesh, Vector2 aUVPos)
        {
            List<Vector3> result = new List<Vector3>();
            Vector3[] verts = aMesh.vertices;
            Vector2[] uvs = aMesh.uv;
            int[] indices = aMesh.triangles;
            for (int i = 0; i < indices.Length; i += 3)
         {
                int i1 = indices[i];
                int i2 = indices[i + 1];
                int i3 = indices[i + 2];
                Vector3 bary = GetBarycentric(uvs[i1], uvs[i2], uvs[i3], aUVPos);
                if (InTriangle(bary))
                {
                    Vector3 localP = bary.x * verts[i1] + bary.y * verts[i2] + bary.z * verts[i3];
                    result.Add(localP);
                }
            }
            return result.ToArray();
        }
    }
}
