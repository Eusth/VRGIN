using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using VRGIN.Core;

namespace VRGIN.Visuals
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class ArcRenderer : MonoBehaviour
    {

        public int VertexCount = 50;
        public float UvSpeed = 5;
        public float Velocity = 6f;
        private MeshFilter _MeshFilter;
        private Renderer _Renderer;
        public Vector3 target;
        public float Offset = 0;
        public float Scale = 1;
        private Mesh _mesh;

        // Use this for initialization
        void Awake()
        {
            _MeshFilter = GetComponent<MeshFilter>();
            _Renderer = GetComponent<Renderer>();

            _mesh = new Mesh();
            _Renderer.material = VRManager.Instance.Context.Materials.Sprite;
#if UNITY_4_5
            _Renderer.castShadows = false;
#else
            _Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
#endif
            _Renderer.receiveShadows = false;
            _Renderer.useLightProbes = false;
            _Renderer.material.color = VRManager.Instance.Context.PrimaryColor;
        }


        // Update is called once per frame
        public void Update()
        {
            var direction = transform.forward;
            var vertices = new List<Vector3>();

            var pos = transform.position;
            float v = -(Velocity * transform.forward).y * Scale;
            float g = Physics.gravity.y * Scale;
            //float maxH = (v * v) / (2 * g);
            //float h = Mathf.Max(maxH, pos.y - Offset);
            float h = pos.y - Offset;

            float totT1 = (Mathf.Sqrt(v * v - 2 * g * h) + v) / g;
            float totT2 = (v - Mathf.Sqrt(v * v - 2 * g * h)) / g;
            float totT = Mathf.Max(totT1, totT2);
            totT = Mathf.Abs(totT);

            float timeStep = totT / VertexCount;

            for (int i = 0; i <= VertexCount; i++)
            {
                float t = Mathf.Clamp(((i / (VertexCount - 1f)) * totT) + ((Time.time * UvSpeed) % 2) * timeStep - timeStep, 0, totT);
                //Logger.Info(t);
                vertices.Add(transform.InverseTransformPoint(pos + ((direction * Velocity) * t + 0.5f * Physics.gravity * t * t) * Scale));
            }


            target = transform.position + ((direction * Velocity) * totT + 0.5f * Physics.gravity * totT * totT) * Scale;
            target.y = 0;

            GetComponent<Renderer>().material.mainTextureOffset += new Vector2(UvSpeed * Time.deltaTime, 0);

            _mesh.vertices = vertices.ToArray();
            //mesh.SetIndices(vertices.Select((v, i) => i).ToArray(), MeshTopology.LineStrip, 0);
            _mesh.SetIndices(vertices.Take(vertices.Count - 1).Select((ve, i) => i).Where(i => i % 2 == 0).SelectMany(i => new int[] { i, i + 1 }).ToArray(), MeshTopology.Lines, 0);

            _MeshFilter.mesh = _mesh;
        }

        void OnEnable()
        {
            GetComponent<Renderer>().enabled = true;
        }

        void OnDisable()
        {
            GetComponent<Renderer>().enabled = false;
        }
    }

}