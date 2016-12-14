using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;

namespace VRGIN.U46.Visuals
{
    public class PlayAreaVisualization : ProtectedBehaviour
    {
        public PlayArea Area = new PlayArea();
        SteamVR_PlayArea PlayArea;
        Transform Indicator;
        Transform DirectionIndicator;
        Transform HeightIndicator;

        protected override void OnAwake()
        {
            base.OnAwake();

            CreateArea();
            
            Indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            Indicator.SetParent(transform, false);

            HeightIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            HeightIndicator.SetParent(transform, false);


            foreach (var indicator in new Transform[] { Indicator, HeightIndicator })
            {
                var renderer = indicator.GetComponent<Renderer>();
                renderer.material = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
                //renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                //renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
#if UNITY_4_5
                renderer.castShadows = false;
#else
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
#endif
                renderer.receiveShadows = false;
                renderer.useLightProbes = false;
                renderer.material.color = VR.Context.PrimaryColor;
            }
        }

        protected virtual void CreateArea()
        {
            PlayArea = new GameObject("PlayArea").AddComponent<SteamVR_PlayArea>();
            PlayArea.drawInGame = true;
            PlayArea.size = SteamVR_PlayArea.Size.Calibrated;

            PlayArea.transform.SetParent(transform, false);

            DirectionIndicator = CreateClone();
        }

        protected virtual Transform CreateClone()
        {
            var model = new GameObject("Model").AddComponent<HMDLoader>();
            model.NewParent = PlayArea.transform;

            return model.transform;
        }

        internal static PlayAreaVisualization Create(PlayArea playArea=null)
        {
            var visualization = new GameObject("Play Area Viszalization").AddComponent<PlayAreaVisualization>();
            if (playArea != null)
            {
                visualization.Area = playArea;
            }
            return visualization;
        }

        protected override void OnStart()
        {
            base.OnStart();

        }
        
        protected virtual void OnEnable() {
            PlayArea.BuildMesh();
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void OnDestroy()
        {
        }

        public void Enable()
        {
            gameObject.SetActive(true);

        }
        public void Disable()
        {
            gameObject.SetActive(false);
        }

        public void UpdatePosition()
        {
            var steamCam = VRCamera.Instance.SteamCam;
            float cylinderHeight = 2;
            float playerHeight = steamCam.head.localPosition.y;
            float pivot = 1f;

            transform.position = Area.Position;
            transform.localScale = Vector3.one * Area.Scale;
            PlayArea.transform.localPosition = -new Vector3(steamCam.head.transform.localPosition.x, 0, steamCam.head.transform.localPosition.z);
            transform.rotation = Quaternion.Euler(0, Area.Rotation, 0);

            Indicator.localScale = Vector3.one * 0.1f + Vector3.one * Mathf.Sin(Time.time * 5) * 0.05f;
            HeightIndicator.localScale = new Vector3(0.01f, playerHeight / cylinderHeight, 0.01f);
            HeightIndicator.localPosition = new Vector3(0, playerHeight - pivot * (playerHeight / cylinderHeight), 0);
        }

        protected override void OnLateUpdate()
        {
            UpdatePosition();
        }


        private class HMDLoader : ProtectedBehaviour
        {
            public Transform NewParent;
            private SteamVR_RenderModel _Model;

            protected override void OnStart()
            {
                DontDestroyOnLoad(this);

                transform.localScale = Vector3.zero;

                _Model = gameObject.AddComponent<SteamVR_RenderModel>();
                //model.transform.SetParent(VR.Camera.SteamCam.head, false);
                _Model.shader = VR.Context.Materials.StandardShader;
                gameObject.AddComponent<SteamVR_TrackedObject>();

                _Model.SetDeviceIndex((int)OpenVR.k_unTrackedDeviceIndex_Hmd);
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();

                if (!NewParent && !this.enabled)
                {
                    DestroyImmediate(gameObject);
                }

                if (GetComponent<Renderer>())
                {
                    if (NewParent)
                    {
                        // Done loading!
                        transform.SetParent(NewParent, false);
                        transform.localScale = Vector3.one;
                        GetComponent<Renderer>().material.color = VR.Context.PrimaryColor;

                        this.enabled = false;
                    }
                    else
                    {
                        // Seems like we're too late...
                        VRLog.Info("We're too late!");
                        Destroy(gameObject);
                    }

                }
            }
        }
    }
}
