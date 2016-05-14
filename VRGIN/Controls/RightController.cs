using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core.Controls
{
    public class RightController : Controller
    {
        public static RightController Create()
        {
            var rightHand = new GameObject("Right Controller").AddComponent<RightController>();

            return rightHand;
        }
        private static int S_ToolIndex = 1; // Start with warp tool

        public override int ToolIndex
        {
            get
            {
                return S_ToolIndex;
            }

            set
            {
                S_ToolIndex = value;

            }
        }
    }
}
