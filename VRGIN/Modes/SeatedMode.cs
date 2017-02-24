using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Leap;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Visuals;
using static VRGIN.Visuals.GUIMonitor;

namespace VRGIN.Modes
{
    public enum LockMode
    {
        None,
        XZPlane
    }

    public class SeatedMode : ControlMode
    {

        private static bool _IsFirstStart = true;

        protected GUIMonitor Monitor;
        protected IActor LockTarget;
        protected ImpersonationMode LockMode;

        protected override void OnStart()
        {
            base.OnStart();
            
            if (_IsFirstStart)
            {
                VR.Camera.SteamCam.origin.transform.position = new Vector3(0, 0, 0);
                Recenter();
                _IsFirstStart = false;
            }

            Monitor = GUIMonitor.Create();
            Monitor.transform.SetParent(VR.Camera.SteamCam.origin, false);
            
            OpenVR.ChaperoneSetup.SetWorkingPlayAreaSize(1000, 1000); // Make it really big
            //OpenVR.Chaperone.ForceBoundsVisible(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        //protected virtual void OnLevel()
        //{
        //    _Master = Camera.main.transform;
        //}

        private void OnLeapConnect(object sender, ConnectionEventArgs e)
        {
            ChangeModeOnControllersDetected();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Move origin
            if (VR.Camera.HasValidBlueprint && VR.Camera.Blueprint)
            {
                if (LockTarget != null && LockTarget.IsValid)
                {
                    VR.Camera.Blueprint.transform.position = LockTarget.Eyes.position;

                    if (LockMode == ImpersonationMode.Approximately)
                    {
                        VR.Camera.Blueprint.transform.eulerAngles = new Vector3(0, LockTarget.Eyes.eulerAngles.y, 0);
                    }
                    else
                    {
                        VR.Camera.Blueprint.transform.rotation = LockTarget.Eyes.rotation;
                    }
                }

                VR.Camera.SteamCam.origin.transform.position = VR.Camera.Blueprint.transform.position;

                if ((VR.Settings.PitchLock && LockTarget == null))
                {
                    VR.Camera.SteamCam.origin.transform.eulerAngles = new Vector3(0, VR.Camera.Blueprint.transform.eulerAngles.y, 0);

                    CorrectRotationLock();
                }
                else
                {
                    VR.Camera.SteamCam.origin.transform.rotation = VR.Camera.Blueprint.transform.rotation;
                }
            }
        }

        protected virtual void SyncCameras()
        {
        }

        protected virtual void CorrectRotationLock()
        {

        }

        public override void Impersonate(IActor actor, ImpersonationMode mode)
        {
            base.Impersonate(actor, mode);

            SyncCameras();
            LockTarget = actor;
            LockMode = mode;
            Recenter();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            Destroy(Monitor.gameObject);
        }

        public override IEnumerable<Type> Tools
        {
            get
            {
                return base.Tools.Concat(new Type[] { typeof(MenuTool) });
            }
        }

        public override ETrackingUniverseOrigin TrackingOrigin
        {
            get
            {
                return ETrackingUniverseOrigin.TrackingUniverseSeated;
            }
        }


        protected override IEnumerable<IShortcut> CreateShortcuts()
        {
            return new List<IShortcut>() {
                new KeyboardShortcut(VR.Shortcuts.GUIRaise, MoveGUI(0.1f)),
                new KeyboardShortcut(VR.Shortcuts.GUILower, MoveGUI(-.1f)),
                new KeyboardShortcut(VR.Shortcuts.GUIIncreaseAngle, delegate { VR.Settings.Angle += Time.deltaTime * 50f; }),
                new KeyboardShortcut(VR.Shortcuts.GUIDecreaseAngle, delegate { VR.Settings.Angle -= Time.deltaTime * 50f; }),
                new KeyboardShortcut(VR.Shortcuts.GUIIncreaseDistance, delegate { VR.Settings.Distance += Time.deltaTime * 0.1f; }),
                new KeyboardShortcut(VR.Shortcuts.GUIDecreaseDistance, delegate { VR.Settings.Distance -= Time.deltaTime * 0.1f; }),
                new KeyboardShortcut(VR.Shortcuts.GUIRotateLeft, delegate { VR.Settings.Rotation += Time.deltaTime * 50f; }),
                new KeyboardShortcut(VR.Shortcuts.GUIRotateRight, delegate { VR.Settings.Rotation -= Time.deltaTime * 50f; }),
                new KeyboardShortcut(VR.Shortcuts.GUIChangeProjection, ChangeProjection),
                new MultiKeyboardShortcut(VR.Shortcuts.ToggleRotationLock, ToggleRotationLock),
                new MultiKeyboardShortcut(VR.Shortcuts.ImpersonateApproximately, delegate { if(LockTarget == null || !LockTarget.IsValid) { Impersonate(VR.Interpreter.FindNextActorToImpersonate(), ImpersonationMode.Approximately); } else { Impersonate(null); } }),
                new MultiKeyboardShortcut(VR.Shortcuts.ImpersonateExactly, delegate { if(LockTarget == null || !LockTarget.IsValid) { Impersonate(VR.Interpreter.FindNextActorToImpersonate(), ImpersonationMode.Exactly); } else { Impersonate(null); } }),
                new MultiKeyboardShortcut(VR.Shortcuts.ResetView, Recenter)
            }.Concat(base.CreateShortcuts());
        }

        private void ToggleRotationLock()
        {
            SyncCameras();
            VR.Settings.PitchLock = !VR.Settings.PitchLock;
        }

        private void ChangeProjection()
        {
            VR.Settings.Projection = (CurvinessState)(((int)VR.Settings.Projection + 1) % Enum.GetValues(typeof(CurvinessState)).Length);
        }

        public void Recenter()
        {
            VRLog.Info("Recenter");
            OpenVR.System.ResetSeatedZeroPose();
        }

        protected Action MoveGUI(float speed)
        {
            return delegate
            {
                VR.Settings.OffsetY += speed * Time.deltaTime;
            };
        }

    }
}
