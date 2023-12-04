using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine; // Assuming the use of Unity's Vector3

public static class GeometryUtilities
{
    public static int[] TriangulatePolygons(List<Vector3> vertices)
    {

        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 vertex = vertices[i];
            vertices[i] = new Vector3(vertex.x, 0, vertex.z);
        }
        vertices.RemoveAt(vertices.Count - 1);
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

        // Correct the winding order if necessary
        CorrectWindingOrder(vertices, ref indices);

        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void CorrectWindingOrder(List<Vector3> vertices, ref int[] indices)
    {
        for (int i = 0; i < indices.Length; i += 3)
        {
            if (IsTriangleInverted(vertices, indices[i], indices[i + 1], indices[i + 2]))
            {
                // Swap the winding order by swapping two indices
                int temp = indices[i + 1];
                indices[i + 1] = indices[i + 2];
                indices[i + 2] = temp;
            }
        }
    }

    private static bool IsTriangleInverted(List<Vector3> vertices, int index1, int index2, int index3)
    {
        Vector3 v1 = vertices[index1];
        Vector3 v2 = vertices[index2];
        Vector3 v3 = vertices[index3];

        // Calculate the normal using the cross product
        Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1);

        // Assuming that the desired direction is upwards (y-positive)
        // Check if the normal is facing downwards (y-negative)
        return normal.y < 0;
    }
}