using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Net3dBool;

namespace N3dBoolExample
{
    public static class MeshBooleanOperator
    {
        public static Mesh GetUnion(MeshFilter meshF1, MeshFilter meshF2)
        {
            BooleanModeller booleanModeller = new BooleanModeller(meshF1.ToSolidInWCS(), meshF2.ToSolidInWCS());
            var end = booleanModeller.GetUnion();
            end.translate((-meshF1.transform.position).ToVector3Double());

            ObjectFaceF objF = new ObjectFaceF(end.Vertices.ToVector3Arr(), end.Triangles);
            objF.MergeNeighbors();
            var result = objF.ToMesh();
            result.SetUVinWCS();
            return result;
        }

        public static Mesh GetDifference(MeshFilter meshF1, MeshFilter meshF2)
        {
            BooleanModeller booleanModeller = new BooleanModeller(meshF1.ToSolidInWCS(), meshF2.ToSolidInWCS());
            var end = booleanModeller.GetDifference();
            end.translate((-meshF1.transform.position).ToVector3Double());

            ObjectFaceF objF = new ObjectFaceF(end.Vertices.ToVector3Arr(), end.Triangles);
            objF.MergeNeighbors();

            var result = objF.ToMesh();
            result.SetUVinWCS();
            return result;
        }

        public static Mesh GetIntersection(MeshFilter meshF1, MeshFilter meshF2)
        {
            BooleanModeller booleanModeller = new BooleanModeller(meshF1.ToSolidInWCS(), meshF2.ToSolidInWCS());
            var end = booleanModeller.GetIntersection();
            end.translate((-meshF1.transform.position).ToVector3Double());

            ObjectFaceF objF = new ObjectFaceF(end.Vertices.ToVector3Arr(), end.Triangles);
            objF.MergeNeighbors();

            var result = objF.ToMesh();
            result.SetUVinWCS();
            return result;
        }
    }
}