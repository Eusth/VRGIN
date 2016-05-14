using UnityEngine;

namespace VRGIN.Core.Controls
{

    public class LeftController : Controller
    {
        public static LeftController Create()
        {
            var leftHand = new GameObject("Left Controller").AddComponent<LeftController>();
            
            return leftHand;
        }
        
        private static int S_ToolIndex = 0;

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
