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
        private static bool _IsFirstStart = true;

        protected GUIMonitor Monitor;

        protected override void OnStart()
        {
            base.OnStart();

            if(_IsFirstStart)
            {
                VR.Camera.SteamCam.origin.transform.position = new Vector3(0, 0, 0);
                Recenter();
                _IsFirstStart = false;
            }

            Monitor = GUIMonitor.Create();
            Monitor.transform.SetParent(VR.Camera.SteamCam.origin, false);
        }

        //protected virtual void OnLevel()
        //{
        //    _Master = Camera.main.transform;
        //}

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Move origin
            if (VR.Camera.Blueprint)
            {
                VR.Camera.SteamCam.origin.transform.position = VR.Camera.Blueprint.transform.position;
                VR.Camera.SteamCam.origin.transform.rotation = VR.Camera.Blueprint.transform.rotation;
            }
        }

        public override void Impersonate(IActor actor)
        {
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
                new KeyboardShortcut(new KeyStroke("KeypadMinus"), MoveGUI(0.1f), KeyMode.Press),
                new KeyboardShortcut(new KeyStroke("KeypadPlus"), MoveGUI(-.1f), KeyMode.Press),
                new KeyboardShortcut(new KeyStroke("F12"), Recenter)
            }.Concat(base.CreateShortcuts());
        }

        public void Recenter()
        {
            Logger.Info("Recenter");
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
