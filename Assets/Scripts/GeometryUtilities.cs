using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine; // Assuming the use of Unity's Vector3

public static class GeometryUtilities
{
    public static int[] TriangulatePolygons(List<Vector3> vertices)
    {
        Vector3[] verticesArray = vertices.ToArray();
        Triangulator tr = new Triangulator(verticesArray); // Assuming you have a class called Triangulator
        int[] indices = tr.Triangulate();
        return indices;
    }

    public static List<List<List<double[]>>> ConvertJArrayToMultiPolygonList(JArray jArray)
    {
        var outerList = new List<List<List<double[]>>>();

        foreach (JArray firstLevel in jArray)
        {
            var middleList = new List<List<double[]>>();

            foreach (JArray secondLevel in firstLevel)
            {
                var innerList = new List<double[]>();

                foreach (JArray thirdLevel in secondLevel)
                {
                    innerList.Add(thirdLevel.ToObject<double[]>());
                }

                middleList.Add(innerList);
            }

            outerList.Add(middleList);
        }

        return outerList;
    }

    public static List<List<double[]>> ConvertJArrayToPolygonList(JArray jArray)
    {
        var outerList = new List<List<double[]>>();

        foreach (JArray firstLevel in jArray)
        {
            var innerList = new List<double[]>();

            foreach (JArray secondLevel in firstLevel)
            {
                innerList.Add(secondLevel.ToObject<double[]>());
            }

            outerList.Add(innerList);
        }

        return outerList;
    }

    public static Mesh CreateMeshFromIndices(List<Vector3> vertices, int[] indices)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}