using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Modes;
using VRGIN.Visuals;

namespace VRGIN.Controls.Tools
{
    public class WarpTool : Tool
    {

        private enum WarpState
        {
            None,
            Rotating,
            Transforming,
            Grabbing
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

        ArcRenderer ArcRenderer;
        SteamVR_PlayArea PlayArea;
        Transform PlayAreaRotation;
        Transform Indicator;
        Transform DirectionIndicator;
        Transform HeightIndicator;
        private PlayArea _CurrentPlayArea = new PlayArea();
        private PlayArea _ProspectedPlayArea = new PlayArea();
        private const float SCALE_THRESHOLD = 0.05f;
        private const float TRANSLATE_THRESHOLD = 0.05f;



        /// <summary>
        /// Gets or sets what the user can do by touching the thumbpad
        /// </summary>
        private WarpState State = WarpState.None;

        private TravelDistanceRumble _TravelRumble;

        private bool _CanImpersonate = false;
        private Vector3 _PrevPoint;
        private bool _Scaling = false;
        private bool _Translating = false;
        private float? _GripStartTime = null;
        private float? _TriggerDownTime = null;
        bool Showing = false;

        private List<Vector2> _Points = new List<Vector2>();
        private const float GRIP_TIME_THRESHOLD = 0.5f;
        private const float GRIP_DIFF_THRESHOLD = 0.03f;

        private const float EXACT_IMPERSONATION_TIME = 1;
        private Vector3 _PrevControllerPos;

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

            // Prepare rumble definitions
            _TravelRumble = new TravelDistanceRumble(500, 0.1f, transform);
            _TravelRumble.UseLocalPosition = true;

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

            EnterState(WarpState.None);
            SetVisibility(false);

            // Always stop rumbling when we're disabled
            Owner.StopRumble(_TravelRumble);
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

            if (State == WarpState.None)
            {
                if (Controller.GetTouchDown(EVRButtonId.k_EButton_Axis0))
                {
                    EnterState(WarpState.Rotating);
                }
                else if (Controller.GetPressDown(EVRButtonId.k_EButton_Grip))
                {
                    EnterState(WarpState.Grabbing);
                }
            }
            if (State == WarpState.Grabbing)
            {
                if (Controller.GetPress(EVRButtonId.k_EButton_Grip))
                {
                    var diff = transform.position - _PrevControllerPos;
                    if (Time.time - _GripStartTime > GRIP_TIME_THRESHOLD || diff.magnitude > GRIP_DIFF_THRESHOLD)
                    {
                        VR.Camera.SteamCam.origin.transform.position -= diff;
                        _ProspectedPlayArea.Height -= diff.y;
                        _PrevControllerPos = transform.position;
                        _GripStartTime = 0; // To make sure that pos is not reset
                    }
                }
                if (Controller.GetPressUp(EVRButtonId.k_EButton_Grip))
                {
                    EnterState(WarpState.None);
                    if (Time.time - _GripStartTime < GRIP_TIME_THRESHOLD)
                    {
                        Owner.StartRumble(new RumbleImpulse(800));
                        _ProspectedPlayArea.Height = 0;
                        _ProspectedPlayArea.Scale = 1.0f;
                    }
                }
            }


            if (State == WarpState.Rotating)
            {
                if (Controller.GetPressDown(EVRButtonId.k_EButton_Axis0))
                {
                    EnterState(WarpState.Transforming);
                }

                if (Controller.GetTouchUp(EVRButtonId.k_EButton_Axis0))
                {
                    EnterState(WarpState.None);
                }
            }

            if (State == WarpState.Transforming)
            {

                if (Controller.GetPress(EVRButtonId.k_EButton_Axis0))
                {
                    DetectTranslationAndScale();
                }
                if (Controller.GetPressUp(EVRButtonId.k_EButton_Axis0))
                {
                    var steamCam = VRCamera.Instance.SteamCam;

                    // Warp!
                    ApplyPlayArea(_ProspectedPlayArea);
                    EnterState(WarpState.Rotating);
                }
            }

