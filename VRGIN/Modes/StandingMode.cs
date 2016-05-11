using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core.Modes
{
    public class StandingMode : ControlMode
    {
        public override void Impersonate(IActor actor)
        {

        }

        public override void OnDestroy()
        {

        }

        protected override void OnUpdate()
        {
            var origin = VRCamera.Instance.SteamCam.origin;

            Camera.main.transform.position = VR.Camera.transform.position;
            Camera.main.transform.rotation = VR.Camera.transform.rotation;
        }
    }
}
