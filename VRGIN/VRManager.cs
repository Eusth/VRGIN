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
        private VRGUI _Gui;

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
                ModeType = typeof(T);
            }
        }

        public ControlMode Mode
        {
            get;
            private set;
        }

        private static Type ModeType;


        protected override void OnAwake()
        {
            Application.targetFrameRate = 90;
            Time.fixedDeltaTime = 1 / 90f;
            Application.runInBackground = true;

            GameObject.DontDestroyOnLoad(SteamVR_Render.instance.gameObject);
            GameObject.DontDestroyOnLoad(gameObject);

            // Makes sure that the GUI is instanciated
            _Gui = VRGUI.Instance;

#if UNITY_4_5
            SteamVR_Render.instance.helpSeconds = 0;
#endif
        }
        protected override void OnStart()
        {
            VRCamera.Instance.Copy(Camera.main);
        }

        protected override void OnLevel(int level)
        {
            VRCamera.Instance.Copy(Camera.main);

            if(ModeType != null && ModeType.IsSubclassOf(typeof(ControlMode)))
            {
                Mode = VRCamera.Instance.gameObject.AddComponent(ModeType) as ControlMode;
            }
        }
    }

    public interface IVRManagerContext
    {
        string GuiLayer { get; }
        int UILayerMask { get; }
        Color PrimaryColor { get; }
        IMaterialPalette Materials { get; }
        VRSettings Settings { get; }
    }
}
