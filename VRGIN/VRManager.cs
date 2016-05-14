using System;
using System.Collections;
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
        public static VRManager Manager { get { return VRManager.Instance; } }
    }

    public class VRManager : ProtectedBehaviour
    {
        private VRGUI _Gui;
        private bool _CameraLoaded = false;

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
                ModeType = typeof(T);

                // Change!
                if (Mode != null)
                {
                    // Get on clean grounds
                    GameObject.DestroyImmediate(Mode);
                }

                if (_CameraLoaded)
                {
                    Mode = VRCamera.Instance.gameObject.AddComponent<T>();
                }
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
            _CameraLoaded = false;
            Copy(Interpreter.FindCamera());

        }

        protected override void OnLevel(int level)
        {
            _CameraLoaded = false;
            Copy(Interpreter.FindCamera());
            //StartCoroutine(Load());
        }

        private IEnumerator Load()
        {
            for (int i = 0; i < 3; i++)
            {
                var camera = Interpreter.FindCamera();
                if (camera)
                {
                    Copy(camera);
                    yield break;
                }
                yield return null;
            }

            Copy(null);
        }

        private void Copy(Camera camera)
        {
            if (_CameraLoaded) return;
            VRCamera.Instance.Copy(camera);

            if (!Mode && ModeType != null && ModeType.IsSubclassOf(typeof(ControlMode)))
            {
                Mode = VRCamera.Instance.gameObject.AddComponent(ModeType) as ControlMode;
            }

            _CameraLoaded = true;
        }
    }

    public interface IVRManagerContext
    {
        string GuiLayer { get; }
        int UILayerMask { get; }
        Color PrimaryColor { get; }
        IMaterialPalette Materials { get; }
        VRSettings Settings { get; }
        string HMDLayer { get; }
    }
}
