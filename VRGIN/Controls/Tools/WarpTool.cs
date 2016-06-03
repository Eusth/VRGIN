using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Visuals;

namespace VRGIN.Controls.Tools
{
    public class WarpTool : Tool
    {

        public enum WarpMode
        {
            Rotate,
            TranslateAndScale
        }

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

                if (GetComponent<Renderer>())
                {
                    if (NewParent)
                    {
                        // Done loading!
                        transform.SetParent(NewParent, false);
                        transform.localScale = Vector3.one;
                        GetComponent<Renderer>().material.color = VR.Context.PrimaryColor;
                    }
                    else
                    {
                        // Seems like we're too late...
                        Core.Logger.Info("We're too late!");
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
        private PlayArea _CurrentPlayArea = new PlayArea();
        private PlayArea _ProspectedPlayArea = new PlayArea();
        private const float SCALE_THRESHOLD = 0.2f;
        private const float TRANSLATE_THRESHOLD = 0.2f;



        /// <summary>
        /// Gets or sets what the user can do by touching the thumbpad
        /// </summary>
        public WarpMode Mode = WarpMode.Rotate;

        private RumbleSession _RumbleSession;

        private bool _CanImpersonate = false;
        private Vector2 _StartPoint;
        private bool _Scaling = false;
        private bool _Translating = false;
        private float? _GripStartTime = null;
        bool Showing = false;

        private List<Vector2> _Points = new List<Vector2>();
        private const float GRIP_THRESHOLD = 1;

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
            VRLog.Info("Awake!");
            ArcRenderer = new GameObject("Arc Renderer").AddComponent<ArcRenderer>();
            ArcRenderer.transform.SetParent(transform, false);
            ArcRenderer.gameObject.SetActive(false);

            CreateArea();

            // -- Create indicator

            Indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            Indicator.SetParent(PlayAreaRotation, false);

            HeightIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            HeightIndicator.SetParent(PlayAreaRotation, false);


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

            SetVisibility(false);
        }

        protected override void OnDestroy()
        {
            VRLog.Info("Destroy!");

            DestroyImmediate(PlayAreaRotation.gameObject);
        }

        protected override void OnStart()
        {
            VRLog.Info("Start!");

            base.OnStart();

            ResetPlayArea(ref _CurrentPlayArea);
            ResetPlayArea(ref _ProspectedPlayArea);
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

        private void ResetPlayArea(ref PlayArea area)
        {
            area.Position = VR.Camera.SteamCam.origin.position;
            area.Scale = VR.Settings.IPDScale;
            area.Rotation = VR.Camera.SteamCam.origin.rotation.eulerAngles.y;
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

            if (triggerHapticImpulse && _RumbleSession == null)
            {
                _RumbleSession = new RumbleSession(100, 10);
                Owner.StartRumble(_RumbleSession);

            }
            else if (!triggerHapticImpulse && _RumbleSession != null)
            {
                _RumbleSession.Close();
                _RumbleSession = null;
            }
        }

        protected override void OnLateUpdate()
        {
            if (Showing)
            {
                var steamCam = VRCamera.Instance.SteamCam;
                float cylinderHeight = 2;
                float playerHeight = steamCam.head.localPosition.y;
                float pivot = 1f;

                PlayAreaRotation.position = new Vector3(ArcRenderer.target.x, _ProspectedPlayArea.Height, ArcRenderer.target.z);
                PlayAreaRotation.localScale = Vector3.one * _ProspectedPlayArea.Scale;
                PlayArea.transform.localPosition = -new Vector3(steamCam.head.transform.localPosition.x, 0, steamCam.head.transform.localPosition.z);
                PlayAreaRotation.rotation = Quaternion.Euler(0, _ProspectedPlayArea.Rotation, 0);
                ArcRenderer.Offset = _ProspectedPlayArea.Height;
                ArcRenderer.Scale = VR.Settings.IPDScale;

                Indicator.localScale = Vector3.one * 0.1f + Vector3.one * Mathf.Sin(Time.time * 5) * 0.05f;
                HeightIndicator.localScale = new Vector3(0.01f, playerHeight / cylinderHeight, 0.01f);
                HeightIndicator.localPosition = new Vector3(0, playerHeight - pivot * (playerHeight / cylinderHeight), 0);
                //DirectionIndicator.localRotation = Quaternion.Euler(0, SteamCam.head.localEulerAngles.y, 0);
                //DirectionIndicator.localPosition = (DirectionIndicator.localRotation) * new Vector3(0, 0.02f, 0.1f);
            }
        }

        protected override void OnFixedUpdate()
        {
            if (Controller.GetTouchDown(EVRButtonId.k_EButton_Axis0))
            {
                SetVisibility(true);

                Reset();
                _StartPoint = Controller.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
                _CurrentPlayArea = _ProspectedPlayArea;
            }
            if (Controller.GetTouchUp(EVRButtonId.k_EButton_Axis0))
            {
                SetVisibility(false);
            }

            if (Controller.GetPressDown(EVRButtonId.k_EButton_Grip))
            {
                _GripStartTime = Time.time;
            }
            if (_GripStartTime != null)
            {
                if (Time.time - _GripStartTime.Value > GRIP_THRESHOLD)
                {
                    _ProspectedPlayArea.Height = 0;
                    _ProspectedPlayArea.Scale = 1.0f;
                    _GripStartTime = null;
                }
            }

            if (Controller.GetPressUp(EVRButtonId.k_EButton_Grip) && _GripStartTime != null)
            {
                Mode = (WarpMode)((((int)Mode) + 1) % Enum.GetValues(typeof(WarpMode)).Length);
            }


            if (Controller.GetPressDown(EVRButtonId.k_EButton_Axis0))
            {
                var steamCam = VRCamera.Instance.SteamCam;

                // Warp!
                ApplyPlayArea(_ProspectedPlayArea);
            }

            if (Showing)
            {

                if (Mode == WarpMode.Rotate)
                {
                    _Points.Add(Controller.GetAxis(EVRButtonId.k_EButton_Axis0));

                    if (_Points.Count > 2)
                    {
                        DetectCircle();
                    }
                }
                else if (Mode == WarpMode.TranslateAndScale)
                {
                    DetectTranslationAndScale();
                }
            }

            //if (_CanImpersonate)
            {
                if (VRManager.Instance.Interpreter.Actors.Any() && Controller.GetHairTriggerDown())
                {
                    VRManager.Instance.Mode.Impersonate(VRManager.Instance.Interpreter.Actors.First());
                }
            }
        }

        private void DetectTranslationAndScale()
        {
            var point = Controller.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
            float sx = point.x - _StartPoint.x;
            float sy = point.y - _StartPoint.y;

            // Update state
            if (!_Scaling && !_Translating && Mathf.Abs(sx) > SCALE_THRESHOLD)
            {
                _Scaling = true;
            }
            if (!_Translating && !_Scaling && Mathf.Abs(sy) > TRANSLATE_THRESHOLD)
            {
                _Translating = true;
            }

            // Update values
            if (_Scaling)
            {
                // [-2..2] -> [-0.6..0.6] -> [0.6..1.6]
                _ProspectedPlayArea.Scale = _CurrentPlayArea.Scale * (sx * 0.3f + 1);
            }

            if (_Translating)
            {
                // [-2..2] -> [-1..1]
                _ProspectedPlayArea.Height = _CurrentPlayArea.Height + (sy * 0.5f);
            }
        }

        private void DetectCircle()
        {

            float? minDist = null;
            float? maxDist = null;
            float avgDist = 0;

            // evaulate points to determine center
            foreach (var point in _Points)
            {
                float dist = point.magnitude;
                minDist = Math.Max(minDist ?? dist, dist);
                maxDist = Math.Max(maxDist ?? dist, dist);
                avgDist += dist;
            }
            avgDist /= _Points.Count;

            if (maxDist - minDist < 0.2f && minDist > 0.2f)
            {
                float startAngle = Mathf.Atan2(_Points.First().y, _Points.First().x) * Mathf.Rad2Deg;
                float endAngle = Mathf.Atan2(_Points.Last().y, _Points.Last().x) * Mathf.Rad2Deg;
                float rot = (endAngle - startAngle);
                if (Mathf.Abs(rot) < 60)
                {
                    _ProspectedPlayArea.Rotation -= rot;
                    //Logger.Info("Detected circular movement. Total: {0}", _AdditionalRotation);
                }
                else
                {
                    Core.Logger.Info("Discarding too large rotation: {0}", rot);
                }
            }
            _Points.Clear();
        }

        private void Reset()
        {
            _Scaling = false;
            _Translating = false;
            _Points.Clear();

            //ResetPlayArea(_CurrentPlayArea);
            //ResetPlayArea(_ProspectedPlayArea);
        }

        private void ApplyPlayArea(PlayArea area)
        {
            var rotOffset = Quaternion.Euler(0, _ProspectedPlayArea.Rotation, 0);
            var steamCam = VR.Camera.SteamCam;

            steamCam.origin.position = ArcRenderer.target
                + Vector3.up * _ProspectedPlayArea.Height
                - rotOffset * new Vector3(steamCam.head.transform.localPosition.x, 0, steamCam.head.transform.localPosition.z) * _ProspectedPlayArea.Scale;
            steamCam.origin.rotation = rotOffset;

            VR.Settings.IPDScale = _ProspectedPlayArea.Scale;
        }

        public override List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>(new HelpText[] {
                HelpText.Create("Tap to teleport", FindAttachPosition("trackpad"), new Vector3(0, 0.02f, 0.05f)),
                HelpText.Create("Circle to rotate", FindAttachPosition("trackpad"), new Vector3(0.05f, 0.02f, 0), new Vector3(0.015f, 0, 0)),
                HelpText.Create("Swipe to transform", FindAttachPosition("trackpad"), new Vector3(-0.05f, 0.02f, 0), new Vector3(-0.015f, 0, 0)),
                HelpText.Create("Warp into main char", FindAttachPosition("trigger"), new Vector3(0.06f, 0.04f, -0.05f)),
                HelpText.Create("rotate / transform", FindAttachPosition("lgrip"), new Vector3(-0.06f, 0.0f, -0.05f))
            });
        }
    }
}
