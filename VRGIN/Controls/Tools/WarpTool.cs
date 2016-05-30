using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core.Helpers;
using VRGIN.Core.Visuals;

namespace VRGIN.Core.Controls
{
    public class WarpTool : Tool
    {

        private class HMDLoader : ProtectedBehaviour
        {
            public Transform NewParent;
            private SteamVR_RenderModel _Model;

            protected override void OnStart()
            {
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

                if(GetComponent<Renderer>())
                {
                    if (NewParent)
                    {
                        // Done loading!
                        transform.SetParent(NewParent, false);
                        transform.localScale = Vector3.one;
                        GetComponent<Renderer>().material.color = VR.Context.PrimaryColor;
                    } else
                    {
                        // Seems like we're too late...
                        Logger.Info("We're too late!");
                        Destroy(gameObject);
                    }

                }
            }
        }

        ArcRenderer ArcRenderer;
        SteamVR_PlayArea PlayArea;
        Transform PlayAreaRotation;
        Transform Indicator;
        Transform DirectionIndicator;
        Transform HeightIndicator;

        private RumbleSession _RumbleSession;

        private float _AdditionalRotation;
        bool _CanImpersonate = false;


        bool Showing = false;

        private List<Vector2> points = new List<Vector2>();

        public override Texture2D Image
        {
            get
            {
                return UnityHelper.LoadImage("icon_warp.png");
            }
        }

        protected virtual void CreateArea()
        {
            PlayAreaRotation = new GameObject("PlayArea Y").transform;

            PlayArea = new GameObject("PlayArea").AddComponent<SteamVR_PlayArea>();
            PlayArea.drawInGame = true;
            PlayArea.size = SteamVR_PlayArea.Size.Calibrated;

            PlayArea.transform.SetParent(PlayAreaRotation, false);


            DirectionIndicator = CreateClone();
            DontDestroyOnLoad(PlayAreaRotation.gameObject);

            //DontDestroyOnLoad(PlayAreaRotation.gameObject);
        }

        protected virtual Transform CreateClone()
        {
            var model = new GameObject("Model").AddComponent<HMDLoader>();
            model.NewParent = PlayArea.transform;
            return model.transform;
        }

        protected override void OnAwake()
        {
            Logger.Info("Awake!");
            ArcRenderer = new GameObject("Arc Renderer").AddComponent<ArcRenderer>();
            ArcRenderer.transform.SetParent(transform, false);
            ArcRenderer.gameObject.SetActive(false);

            CreateArea();
            
            // -- Create indicator

            Indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            Indicator.SetParent(PlayAreaRotation, false);

            HeightIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            HeightIndicator.SetParent(PlayAreaRotation, false);


            foreach(var indicator in new Transform[] {Indicator, HeightIndicator })
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

            SetVisibility(false);
        }

        protected override void OnDestroy()
        {
            Logger.Info("Destroy!");

            GameObject.DestroyImmediate(PlayAreaRotation.gameObject);
        }

        protected override void OnStart()
        {
            Logger.Info("Start!");

            base.OnStart();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SetVisibility(false);

            PlayArea.BuildMesh();
        }
        
        void SetVisibility(bool visible)
        {
            Showing = visible;
            ArcRenderer.gameObject.SetActive(visible);
            PlayAreaRotation.gameObject.SetActive(visible);           
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SetVisibility(false);
        }
        
        protected override void OnUpdate()
        {
            base.OnUpdate();

            bool triggerHapticImpulse = false;

            if (!IsTracking) return;

            _CanImpersonate = false;

            if (VRManager.Instance.Interpreter.IsEveryoneHeaded)
            {
                var firstMember = VRManager.Instance.Interpreter.Actors.FirstOrDefault();
                if (firstMember != null && firstMember.IsValid)
                {
                    var hisRot = Quaternion.Euler(firstMember.Eyes.rotation.ToPitchYawRollRad().x * Mathf.Rad2Deg, 0, 0);
                    var hisHeight = firstMember.Eyes.position.y;
                    var steamCam = VRCamera.Instance.SteamCam;

                    var myRot = Quaternion.Euler(steamCam.head.rotation.ToPitchYawRollRad().x * Mathf.Rad2Deg, 0, 0);
                    var myHeight = steamCam.head.position.y;

                    if (Quaternion.Dot(hisRot, myRot) > 0.96f && Mathf.Abs(myHeight - hisHeight) < 0.05f)
                    {
                        triggerHapticImpulse = true;
                        _CanImpersonate = true;
                    }
                }
            }

            if(triggerHapticImpulse && _RumbleSession == null)
            {
                _RumbleSession = new RumbleSession(100, 10);
                Owner.StartRumble(_RumbleSession);
                
            } else if(!triggerHapticImpulse && _RumbleSession != null)
            {
                _RumbleSession.Close();
                _RumbleSession = null;
            }
        }

