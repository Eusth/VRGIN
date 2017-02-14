using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Controls.Speech;
using VRGIN.Modes;
using VRGIN.Visuals;
using WindowsInput;

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
        public static Shortcuts Shortcuts { get { return Context.Settings.Shortcuts; } }
        public static VRManager Manager { get { return VRManager.Instance; } }
        public static InputSimulator Input { get { return VRManager.Instance.Input; } }
        public static SpeechManager Speech { get { return VRManager.Instance.Speech; } }
        public static HMDType HMD { get { return VRManager.Instance.HMD; } } 
    }

    public enum HMDType
    {
        Oculus,
        Vive,
        Other
    }

    public class ModeInitializedEventArgs : EventArgs
    {
        public ControlMode Mode { get; private set; }

        public ModeInitializedEventArgs(ControlMode mode)
        {
            Mode = mode;
        }
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
        public SpeechManager Speech { get; private set; }
        public HMDType HMD { get; private set; }

        public event EventHandler<ModeInitializedEventArgs> ModeInitialized = delegate { };

        /// <summary>
        /// Creates the manager with a context and an interpeter.
        /// </summary>
        /// <typeparam name="T">The interpreter that keeps track of actors and cameras, etc.</typeparam>
        /// <param name="context">The context of the game (materials, layers, settings...)</param>
        /// <returns></returns>
        public static VRManager Create<T>(IVRManagerContext context) where T : GameInterpreter
        {
            if (_Instance == null)
            {
                _Instance = new GameObject("VRGIN_Manager").AddComponent<VRManager>();
                _Instance.Context = context;
                _Instance.Interpreter = _Instance.gameObject.AddComponent<T>();
                // Makes sure that the GUI is instanciated
                _Instance._Gui = VRGUI.Instance;
                _Instance.Input = new InputSimulator();

                if (VR.Settings.SpeechRecognition)
                {
                    _Instance.Speech = _Instance.gameObject.AddComponent<SpeechManager>();
                }

                // Save settings so the XML is up-to-date
                VR.Settings.Save();

            }
            return _Instance;
        }

        /// <summary>
        /// Sets the mode the game works in.
        /// 
        /// A mode is required for the VR support to work. Refer to <see cref="SeatedMode"/> and <see cref="StandingMode"/> for
        /// example implementations. It is recommended to extend them.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void SetMode<T>() where T : ControlMode
        {
            if (Mode == null || !(Mode is T))
            {
                ModeType = typeof(T);

                // Change!
                if (Mode != null)
                {
                    // Get on clean grounds
                    Mode.ControllersCreated -= OnControllersCreated;
                    DestroyImmediate(Mode);
                }

                if (_CameraLoaded)
                {
                    Mode = VRCamera.Instance.gameObject.AddComponent<T>();
                    Mode.ControllersCreated += OnControllersCreated;
                }
            }
        }

        public ControlMode Mode
        {
            get;
            private set;
        }
        public InputSimulator Input { get; internal set; }

        private static Type ModeType;

        protected override void OnAwake()
        {
            var trackingSystem = SteamVR.instance.hmd_TrackingSystemName;
            VRLog.Info("------------------------------------");
            VRLog.Info(" Booting VR [{0}]", trackingSystem);
            VRLog.Info("------------------------------------");
            HMD = trackingSystem == "oculus" ? HMDType.Oculus : trackingSystem == "lighthouse" ? HMDType.Vive : HMDType.Other;

            Application.targetFrameRate = 90;
            Time.fixedDeltaTime = 1 / 90f;
            Application.runInBackground = true;

            GameObject.DontDestroyOnLoad(SteamVR_Render.instance.gameObject);
            GameObject.DontDestroyOnLoad(gameObject);
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


        private void Copy(Camera camera)
        {
            if (_CameraLoaded) return;
            VRCamera.Instance.Copy(camera);

            if (!Mode && ModeType != null && ModeType.IsSubclassOf(typeof(ControlMode)))
            {
                Mode = VRCamera.Instance.gameObject.AddComponent(ModeType) as ControlMode;
                Mode.ControllersCreated += OnControllersCreated;
            }

            _CameraLoaded = true;
        }

        private void OnControllersCreated(object sender, EventArgs e)
        {
            ModeInitialized(this, new ModeInitializedEventArgs(Mode));
        }
    }
}
