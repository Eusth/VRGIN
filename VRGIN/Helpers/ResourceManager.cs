using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers
{
    /// <summary>
    /// Asset bundle compatibility:
    /// 
    /// # 5.0
    /// 5.0
    /// 
    /// # 5.2
    /// 5.1, 5.2, 5.3?
    /// 
    /// # 5.3
    /// 5.3
    /// 
    /// # 5.4
    /// 5.4, 5.5?
    /// </summary>
    public static class ResourceManager
    {
        private static readonly string VERSION = string.Join(".", Application.unityVersion.Split('.').Take(2).ToArray());

        public static byte[] SteamVR
        {
            get
            {
                if(VERSION.CompareTo("5.0") <= 0)
                {
                    return Resource.steamvr_5_0;
                }
                if(VERSION.CompareTo("5.2") <= 0)
                {
                    return Resource.steamvr_5_2;
                }
                if(VERSION.CompareTo("5.3") <= 0)
                {
                    return Resource.steamvr_5_3;
                }
                if(VERSION.CompareTo("5.4") <= 0)
                {
                    return Resource.steamvr_5_4;
                }

                return Resource.steamvr_5_5;
            }
        }

        public static byte[] Capture
        {
            get
            {
                if(VERSION.CompareTo("5.0") <= 0)
                {
                    return Resource.capture_5_0;
                } 
                if (VERSION.CompareTo("5.2") <= 0)
                {
                    return Resource.capture_5_2;
                }
                if (VERSION.CompareTo("5.3") <= 0)
                {
                    return Resource.capture_5_3;
                }
                if (VERSION.CompareTo("5.4") <= 0)
                {
                    return Resource.capture_5_4;
                }
                return Resource.capture_5_5;
            }
        }

        public static byte[] Hands
        {
            get
            {
                return Resource.hands_5_3;
            }
        }


    }
}
