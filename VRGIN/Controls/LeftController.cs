using UnityEngine;

namespace VRGIN.Controls
{

    public class LeftController : Controller
    {
        public static LeftController Create()
        {
            var leftHand = new GameObject("Left Controller").AddComponent<LeftController>();
            
            return leftHand;
        }
    }
}
