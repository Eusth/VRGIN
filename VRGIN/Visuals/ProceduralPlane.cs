//
//  ProceduralPlane.cs
//
//  Created by Dimitris Doukas
//  Copyright 2014 doukasd.com
//

using UnityEngine;
using System.Collections;
using System;

namespace VRGIN.Visuals
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ProceduralPlane : MonoBehaviour
    {

        //constants
        private const int DEFAULT_X_SEGMENTS = 10;
        private const int DEFAULT_Y_SEGMENTS = 10;
        private const int MIN_X_SEGMENTS = 1;
        private const int MIN_Y_SEGMENTS = 1;
        private const float DEFAULT_WIDTH = 1.0f;
        private const float DEFAULT_HEIGHT = 1.0f;

        //public variables
        public int xSegments = DEFAULT_X_SEGMENTS;
        public int ySegments = DEFAULT_Y_SEGMENTS;
        public Vector2 topLeftOffset = Vector2.zero;
        public Vector2 topRightOffset = Vector2.zero;
        public Vector2 bottomLeftOffset = Vector2.zero;
        public Vector2 bottomRightOffset = Vector2.zero;
        public float distance = 1f;
        //private variables
        private Mesh modelMesh;
        private MeshFilter meshFilter;
        public float width = DEFAULT_WIDTH;
        public float height = DEFAULT_HEIGHT;
        private int numVertexColumns, numVertexRows;    //columns and rows of vertices
        public float angleSpan = 160;
        public float curviness = 0;

        public void AssignDefaultShader()
        {
            //assign it an unlit shader, common if used for digital keystoning of render texture
            MeshRenderer meshRenderer = (MeshRenderer)gameObject.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
            meshRenderer.sharedMaterial.color = Color.white;
        }

        public void Rebuild()
        {
            //height = distance * 3;


            //create the mesh
            modelMesh = new Mesh();
            modelMesh.name = "ProceduralPlaneMesh";
            meshFilter = (MeshFilter)gameObject.GetComponent<MeshFilter>();
            meshFilter.mesh = modelMesh;

            //sanity check
            if (xSegments < MIN_X_SEGMENTS) xSegments = MIN_X_SEGMENTS;
            if (ySegments < MIN_Y_SEGMENTS) ySegments = MIN_Y_SEGMENTS;

            //calculate how many vertices we need
            numVertexColumns = xSegments + 1;
            numVertexRows = ySegments + 1;

            //calculate sizes
            int numVertices = numVertexColumns * numVertexRows;
            int numUVs = numVertices;                   //always
            int numTris = xSegments * ySegments * 2;    //fact
            int trisArrayLength = numTris * 3;          //3 places in the array for each tri

            // log the number of tris
            //Logger.Info ("Plane has " + trisArrayLength/3 + " tris");

            //initialize arrays
            Vector3[] Vertices = new Vector3[numVertices];
            Vector2[] UVs = new Vector2[numUVs];
            int[] Tris = new int[trisArrayLength];

            //precalculate increments
            float xStep = width / xSegments;
            float yStep = height / ySegments;
            float uvStepH = 1.0f / xSegments;   // place UVs evenly
            float uvStepV = 1.0f / ySegments;
            float xOffset = -width / 2f;        // this offset means we want the pivot at the center
            float yOffset = -height / 2f;        // same as above
            float radSpan = angleSpan * Mathf.PI / 180;
            float mSpan = 1;
            float aspect = (float)Screen.width / Screen.height;
            float m2rad = radSpan / mSpan;


            for (int j = 0; j < numVertexRows; j++)
            {
                for (int i = 0; i < numVertexColumns; i++)
                {
                    // calculate some weights for the "keystone" vertex pull
                    // for some reason this doesn't work
                    // TODO: fix this to cache values and make it faster
                    //float bottomLeftWeight = ((numVertexColumns-1)-i)/(numVertexColumns-1) * ((numVertexRows-1)-j)/(numVertexRows-1);

                    // position current vertex
                    // these offsets are too ridiculous to even try to explain
                    // ok trying: basically each vertex we drag is affected by the offsets on the 4 courners but
                    // the weight of that effect is linearly inverse analogous to the distance from that corner

                    Vector3 p = new Vector3(
                             i * xStep + xOffset
                                                                        + bottomLeftOffset.x * ((numVertexColumns - 1) - i) / (numVertexColumns - 1) * ((numVertexRows - 1) - j) / (numVertexRows - 1)
                                                                        + bottomRightOffset.x * i / (numVertexColumns - 1) * ((numVertexRows - 1) - j) / (numVertexRows - 1)
                                                                        + topLeftOffset.x * ((numVertexColumns - 1) - i) / (numVertexColumns - 1) * j / (numVertexRows - 1)
                                                                        + topRightOffset.x * i / (numVertexColumns - 1) * j / (numVertexRows - 1),
                            j * yStep + yOffset
                                                                        + bottomLeftOffset.y * ((numVertexColumns - 1) - i) / (numVertexColumns - 1) * ((numVertexRows - 1) - j) / (numVertexRows - 1)
                                                                        + bottomRightOffset.y * i / (numVertexColumns - 1) * ((numVertexRows - 1) - j) / (numVertexRows - 1)
                                                                        + topLeftOffset.y * ((numVertexColumns - 1) - i) / (numVertexColumns - 1) * j / (numVertexRows - 1)
                                                                        + topRightOffset.y * i / (numVertexColumns - 1) * j / (numVertexRows - 1) - ((height - 1) / 2),

                            distance
                    );


                    float nx = Mathf.Lerp((aspect * height) * p.x, Mathf.Cos(Mathf.PI / 2 - p.x * m2rad) * distance, Mathf.Clamp01(curviness));
                    float z = Mathf.Sin(Mathf.PI / 2 - p.x * m2rad * Mathf.Clamp01(curviness));

                    int index = j * numVertexColumns + i;
                    //Logger.Info(90 - x * m2angle);

                    Vertices[index] = new Vector3(nx,
                                                 p.y,
                                                  z);

                    if (curviness > 1)
                    {
                        float roundness = curviness - 1;
                        Vertices[index] = Vector3.Lerp(Vertices[index], Vertices[index].normalized * distance, Mathf.Clamp01(roundness));
                        //  Logger.Info(roundness);
                    }

                    //calculate UVs
                    UVs[index] = new Vector2(i * uvStepH, j * uvStepV);

                    //create the tris				
                    if (j == 0 || i >= numVertexColumns - 1)
                    {
                        continue;
                    }
                    else
                    {
                        // For every vertex we draw 2 tris in this for-loop, therefore we need 2*3=6 indices in the Tris array
                        int baseIndex = (j - 1) * xSegments * 6 + i * 6;

                        //1st tri - below and in front
                        Tris[baseIndex + 0] = j * numVertexColumns + i;
                        Tris[baseIndex + 1] = j * numVertexColumns + i + 1;
                        Tris[baseIndex + 2] = (j - 1) * numVertexColumns + i;

                        //2nd tri - the one it doesn't touch
                        Tris[baseIndex + 3] = (j - 1) * numVertexColumns + i;
                        Tris[baseIndex + 4] = j * numVertexColumns + i + 1;
                        Tris[baseIndex + 5] = (j - 1) * numVertexColumns + i + 1;
                    }
                }
            }

            // assign vertices, uvs and tris
            modelMesh.Clear();
            modelMesh.vertices = Vertices;
            modelMesh.uv = UVs;
            modelMesh.triangles = Tris;
            modelMesh.RecalculateNormals();
            modelMesh.RecalculateBounds();
        }

        public float TransformX(float x)
        {
            float aspect = (float)Screen.width / Screen.height;
            float radSpan = angleSpan * Mathf.PI / 180;
            float mSpan = 1;
            return Mathf.Lerp((aspect * height) * x, Mathf.Cos(Mathf.PI / 2 - x * (radSpan / mSpan)) * distance, curviness);
        }

        public float TransformZ(float x)
        {
            float aspect = (float)Screen.width / Screen.height;
            float radSpan = angleSpan * Mathf.PI / 180;
            float mSpan = 1;
            float m2rad = radSpan / mSpan;

            return Mathf.Sin(Mathf.PI / 2 - x * m2rad * curviness);
        }
    }

}