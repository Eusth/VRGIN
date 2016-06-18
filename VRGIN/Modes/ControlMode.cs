using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Visuals;

namespace VRGIN.Modes
{
    public enum ImpersonationMode
    {
        Approximately,
        Exactly
    }

    public abstract class ControlMode : ProtectedBehaviour
    {
        private static bool _ControllerFound = false;


        public virtual void Impersonate(IActor actor)
        {
            this.Impersonate(actor, ImpersonationMode.Approximately);
        }

        public virtual void Impersonate(IActor actor, ImpersonationMode mode)
        {
            if (actor != null)
            {
                actor.HasHead = false;
            }
        }

        public abstract ETrackingUniverseOrigin TrackingOrigin { get; }

        public Controller Left { get; private set; }
        public Controller Right { get; private set; }

        protected IEnumerable<IShortcut> Shortcuts { get; private set; }

        protected SteamVR_ControllerManager ControllerManager;
        internal event EventHandler<EventArgs> ControllersCreated = delegate { };

        protected override void OnStart()
        {
            CreateControllers();

            Shortcuts = CreateShortcuts();
            SteamVR_Render.instance.trackingSpace = TrackingOrigin;
        }

        protected virtual void OnEnable()
        {
            SteamVR_Utils.Event.Listen("device_connected", OnDeviceConnected);
        }

        protected virtual void OnDisable()
        {
            SteamVR_Utils.Event.Listen("device_connected", OnDeviceConnected);
        }

        /// <summary>
        /// Creates both controllers by using <see cref="CreateRightController"/> and <see cref="CreateLeftController"/>.
        /// Override those methods to change the controller implementation to be used.
        /// </summary>
        protected virtual void CreateControllers()
        {
            var steamCam = VR.Camera.SteamCam;

            steamCam.origin.gameObject.SetActive(false);
            {
                ControllerManager = steamCam.origin.gameObject.AddComponent<SteamVR_ControllerManager>();

                Left = CreateLeftController();
                Left.transform.SetParent(steamCam.origin);

                Right = CreateRightController();
                Right.transform.SetParent(steamCam.origin);

                Left.Other = Right;
                Right.Other = Left;

                ControllerManager.left = Left.gameObject;
                ControllerManager.right = Right.gameObject;
            }
            steamCam.origin.gameObject.SetActive(true);

            VRLog.Info("---- Initialize left tools");
            InitializeTools(Left, true);

            VRLog.Info("---- Initialize right tools");
            InitializeTools(Right, false);

            ControllersCreated(this, new EventArgs());
        }

        public virtual void OnDestroy()
        {
            Destroy(ControllerManager);
            Destroy(Left);
            Destroy(Right);
        }

        protected virtual void InitializeTools(Controller controller, bool isLeft)
        {
            // Combine
            var toolTypes = Tools.Concat(isLeft ? LeftTools : RightTools).Distinct();

            foreach (var type in toolTypes)
            {
                controller.AddTool(type);
            }

            VRLog.Info("{0} tools added", toolTypes.Count());
        }

        protected virtual Controller CreateLeftController()
        {
            return LeftController.Create();
        }

        protected virtual Controller CreateRightController()
        {
            return RightController.Create();
        }

        public virtual IEnumerable<Type> Tools
        {
            get { return new List<Type>(); }
        }

        public virtual IEnumerable<Type> LeftTools
        {
            get { return new List<Type>(); }
        }

        public virtual IEnumerable<Type> RightTools
        {
            get { return new List<Type>(); }
        }

        protected virtual IEnumerable<IShortcut> CreateShortcuts()
        {
            return new List<IShortcut>()
            {
                new KeyboardShortcut(new KeyStroke("Alt + KeypadMinus"), delegate { VR.Settings.IPDScale += Time.deltaTime * 0.1f; }, KeyMode.Press ),
                new KeyboardShortcut(new KeyStroke("Alt + KeypadPlus"), delegate { VR.Settings.IPDScale -= Time.deltaTime * 0.1f; }, KeyMode.Press ),
                new MultiKeyboardShortcut(new KeyStroke("Ctrl + C"), new KeyStroke("Ctrl + D"), delegate { UnityHelper.DumpScene("dump.json"); } ),
                new MultiKeyboardShortcut(new KeyStroke("Ctrl + C"), new KeyStroke("Ctrl + V"), ToggleUserCamera),
                new KeyboardShortcut(new KeyStroke("Alt + S"), delegate { VR.Settings.Save(); }),
                new KeyboardShortcut(new KeyStroke("Alt + L"), delegate { VR.Settings.Reload(); }),
                new KeyboardShortcut(new KeyStroke("Ctrl + Alt + L"), delegate { VR.Settings.Reset(); }),
                //new MultiKeyboardShortcut(new KeyStroke("Ctrl + C"), new KeyStroke("Ctrl+B"), delegate {
                //    ProtectedBehaviour.DumpTable();
                //})
                new KeyboardShortcut(new KeyStroke("Ctrl + F5"), delegate { VR.Camera.CopyFX(VR.Camera.Blueprint); }, KeyMode.PressUp),

            };
        }

        protected virtual void ToggleUserCamera()
        {
            if (!PlayerCamera.Created)
            {
                VRLog.Info("Create user camera");

                PlayerCamera.Create();
            }
            else
            {
                VRLog.Info("Remove user camera");

                PlayerCamera.Remove();
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            SteamVR_Render.instance.trackingSpace = TrackingOrigin;

            // Update head visibility

            var steamCam = VRCamera.Instance.SteamCam;
            int i = 0;

            bool allActorsHaveHeads = VR.Interpreter.IsEveryoneHeaded;

            foreach (var actor in VR.Interpreter.Actors)
            {
                if (actor.HasHead)
                {
                    if (allActorsHaveHeads)
                    {
                        var hisPos = actor.Eyes.position;
                        var hisForward = actor.Eyes.forward;

                        var myPos = steamCam.head.position;
                        var myForward = steamCam.head.forward;

                        VRLog.Debug("Actor #{0} -- He: {1} -> {2} | Me: {3} -> {4}", i, hisPos, hisForward, myPos, myForward);
                        if (Vector3.Distance(hisPos, myPos) < 0.15f && Vector3.Dot(hisForward, myForward) > 0.6f)
                        {
                            actor.HasHead = false;
                        }
                    }
                }
                else
                {
                    if (Vector3.Distance(actor.Eyes.position, steamCam.head.position) > 0.3f)
                    {
                        actor.HasHead = true;
                    }
                }
                i++;
            }
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            foreach (var shortcut in Shortcuts)
            {
                shortcut.Evaluate();
            }
        }

        private void OnDeviceConnected(object[] args)
        {
            if (!_ControllerFound)
            {
                var index = (uint)(int)args[0];
                var connected = (bool)args[1];
                VRLog.Info("Device connected: {0}", index);

                if (connected && index > OpenVR.k_unTrackedDeviceIndex_Hmd)
                {
                    var system = OpenVR.System;
                    if (system != null && system.GetTrackedDeviceClass(index) == ETrackedDeviceClass.Controller)
                    {
                        _ControllerFound = true;

                        // Switch to standing mode
                        ChangeModeOnControllersDetected();
                    }
                }
            }
        }

        protected virtual void ChangeModeOnControllersDetected()
        {
        }
    }
}
