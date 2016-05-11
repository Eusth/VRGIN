using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core.Modes;
using VRGIN.Core.Visuals;

namespace VRGIN.Core
{
    /// <summary>
    /// Helper class that gives you easy access to all crucial objects.
    /// </summary>
    public static class VR
    {
        public static GameInterpreter Interpreter { get { return VRManager.Instance.Interpreter; } }
        public static VRCamera Camera { get { return VRCamera.Instance; } }
        public static VRGUI GUI { get { return VRGUI.Instance; } }
        public static IVRManagerContext Context { get { return VRManager.Instance.Context; } }
        public static ControlMode Mode { get { return VRManager.Instance.Mode; } }
        public static VRSettings Settings { get { return Context.Settings; } }
    }

    public class VRManager : ProtectedBehaviour
    {
        private static VRManager _Instance;
        public static VRManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    throw new InvalidOperationException("VR Manager has not been created yet!");
                }
                return _Instance;
            }
        }

        public IVRManagerContext Context { get; private set; }
        public GameInterpreter Interpreter { get; private set; }

        public static VRManager Create<T>(IVRManagerContext context) where T : GameInterpreter
        {
            if(_Instance == null)
            {
                _Instance = new GameObject("VR Manager").AddComponent<VRManager>();
                _Instance.Context = context;
                _Instance.Interpreter = _Instance.gameObject.AddComponent<T>();
            }
            return _Instance;
        }

        public void SetMode<T>() where T : ControlMode
        {
            
            if(Mode == null || !(Mode is T))
            {
                // Change!
                if(Mode != null)
                {
                    // Get on clean grounds
                    GameObject.DestroyImmediate(Mode);
                }

                Mode = VRCamera.Instance.gameObject.AddComponent<T>();
            }
        }

        public ControlMode Mode
        {
            get;
            private set;
        }


        protected override void OnStart()
        {
            VRCamera.Instance.Copy(Camera.main);
        }

        protected override void OnLevel(int level)
        {
            VRCamera.Instance.Copy(Camera.main);
        }
    }

    public interface IVRManagerContext
    {
        string GuiLayer { get; }
        int UILayerMask { get; }
        Color PrimaryColor { get; }
        Type LeftControllerType { get; }
        Type RightControllerType { get; }
        IMaterialPalette Materials { get; }
        VRSettings Settings { get; }
    }
}
