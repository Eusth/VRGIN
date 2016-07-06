using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Core;

namespace VRGIN.Visuals
{
    public class GUIMonitor : GUIQuad
    {

        public enum CurvinessState
        {
            Flat = 0,
            Curved = 1,
            Spherical = 2
        }

        public CurvinessState TargetCurviness = VR.Settings.Projection;
        private float _Curviness = 1;

        public float Angle = 0;
        public float Distance = 0;

        private ProceduralPlane _Plane;
        
        protected override void OnStart()
        {
            base.OnStart();

            _Plane = GetComponent<ProceduralPlane>();
            _Plane.xSegments = 100;
            if(_Plane)
            {
                VRLog.Info("Plane was added...");
            } else
            {
                VRLog.Info("No plane either?");
            }
            UpdateGUI();

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
                    case "Rotation":
                         Rebuild();
                        break;
                    case "Projection":
                        TargetCurviness = VR.Settings.Projection;
                        break;
                }
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if(Mathf.Abs(_Curviness - (int)TargetCurviness) > float.Epsilon)
            {
                _Curviness = Mathf.MoveTowards(_Curviness, (float)TargetCurviness, Time.deltaTime * 5);
                Rebuild();
            }
            //var mode = VR.Mode;
            //GetComponent<Renderer>().enabled = !(  mode.Left.ActiveTool is MenuTool
            //                                    || mode.Right.ActiveTool is MenuTool );
        }

        public void Rebuild()
        {
            VRLog.Info("Build monitor");
            try
            {
                transform.localPosition = new Vector3(transform.localPosition.x, VR.Settings.OffsetY, transform.localPosition.z);
                transform.localScale = Vector3.one * VR.Settings.Distance;
                transform.localRotation = Quaternion.Euler(0f, VR.Settings.Rotation, 0f);
                _Plane.angleSpan = VR.Settings.Angle;
                _Plane.curviness = _Curviness;
                _Plane.height= (VR.Settings.Angle / 100);
                _Plane.distance = 1;
                _Plane.Rebuild();
            } catch(Exception e)
            {
                VRLog.Error(e);
            }
        }
    }
}
