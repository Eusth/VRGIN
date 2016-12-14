﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Modes;
using VRGIN.U46.Visuals;
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


        ArcRenderer ArcRenderer;
        PlayAreaVisualization _Visualization;
        private PlayArea _ProspectedPlayArea = new PlayArea();
        private const float SCALE_THRESHOLD = 0.05f;
        private const float TRANSLATE_THRESHOLD = 0.05f;

        /// <summary>
        /// Gets or sets what the user can do by touching the thumbpad
        /// </summary>
        private WarpState State = WarpState.None;

        private TravelDistanceRumble _TravelRumble;

        private Vector3 _PrevPoint;
        private float? _GripStartTime = null;
        private float? _TriggerDownTime = null;
        bool Showing = false;

        private List<Vector2> _Points = new List<Vector2>();
        private const float GRIP_TIME_THRESHOLD = 0.5f;
        private const float GRIP_DIFF_THRESHOLD = 0.03f;

        private const float EXACT_IMPERSONATION_TIME = 1;
        private Vector3 _PrevControllerPos;
        private Quaternion _PrevControllerRot;
        private Controller.Lock _OtherLock;
        private float _InitialControllerDistance;
        private float _InitialIPD;
        private Vector3 _PrevFromTo;
        private const EVRButtonId SECONDARY_SCALE_BUTTON = EVRButtonId.k_EButton_SteamVR_Trigger;
        private const EVRButtonId SECONDARY_ROTATE_BUTTON = EVRButtonId.k_EButton_Grip;
        private float _IPDOnStart;

        public override Texture2D Image
        {
            get
            {
                return UnityHelper.LoadImage("icon_warp.png");
            }
        }


        protected override void OnAwake()
        {
            VRLog.Info("Awake!");
            ArcRenderer = new GameObject("Arc Renderer").AddComponent<ArcRenderer>();
            ArcRenderer.transform.SetParent(transform, false);
            ArcRenderer.gameObject.SetActive(false);

            // -- Create indicator
            // Prepare rumble definitions
            _TravelRumble = new TravelDistanceRumble(500, 0.1f, transform);
            _TravelRumble.UseLocalPosition = true;

            _Visualization = PlayAreaVisualization.Create(_ProspectedPlayArea);
            DontDestroyOnLoad(_Visualization.gameObject);

            SetVisibility(false);
        }

        protected override void OnDestroy()
        {
            VRLog.Info("Destroy!");

            DestroyImmediate(_Visualization.gameObject);
        }

        protected override void OnStart()
        {
            VRLog.Info("Start!");

            base.OnStart();
            _IPDOnStart = VR.Settings.IPDScale;
            ResetPlayArea(_ProspectedPlayArea);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SetVisibility(false);
        }

        void SetVisibility(bool visible)
        {
            Showing = visible;

            if (visible)
            {
                ArcRenderer.Update();
                UpdateProspectedArea();
                _Visualization.UpdatePosition();
            }
            
            ArcRenderer.gameObject.SetActive(visible);
            _Visualization.gameObject.SetActive(visible);
        }

        private void ResetPlayArea(PlayArea area)
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
                UpdateProspectedArea();
            }
        }

        private void UpdateProspectedArea()
        {
            ArcRenderer.Offset = _ProspectedPlayArea.Height;
            ArcRenderer.Scale = VR.Settings.IPDScale;
            _ProspectedPlayArea.Position = new Vector3(ArcRenderer.target.x, _ProspectedPlayArea.Position.y, ArcRenderer.target.z);
        }

        private void CheckRotationalPress()
        {
            if (Controller.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                var v = Controller.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
                _ProspectedPlayArea.Reset();
                if (v.x < -0.2f)
                {
                    _ProspectedPlayArea.Rotation -= 20f;
                }
                else if (v.x > 0.2f)
                {
                    _ProspectedPlayArea.Rotation += 20f;
                }
                _ProspectedPlayArea.Apply();
            }
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (State == WarpState.None)
            {
                var v = Controller.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
                if (v.magnitude < 0.5f)
                {
                    if (Controller.GetTouchDown(EVRButtonId.k_EButton_SteamVR_Touchpad) /*||Controller.GetTouch(EVRButtonId.k_EButton_SteamVR_Touchpad)*/)
                    {
                        EnterState(WarpState.Rotating);
                    }
                }
                else
                {
                    CheckRotationalPress();
                }

                if (Controller.GetPressDown(EVRButtonId.k_EButton_Grip))
                {
                    EnterState(WarpState.Grabbing);
                }
            }
            if (State == WarpState.Grabbing)
            {
                HandleGrabbing();
            }


            if (State == WarpState.Rotating)
            {
                HandleRotation();
            }

            if (State == WarpState.Transforming)
            {
                if (Controller.GetPressUp(EVRButtonId.k_EButton_Axis0))
                {
                    var steamCam = VRCamera.Instance.SteamCam;

                    // Warp!
                    _ProspectedPlayArea.Apply();

                    // The preview head has to move away
                    ArcRenderer.Update();

                    EnterState(WarpState.Rotating);
                }
            }

            if (State == WarpState.None)
            {
                if (Controller.GetHairTriggerDown())
                {
                    _TriggerDownTime = Time.unscaledTime;
                }
                if (_TriggerDownTime != null)
                {
                    if (Controller.GetHairTrigger() && (Time.unscaledTime - _TriggerDownTime) > EXACT_IMPERSONATION_TIME)
                    {
                        VRManager.Instance.Mode.Impersonate(VR.Interpreter.FindNextActorToImpersonate(),
                            ImpersonationMode.Exactly);
                        _TriggerDownTime = null;
                    }
                    if (VRManager.Instance.Interpreter.Actors.Any() && Controller.GetHairTriggerUp())
                    {
                        VRManager.Instance.Mode.Impersonate(VR.Interpreter.FindNextActorToImpersonate(),
                            ImpersonationMode.Approximately);
                    }
                }
            }
        }

        private void HandleRotation()
        {
            if (Showing)
            {
                _Points.Add(Controller.GetAxis(EVRButtonId.k_EButton_Axis0));

                if (_Points.Count > 2)
                {
                    DetectCircle();
                }
            }

            if (Controller.GetPressDown(EVRButtonId.k_EButton_Axis0))
            {
                EnterState(WarpState.Transforming);
            }

            if (Controller.GetTouchUp(EVRButtonId.k_EButton_Axis0))
            {
                EnterState(WarpState.None);
            }

            
        }

        private void HandleGrabbing()
        {
            if (OtherController.IsTracking && !HasLock())
            {
                OtherController.TryAcquireFocus(out _OtherLock);
            }

            if (HasLock() && OtherController.Input.GetPressDown(SECONDARY_SCALE_BUTTON))
            {
                _InitialControllerDistance = Vector3.Distance(OtherController.transform.position, transform.position);
                _InitialIPD = VR.Settings.IPDScale;
                _PrevFromTo = (OtherController.transform.position - transform.position).normalized;
            }

            if (HasLock() && OtherController.Input.GetPressDown(SECONDARY_ROTATE_BUTTON))
            {
                _PrevFromTo = (OtherController.transform.position - transform.position).normalized;
            }

            if (Controller.GetPress(EVRButtonId.k_EButton_Grip))
            {
                if (HasLock() && (OtherController.Input.GetPress(SECONDARY_ROTATE_BUTTON) || OtherController.Input.GetPress(SECONDARY_SCALE_BUTTON)))
                {
                    var newFromTo = (OtherController.transform.position - transform.position).normalized;

                    if (OtherController.Input.GetPress(SECONDARY_SCALE_BUTTON))
                    {
                        var controllerDistance = Vector3.Distance(OtherController.transform.position, transform.position) * (_InitialIPD / VR.Settings.IPDScale);
                        float ratio = controllerDistance / _InitialControllerDistance;
                        VR.Settings.IPDScale = ratio * _InitialIPD;
                    }

                    if (OtherController.Input.GetPress(SECONDARY_ROTATE_BUTTON))
                    {
                        var angleA = Mathf.Atan2(_PrevFromTo.x, _PrevFromTo.z) * Mathf.Rad2Deg;
                        var angleB = Mathf.Atan2(newFromTo.x, newFromTo.z) * Mathf.Rad2Deg;
                        var angleDiff = Mathf.DeltaAngle(angleA, angleB);
                        VR.Camera.SteamCam.origin.transform.RotateAround(transform.position + newFromTo * 0.5f, Vector3.up, angleDiff);// Mathf.Max(1, Controller.velocity.sqrMagnitude) );

                        _ProspectedPlayArea.Rotation += angleDiff;
                    }

                    _PrevFromTo = (OtherController.transform.position - transform.position).normalized;
                }
                else
                {
                    var diffPos = transform.position - _PrevControllerPos;
                    var diffRot = Quaternion.Inverse(_PrevControllerRot * Quaternion.Inverse(transform.rotation)) * (transform.rotation * Quaternion.Inverse(transform.rotation));
                    if (Time.unscaledTime - _GripStartTime > GRIP_TIME_THRESHOLD || diffPos.magnitude > GRIP_DIFF_THRESHOLD)
                    {
                        var forwardA = Vector3.forward;
                        var forwardB = diffRot * Vector3.forward;
                        var angleA = Mathf.Atan2(forwardA.x, forwardA.z)*  Mathf.Rad2Deg;
                        var angleB = Mathf.Atan2(forwardB.x, forwardB.z)*  Mathf.Rad2Deg;
                        var angleDiff = Mathf.DeltaAngle(angleA, angleB);

                        VR.Camera.SteamCam.origin.transform.position -= diffPos;
                        _ProspectedPlayArea.Height -= diffPos.y;
                        //VRLog.Info("Rotate: {0}", NormalizeAngle(diffRot.eulerAngles.y));
                        //VR.Camera.SteamCam.origin.transform.RotateAround(transform.position, Vector3.up, angleDiff * 0.3f);// Mathf.Max(1, Controller.velocity.sqrMagnitude) );
                        // Handle rotation
                        //_ProspectedPlayArea.Reset();
                        //_ProspectedPlayArea.Rotation += diffRot.eulerAngles.y;
                        //_ProspectedPlayArea.Position -= Vector3.ProjectOnPlane(diffPos, Vector3.up);
                        //_ProspectedPlayArea.Height -= diffPos.y;
                        //_ProspectedPlayArea.Apply();
                        _GripStartTime = 0; // To make sure that pos is not reset
                    }
                }
            }
            if (Controller.GetPressUp(EVRButtonId.k_EButton_Grip))
            {
                EnterState(WarpState.None);
                if (Time.unscaledTime - _GripStartTime < GRIP_TIME_THRESHOLD)
                {
                    Owner.StartRumble(new RumbleImpulse(800));
                    _ProspectedPlayArea.Height = 0;
                    _ProspectedPlayArea.Scale = _IPDOnStart;
                }
            }
            _PrevControllerPos = transform.position;
            _PrevControllerRot = transform.rotation;

            CheckRotationalPress();
        }

        private float NormalizeAngle(float angle)
        {
            return angle % 360f;
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

                case WarpState.Grabbing:
                    Owner.StopRumble(_TravelRumble);
                    if (HasLock())
                    {
                        VRLog.Info("Releasing lock on other controller!");
                        _OtherLock.Release();
                    }
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
                    break;
                case WarpState.Grabbing:
                    _PrevControllerPos = transform.position;
                    _GripStartTime = Time.unscaledTime;
                    _TravelRumble.Reset();
                    _PrevControllerPos = transform.position;
                    _PrevControllerRot = transform.rotation;
                    Owner.StartRumble(_TravelRumble);
                    break;
            }

            State = state;
        }
        private bool HasLock()
        {
            return _OtherLock != null && _OtherLock.IsValid;
        }

        private void Reset()
        {
            _Points.Clear();
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
