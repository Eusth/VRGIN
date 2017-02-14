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

        /// <summary>
        /// Gets the "strongest" forward vector on the Y plane.
        /// This might be a little roundabout, but it seems to work...
        /// </summary>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public static Vector3 GetForwardVector(Quaternion rotation)
        {
            var rotatedForward = rotation * Vector3.forward;
            return new Vector3[] {
                Vector3.ProjectOnPlane(rotatedForward, Vector3.up),
                Vector3.ProjectOnPlane(rotation * (rotatedForward.y > 0f ? Vector3.down : Vector3.up), Vector3.up)
            }.OrderByDescending(v => v.sqrMagnitude).First().normalized;
        }
    }
}