            if (Showing && State == WarpState.Rotating)
            {
                _Points.Add(Controller.GetAxis(EVRButtonId.k_EButton_Axis0));

                if (_Points.Count > 2)
                {
                    DetectCircle();
                }
            }

            //if (_CanImpersonate)
            {
                if (Controller.GetHairTriggerDown())
                {
                    _TriggerDownTime = Time.time;
                }
                if (_TriggerDownTime != null)
                {
                    if (Controller.GetHairTrigger() && (Time.time - _TriggerDownTime) > EXACT_IMPERSONATION_TIME)
                    {
                        VRManager.Instance.Mode.Impersonate(VRManager.Instance.Interpreter.Actors.First(),
                            ImpersonationMode.Exactly);
                        _TriggerDownTime = null;
                    }
                    if (VRManager.Instance.Interpreter.Actors.Any() && Controller.GetHairTriggerUp())
                    {
                        VRManager.Instance.Mode.Impersonate(VRManager.Instance.Interpreter.Actors.First(),
                            ImpersonationMode.Approximately);
                    }
                }
            }
        }

        private void DetectTranslationAndScale()
        {
            var point = transform.position;
            var v = VR.Camera.SteamCam.head.transform.InverseTransformVector(point - _PrevPoint);
            // Update state
            if (!_Scaling && !_Translating && Mathf.Abs(v.z) > SCALE_THRESHOLD)
            {
                _Scaling = true;
            }
            if (!_Translating && !_Scaling && Mathf.Abs(v.y) > TRANSLATE_THRESHOLD)
            {
                _Translating = true;
            }

            // Update values
            if (_Scaling)
            {
                // [-2..2] -> [-0.6..0.6] -> [0.6..1.6]
                _ProspectedPlayArea.Scale = Mathf.Clamp(_ProspectedPlayArea.Scale + v.z, 0.01f, 50f);
            }

            if (_Translating)
            {
                // [-2..2] -> [-1..1]
                _ProspectedPlayArea.Height += (v.y);
            }

            if (_Scaling || _Translating)
            {
                _PrevPoint = point;
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
                    VRLog.Info("Discarding too large rotation: {0}", rot);
                }
            }
            _Points.Clear();
        }

        private void EnterState(WarpState state)
        {
            // LEAVE state
            switch (State)
            {
                case WarpState.None:


                    break;
                case WarpState.Rotating:

                    break;
                case WarpState.Transforming:
                    Owner.StopRumble(_TravelRumble);
                    break;

                case WarpState.Grabbing:
                    Owner.StopRumble(_TravelRumble);
                    break;
            }


            // ENTER state
            switch (state)
            {
                case WarpState.None:
                    SetVisibility(false);
                    break;
                case WarpState.Rotating:
                    SetVisibility(true);
                    Reset();
                    _CurrentPlayArea = _ProspectedPlayArea;
                    break;
                case WarpState.Transforming:
                    _PrevPoint = transform.position;
                    ArcRenderer.gameObject.SetActive(false);
                    _TravelRumble.Reset();
                    Owner.StartRumble(_TravelRumble);
                    break;
                case WarpState.Grabbing:
                    _PrevControllerPos = transform.position;
                    _GripStartTime = Time.time;
                    _TravelRumble.Reset();
                    Owner.StartRumble(_TravelRumble);
                    break;
            }

            State = state;
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
                HelpText.Create("Press to teleport", FindAttachPosition("trackpad"), new Vector3(0, 0.02f, 0.05f)),
                HelpText.Create("Circle to rotate", FindAttachPosition("trackpad"), new Vector3(0.05f, 0.02f, 0), new Vector3(0.015f, 0, 0)),
                HelpText.Create("press & move controller", FindAttachPosition("trackpad"), new Vector3(-0.05f, 0.02f, 0), new Vector3(-0.015f, 0, 0)),
                HelpText.Create("Warp into main char", FindAttachPosition("trigger"), new Vector3(0.06f, 0.04f, -0.05f)),
                HelpText.Create("reset area", FindAttachPosition("lgrip"), new Vector3(-0.06f, 0.0f, -0.05f))
            });
        }
    }
}