        protected override void OnLateUpdate()
        {
            var steamCam = VRCamera.Instance.SteamCam;
            float cylinderHeight = 2;
            float playerHeight = steamCam.head.localPosition.y;
            float pivot = 1f;

            PlayAreaRotation.position = ArcRenderer.target;
            PlayAreaRotation.localScale = Vector3.one * VR.Settings.IPDScale;
            PlayArea.transform.localPosition = -new Vector3(steamCam.head.transform.localPosition.x, 0, steamCam.head.transform.localPosition.z);
            PlayAreaRotation.rotation = Quaternion.Euler(0, -_AdditionalRotation + steamCam.origin.rotation.eulerAngles.y, 0);

            Indicator.localScale = Vector3.one * 0.1f + Vector3.one * Mathf.Sin(Time.time * 5) * 0.05f;
            HeightIndicator.localScale = new Vector3(0.01f, playerHeight / cylinderHeight, 0.01f);
            HeightIndicator.localPosition = new Vector3(0, playerHeight - pivot * (playerHeight / cylinderHeight), 0);
            //DirectionIndicator.localRotation = Quaternion.Euler(0, SteamCam.head.localEulerAngles.y, 0);
            //DirectionIndicator.localPosition = (DirectionIndicator.localRotation) * new Vector3(0, 0.02f, 0.1f);

        }

        protected override void OnFixedUpdate()
        {
            if (Controller.GetTouchDown(EVRButtonId.k_EButton_Axis0))
            {
                SetVisibility(true);
            }
            if (Controller.GetTouchUp(EVRButtonId.k_EButton_Axis0))
            {
                SetVisibility(false);
                Reset();
            }


            if (Controller.GetPressDown(EVRButtonId.k_EButton_Axis0))
            {
                var steamCam = VRCamera.Instance.SteamCam;

                // Warp!
                var rotOffset = Quaternion.Euler(0, -_AdditionalRotation + steamCam.origin.rotation.eulerAngles.y, 0);
                steamCam.origin.position = ArcRenderer.target - rotOffset * new Vector3(steamCam.head.transform.localPosition.x, 0, steamCam.head.transform.localPosition.z) * VR.Settings.IPDScale;
                steamCam.origin.rotation = rotOffset;
                Reset();
            }

            if (Showing)
            {
                points.Add(Controller.GetAxis(EVRButtonId.k_EButton_Axis0));

                if(points.Count > 2)
                {
                    DetectCircle();
                }
            }

            //if (_CanImpersonate)
            {
                if (VRManager.Instance.Interpreter.Actors.Any() && Controller.GetHairTriggerDown()) {
                    VRManager.Instance.Mode.Impersonate(VRManager.Instance.Interpreter.Actors.First());
                }
            }
        }

        private void DetectCircle()
        {

            float? minDist = null;
            float? maxDist = null;
            float avgDist = 0;

            // evaulate points to determine center
            foreach (var point in points)
            {
                float dist = point.magnitude;
                minDist = Math.Max(minDist ?? dist, dist);
                maxDist = Math.Max(maxDist ?? dist, dist);
                avgDist += dist;
            }
            avgDist /= points.Count;

            if (maxDist - minDist < 0.2f && minDist > 0.2f)
            {
                float startAngle = Mathf.Atan2(points.First().y, points.First().x) * Mathf.Rad2Deg;
                float endAngle = Mathf.Atan2(points.Last().y, points.Last().x) * Mathf.Rad2Deg;
                float rot = (endAngle - startAngle);
                if (Mathf.Abs(rot) < 60)
                {
                    _AdditionalRotation += rot;
                    //Logger.Info("Detected circular movement. Total: {0}", _AdditionalRotation);
                } else
                {
                    Logger.Info("Discarding too large rotation: {0}", rot);
                }
            }
            points.Clear();
        }

        private void Reset()
        {
            _AdditionalRotation = 0;
        }

        public override List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>(new HelpText[] {
                HelpText.Create("Tap to teleport", FindAttachPosition("trackpad"), new Vector3(0, 0.02f, 0.05f)),
                HelpText.Create("Circle to rotate area", FindAttachPosition("trackpad"), new Vector3(0.05f, 0.02f, 0), new Vector3(0.015f, 0, 0)),
                HelpText.Create("Warp into main char", FindAttachPosition("trigger"), new Vector3(0.06f, 0.04f, -0.05f))
            });
        }
    }
}
