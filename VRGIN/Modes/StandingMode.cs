using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;

namespace VRGIN.Modes
{
    public class StandingMode : ControlMode
    {

        public override void Impersonate(IActor actor, ImpersonationMode mode)
        {
            base.Impersonate(actor, mode);

            MoveToPosition(actor.Eyes.position, actor.Eyes.rotation, mode == ImpersonationMode.Approximately);

        }

        protected void MoveToPosition(Vector3 targetPosition, bool ignoreHeight = true)
        {
            MoveToPosition(targetPosition, VR.Camera.SteamCam.head.rotation, ignoreHeight);
        }

        protected void MoveToPosition(Vector3 targetPosition, Quaternion rotation = default(Quaternion), bool ignoreHeight = true)
        {
            var targetForward = GetForwardVector(rotation);
            var currentForward = GetForwardVector(VR.Camera.SteamCam.head.rotation);

            VR.Camera.SteamCam.origin.rotation *= Quaternion.FromToRotation(currentForward, targetForward);

            float targetY = ignoreHeight ? 0 : targetPosition.y;
            float myY = ignoreHeight ? 0 : VR.Camera.SteamCam.head.position.y;
            targetPosition = new Vector3(targetPosition.x, targetY, targetPosition.z);
            var myPosition = new Vector3(VR.Camera.SteamCam.head.position.x, myY, VR.Camera.SteamCam.head.position.z);
            VR.Camera.SteamCam.origin.position += (targetPosition - myPosition);
        }

        /// <summary>
        /// Gets the "strongest" forward vector on the Y plane.
        /// This might be a little roundabout, but it seems to work...
        /// </summary>
        /// <param name="rotation"></param>
        /// <returns></returns>
        private Vector3 GetForwardVector(Quaternion rotation)
        {
            var rotatedForward = rotation * Vector3.forward;
            return new Vector3[] {
                Vector3.ProjectOnPlane(rotatedForward, Vector3.up),
                Vector3.ProjectOnPlane(rotation * (rotatedForward.y > 0f ? Vector3.down : Vector3.up), Vector3.up)
            }.OrderByDescending(v => v.sqrMagnitude).First().normalized;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnStart()
        {
            base.OnStart();

            VR.Camera.SteamCam.origin.position = Vector3.zero;
            VR.Camera.SteamCam.origin.rotation = Quaternion.identity;

        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (VRCamera.Instance.HasValidBlueprint)
            {
                SyncCameras();
            }
        }

        protected virtual void SyncCameras()
        {
            VRCamera.Instance.Blueprint.transform.position = VR.Camera.SteamCam.head.position;
            VRCamera.Instance.Blueprint.transform.rotation = VR.Camera.SteamCam.head.rotation;
        }

        public override IEnumerable<Type> Tools
        {
            get
            {
                return base.Tools.Concat(new Type[] { typeof(MenuTool), typeof(WarpTool) });
            }
        }

        public override ETrackingUniverseOrigin TrackingOrigin
        {
            get
            {
                return ETrackingUniverseOrigin.TrackingUniverseStanding;
            }
        }
    }
}
