using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        
    }
}
