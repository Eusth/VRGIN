using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core.Controls;
using VRGIN.Core.Helpers;
using VRGIN.Core.Visuals;

namespace VRGIN.Core.Modes
{
    public class SeatedMode : ControlMode
    {
        private Transform _Master;
        protected GUIQuad Monitor;

        protected override void OnStart()
        {
            base.OnStart();
            
            _Master = Camera.main.transform;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            VR.Camera.SteamCam.origin.transform.position = _Master.position;
            VR.Camera.SteamCam.origin.transform.rotation = _Master.rotation;
        }

        public override void Impersonate(IActor actor)
        {
        }
        
        public override void OnDestroy()
        {
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
                new KeyboardShortcut(new KeyStroke("KeypadMinus"), MoveGUI(1)),
                new KeyboardShortcut(new KeyStroke("KeypadPlus"), MoveGUI(-1))
            }.Concat(base.CreateShortcuts());
        }

        public void Recenter()
        {
            OpenVR.System.ResetSeatedZeroPose();
        }

        protected Action MoveGUI(float speed)
        {
            return delegate
            {
                // Reposition monitor
            };
        }
        
    }
}
