using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Net3dBool
{
    /// <summary>
    /// 在并查集中表示三角面和所在平面的信息，确认邻接关系；
    /// 一个<see cref="FaceF"/>实例是并查集的一个成员
    /// </summary>
    public class FaceF
    {
        const float TOL = 1e-5f;

        FaceF root = null;
        public FaceF Root { get { return root == null ? this : root.Root; } }
        public bool IsRoot() { return root == null; }

        // 顶点集合
        readonly Vector3[] vertices;
        public Vector3[] Vertices { get { return IsRoot() ? vertices : Root.vertices; } }

        // 确定所在平面
        public Vector3 PlaneNormal { get; private set; }
        public float DisOrigin2plane { get; private set; }

        public float Area
        {
            get
            {
                return Vector3.Cross(Vertices[1] - Vertices[0], Vertices[2] - Vertices[0]).magnitude / 2;
            }
        }

        public FaceF(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            vertices = new Vector3[3] { p1, p2, p3 };
            PlaneNormal = Vector3.Cross(p2 - p1, p3 - p1).normalized;
            DisOrigin2plane = Vector3.Dot(PlaneNormal, p1);
        }
        private FaceF() { }

        public bool SamePlane(FaceF another)
        {
            return SameVector(PlaneNormal, another.PlaneNormal) && 
                Mathf.Abs(DisOrigin2plane - another.DisOrigin2plane) < TOL;
        }

        public bool SameFace(FaceF another)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (!SameVector(Vertices[i], another.Vertices[j])) { return false; }
                }
            }
            return true;
        }

        public void SetParent(FaceF parent)
        {
            if (IsRoot()) { root = parent.Root; }
            else { Root.SetParent(parent); }
        }

        static bool V3Collinear(Vector3 a, Vector3 b, Vector3 c)
        {
            return Mathf.Abs(Vector3.Cross(b - a, c - a).magnitude) < TOL;
        }

        public static bool SameVector(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) < TOL && Mathf.Abs(a.y - b.y) < TOL && Mathf.Abs(a.z - b.z) < TOL;
        }

        public static bool FaceClearOf(FaceF fa, FaceF fb, float epsilon = float.Epsilon)
        {
            Vector3 getMin(Vector3[] vs)
            {
                return new Vector3(Mathf.Min(vs[0].x, vs[1].x, vs[2].x), 
                    Mathf.Min(vs[0].y, vs[1].y, vs[2].y), Mathf.Min(vs[0].z, vs[1].z, vs[2].z));
            }
            Vector3 getMax(Vector3[] vs)
            {
                return new Vector3(Mathf.Max(vs[0].x, vs[1].x, vs[2].x),
                    Mathf.Max(vs[0].y, vs[1].y, vs[2].y), Mathf.Max(vs[0].z, vs[1].z, vs[2].z));
            }
            Vector3 faMin = getMin(fa.Vertices);
            Vector3 faMax = getMax(fa.Vertices);
            Vector3 fbMin = getMin(fb.Vertices);
            Vector3 fbMax = getMax(fb.vertices);

            return faMin.x > fbMax.x + epsilon || faMax.x < fbMin.x - epsilon ||
                faMin.y > fbMax.y + epsilon || faMax.y < fbMin.y - epsilon ||
                faMin.z > fbMax.z + epsilon || faMax.z < fbMin.z - epsilon;
        }

        /// <summary>
        /// 确定两个面是否可以合并
        /// </summary>
        /// <param name="fa"></param>
        /// <param name="fb"></param>
        /// <param name="faCommon">共同点的下标 i，另一个下标为 (i+2)%3</param>
        /// <param name="fbCommon">共同点的下标 j，另一个下标为 (j+1)%3</param>
        /// <returns></returns>
        public static bool TryConfirmNeighbor(FaceF fa, FaceF fb, out int faCommon, out int fbCommon)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (SameVector(fa.Vertices[i], fb.Vertices[j]) &&
                        SameVector(fa.Vertices[(i + 2) % 3], fb.Vertices[(j + 1) % 3]) &&
                        V3Collinear(fa.Vertices[(i + 1) % 3], fa.Vertices[i], fb.Vertices[(j + 2) % 3]))
                    {
                        faCommon = i;
                        fbCommon = j;
                        return true;
                    }
                }
            }
            faCommon = -1;
            fbCommon = -1;
            return false;
        }
    }

    /// <summary>
    /// 由<see cref="FaceF"/>的集合描述的 3D 形状
    /// </summary>
    public class ObjectFaceF
    {
        const float areaEpsilon = 1e-5f;

        public readonly List<FaceF> faceFs = new List<FaceF>();

        public ObjectFaceF(Vector3[] vertices, int[] triangles)
        {
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                faceFs.Add(new FaceF(
                    vertices[triangles[i]], 
                    vertices[triangles[i + 1]], 
                    vertices[triangles[i + 2]]));
            }
        }

        KeyValuePair<Vector3[], int[]> ToMeshObj()
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (var face in faceFs)
            {
                if (!face.IsRoot() || face.Area < areaEpsilon) { continue; }
                vertices.Add(face.Vertices[0]);
                vertices.Add(face.Vertices[1]);
                vertices.Add(face.Vertices[2]);
            }

            int[] triangles = new int[vertices.Count];
            for (int i = 0; i < triangles.Length; i++) { triangles[i] = i; }
            return new KeyValuePair<Vector3[], int[]>(vertices.ToArray(), triangles);
        }

        public Mesh ToMesh()
        {
            var meshMsg = ToMeshObj();
            Mesh result = new Mesh
            {
                vertices = meshMsg.Key,
                triangles = meshMsg.Value
            };
            result.RecalculateBounds();
            result.RecalculateNormals();
            return result;
        }

        /// <summary>
        /// 合并小三角形为大三角形
        /// </summary>
        public void MergeNeighbors()
        {
            int faceNum = faceFs.Count;
            for (int i = 0; i < faceNum; i++)
            {
                for (int j = 0; j < faceNum; j++)
                {
                    if (i == j) { continue; }
                    FaceF fa = faceFs[i];
                    FaceF fb = faceFs[j];

                    if (fa.SameFace(fb) || !fa.SamePlane(fb)) { continue; }

                    if (FaceF.TryConfirmNeighbor(fa, fb, out int faC, out int fbC))
                    {
                        fa.Vertices[faC] = fb.Vertices[(fbC + 2) % 3];
                        fb.SetParent(fa);  // 根节点是 fa
                    }
                }
            }
        }

        /// <summary>
        /// 焊接指定公差内的点(不好用)
        /// </summary>
        /// <param name="sqrTol">公差的平方</param>
        public void WeldVertices(float sqrTol = 1e-10f)
        {
            int faceNum = faceFs.Count;
            for (int i = 0; i < faceNum; i++)
            {
                if (!faceFs[i].IsRoot()) { continue; }

                for (int j = 0; j < faceNum; j++)
                {
                    if (!faceFs[j].IsRoot() || FaceF.FaceClearOf(faceFs[i], faceFs[j])) { continue; }
                    // 到这里可以比较两个三角形
                    // 比较两个三角形的点...
                    for (int ci = 0; ci < 3; ci++)
                    {
                        for (int cj = 0; cj < 3; cj++)
                        {
                            if (i == j && ci == cj) { continue; }
                            if (Vector3.SqrMagnitude(faceFs[i].Vertices[ci] - faceFs[j].Vertices[cj]) < sqrTol)
                            {
                                // 由于仅靠点顺序描述三角面，需要更新所有的点
                                UpdateVertice(faceFs[i].Vertices[ci], faceFs[j].Vertices[cj]);
                            }
                        }
                    }
                }
            }
        }

        void UpdateVertice(Vector3 oldValue, Vector3 newValue)
        {
            foreach (var f in faceFs)
            {
                if (!f.IsRoot()) { continue; }
                var vertices = f.Vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (vertices[i].Equals(oldValue))
                    {
                        vertices[i] = newValue;
                        break;
                    }
                }
            }
        }
    }
}
