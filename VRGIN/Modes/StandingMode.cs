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

            var targetYaw = Quaternion.Euler(0, actor.Eyes.rotation.eulerAngles.y, 0);
            var myYaw = Quaternion.Euler(0, VR.Camera.SteamCam.head.eulerAngles.y, 0);
            VR.Camera.SteamCam.origin.rotation *= Quaternion.Inverse(myYaw) * targetYaw;


            var targetPosition = actor.Eyes.position;
            float targetY = mode == ImpersonationMode.Approximately ? 0 : targetPosition.y;
            float myY = mode == ImpersonationMode.Approximately ? 0 : VR.Camera.SteamCam.head.position.y;
            targetPosition = new Vector3(targetPosition.x, targetY, targetPosition.z);
            var myPosition = new Vector3(VR.Camera.SteamCam.head.position.x, myY, VR.Camera.SteamCam.head.position.z);
            VR.Camera.SteamCam.origin.position += (targetPosition - myPosition);

            OpenVR.ChaperoneSetup.ReloadFromDisk(EChaperoneConfigFile.Live);
        }

        public override void OnDestroy()
        {

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

            var origin = VRCamera.Instance.SteamCam.origin;

            VRCamera.Instance.Blueprint.transform.position = VR.Camera.transform.position;
            VRCamera.Instance.Blueprint.transform.rotation = VR.Camera.transform.rotation;
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
