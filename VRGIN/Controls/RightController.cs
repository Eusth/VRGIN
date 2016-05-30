using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Controls
{
    public class RightController : Controller
    {
        public static RightController Create()
        {
            var rightHand = new GameObject("Right Controller").AddComponent<RightController>();
            rightHand.ToolIndex = 1; // Start with tool 2
            return rightHand;
        }
    }
}
