using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core.Visuals
{
    [RequireComponent(typeof(ProceduralPlane))]
    class GUIMonitor : GUIQuad
    {
        public float Angle = 0;
        public float Curviness = 0;
        public float Distance = 0;

        private ProceduralPlane _Plane;
        protected override void OnStart()
        {
            base.OnStart();
            _Plane = GetComponent<ProceduralPlane>();
        }

        public void Rebuild()
        {
            _Plane.angleSpan = Angle;
            _Plane.curviness = Curviness;
            _Plane.distance = Distance;
            _Plane.Rebuild();    
        }
    }
}
