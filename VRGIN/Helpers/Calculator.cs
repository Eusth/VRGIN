using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers
{
    public static class Calculator
    {
        /// <summary>
        /// Turns a world distance into an invariant meter value.
        /// </summary>
        /// <param name="worldValue"></param>
        /// <returns></returns>
        public static float Distance(float worldValue)
        {
            return worldValue / VR.Settings.IPDScale * VR.Context.UnitToMeter;
        }
        

        /// <summary>
        /// Gets the signed angle between two vectors
        /// </summary>
        /// <returns></returns>
        public static float Angle(Vector3 v1, Vector3 v2)
        {
            var angleA = Mathf.Atan2(v1.x, v1.z) * Mathf.Rad2Deg;
            var angleB = Mathf.Atan2(v2.x, v2.z) * Mathf.Rad2Deg;
            return Mathf.DeltaAngle(angleA, angleB);
        }
    }
}
