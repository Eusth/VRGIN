using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core.Controls;

namespace VRGIN.Core.Visuals
{
    public class GUIMonitor : GUIQuad
    {
        public float Angle = 0;
        public float Curviness = 1;
        public float Distance = 0;

        private ProceduralPlane _Plane;
        
        protected override void OnStart()
        {
            base.OnStart();

            _Plane = GetComponent<ProceduralPlane>();
            _Plane.xSegments = 100;
            if(_Plane)
            {
                Logger.Info("Plane was added...");
            } else
            {
                Logger.Info("No plane either?");
            }
            UpdateGUI(true, true);

            Rebuild();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            VR.Settings.PropertyChanged += OnPropertyChanged;
        }


        protected override void OnDisable()
        {
            base.OnDisable();
            VR.Settings.PropertyChanged -= OnPropertyChanged;
        }

        new public static GUIMonitor Create()
        {
            var monitor = new GameObject("GUI Monitor").AddComponent<ProceduralPlane>().gameObject.AddComponent<GUIMonitor>();
            return monitor;
        }

        public override void UpdateAspect()
        {
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_Plane)
            {
                switch (e.PropertyName) {
                    case "Angle":
                    case "OffsetY":
                    case "Distance":
                         Rebuild();
                        break;
                }
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            //var mode = VR.Mode;
            //GetComponent<Renderer>().enabled = !(  mode.Left.ActiveTool is MenuTool
            //                                    || mode.Right.ActiveTool is MenuTool );
        }

        public void Rebuild()
        {
            Logger.Info("Build monitor");
            try
            {
                transform.localPosition = new Vector3(transform.localPosition.x, VR.Settings.OffsetY, transform.localPosition.z);
                transform.localScale = Vector3.one * VR.Settings.Distance;
                _Plane.angleSpan = VR.Settings.Angle;
                _Plane.curviness = Curviness;
                _Plane.height= (VR.Settings.Angle / 100);
                _Plane.distance = 1;
                _Plane.Rebuild();
            } catch(Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
