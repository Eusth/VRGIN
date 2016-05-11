using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core.Visuals;

namespace VRGIN.Core.Controls
{
    public class WarpTool : Tool
    {
        ArcRenderer ArcRenderer;
        SteamVR_PlayArea PlayArea;
        Transform PlayAreaRotation;
        Transform Indicator;
        Transform DirectionIndicator;

        SteamVR_Camera SteamCam;

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
        
        protected override void OnAwake()
        {
            ArcRenderer = new GameObject("Arc Renderer").AddComponent<ArcRenderer>();
            ArcRenderer.transform.SetParent(transform, false);
            ArcRenderer.gameObject.SetActive(false);

            PlayAreaRotation = new GameObject("PlayArea Y").transform;

            PlayArea = new GameObject("PlayArea").AddComponent<SteamVR_PlayArea>();
            PlayArea.drawInGame = true;
            PlayArea.size = SteamVR_PlayArea.Size.Calibrated;

            PlayArea.transform.SetParent(PlayAreaRotation, false);

            // -- Create indicator

            Indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            Indicator.SetParent(PlayAreaRotation, false);

            var renderer = Indicator.GetComponent<Renderer>();
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
            renderer.material.color = Color.cyan;



            DirectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            DirectionIndicator.SetParent(PlayAreaRotation, false);
            DirectionIndicator.localScale = new Vector3(0.01f, 0.01f, .2f);
            DirectionIndicator.localRotation = Quaternion.identity;
            renderer = DirectionIndicator.GetComponent<Renderer>();
            renderer.material = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
#if UNITY_4_5
            renderer.castShadows = false;
#else
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
#endif
            renderer.receiveShadows = false;
            renderer.useLightProbes = false;
            renderer.material.color = Color.cyan;
        }

        protected override void OnDestroy()
        {
            GameObject.Destroy(PlayAreaRotation.gameObject);
        }

        protected override void OnStart()
        {
            SteamCam = VRCamera.Instance.SteamCam;
            PlayAreaRotation.transform.SetParent(SteamCam.transform, false);
            //ArcRenderer.transform.SetParent(SteamCam.transform, false);
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
            if (!IsTracking) return;

            _CanImpersonate = false;

            if (!VRManager.Instance.Interpreter.IsEveryoneHeaded)
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
                        Controller.TriggerHapticPulse();
                        _CanImpersonate = true;
                    }
                }
            }
        }

        protected override void OnLateUpdate()
        {

            PlayAreaRotation.position = ArcRenderer.target;
            PlayArea.transform.localPosition = -new Vector3(SteamCam.head.transform.localPosition.x, 0, SteamCam.head.transform.localPosition.z);
            PlayAreaRotation.rotation = Quaternion.Euler(0, -_AdditionalRotation + SteamCam.origin.rotation.eulerAngles.y, 0);

            Indicator.localScale = Vector3.one * 0.1f + Vector3.one * Mathf.Sin(Time.time*5) * 0.05f;
            DirectionIndicator.localRotation = Quaternion.Euler(0, SteamCam.head.localEulerAngles.y, 0);
            DirectionIndicator.localPosition = (DirectionIndicator.localRotation) * new Vector3(0, 0.02f, 0.1f);

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
                // Warp!
                var rotOffset = Quaternion.Euler(0, -_AdditionalRotation + SteamCam.origin.rotation.eulerAngles.y, 0);
                SteamCam.origin.position = ArcRenderer.target - rotOffset * new Vector3(SteamCam.head.transform.localPosition.x, 0, SteamCam.head.transform.localPosition.z) * VRManager.Instance.Context.Settings.IPDScale;
                SteamCam.origin.rotation = rotOffset;
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
                    //Console.WriteLine("Detected circular movement. Total: {0}", _AdditionalRotation);
                } else
                {
                    Console.WriteLine("Discarding too large rotation: {0}", rot);
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
