using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Modes;
using VRGIN.Visuals;

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

    public class ModeInitializedEventArgs : EventArgs
    {
        public ControlMode Mode { get; private set; }

        public ModeInitializedEventArgs(ControlMode mode) {
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
        public event EventHandler<ModeInitializedEventArgs> ModeInitialized = delegate { };

        /// <summary>
        /// Creates the manager with a context and an interpeter.
        /// </summary>
        /// <typeparam name="T">The interpreter that keeps track of actors and cameras, etc.</typeparam>
        /// <param name="context">The context of the game (materials, layers, settings...)</param>
        /// <returns></returns>
        public static VRManager Create<T>(IVRManagerContext context) where T : GameInterpreter
        {
            if(_Instance == null)
            {
                _Instance = new GameObject("VR Manager").AddComponent<VRManager>();
                _Instance.Context = context;
                _Instance.Interpreter = _Instance.gameObject.AddComponent<T>();
                _Instance._Gui = VRGUI.Instance;

                // Makes sure that the GUI is instanciated
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
            if(Mode == null || !(Mode is T))
            {
                ModeType = typeof(T);

                // Change!
                if (Mode != null)
                {
                    // Get on clean grounds
                    Mode.ControllersCreated -= OnControllersCreated;
                    GameObject.DestroyImmediate(Mode);
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

        private static Type ModeType;


        protected override void OnAwake()
        {
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

    public interface IVRManagerContext
    {
        /// <summary>
        /// Gets the layer where the VR GUI should be placed. This is mainly used for raycasting and should ideally not be used by anything else.
        /// </summary>
        string GuiLayer { get; }

        /// <summary>
        /// Gets the layer the game uses for its UI.
        /// </summary>
        string UILayer { get; }

        /// <summary>
        /// Gets the mask that can be used for the camera to *not* display the game's GUI. The VR cameras will ignore this, the GUI camera will look for this.
        /// This is almost the same as <see cref="UILayer"/> but more flexible.
        /// </summary>
        int UILayerMask { get; }

        /// <summary>
        /// Gets the color used for the tools and effects. (e.g. teleport)
        /// </summary>
        Color PrimaryColor { get; }

        /// <summary>
        /// Gets the palette that contains all materials used by the library.
        /// </summary>
        IMaterialPalette Materials { get; }

        /// <summary>
        /// Gets the settings object.
        /// </summary>
        VRSettings Settings { get; }

        /// <summary>
        /// Gets the layer that can be used to add objects that will be ignored by the in-game player but that will appear on screen.
        /// </summary>
        string HMDLayer { get; }

        /// <summary>
        /// Gets a list of canvas names that should be ignored entirely.
        /// </summary>
        string[] IgnoredCanvas { get; }

        /// <summary>
        /// Gets whether the library should make a cursor of its own. Needed when the game uses a hardware cursor.
        /// </summary>
        bool SimulateCursor { get; }

        /// <summary>
        /// Gets whether or not the GUI should run in an alternative mode with custom sorting.
        /// </summary>
        bool GUIAlternativeSortingMode { get; }
    }
}
