using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core.Controls;
using VRGIN.Core.Helpers;

namespace VRGIN.Core.Modes
{
    public abstract class ControlMode : ProtectedBehaviour
    {
        public abstract void Impersonate(IActor actor);

        public abstract ETrackingUniverseOrigin TrackingOrigin { get; }

        public Controller Left { get; private set; }
        public Controller Right { get; private set; }

        protected IEnumerable<IShortcut> Shortcuts { get; private set; }

        protected SteamVR_ControllerManager ControllerManager;

        protected override void OnStart()
        {
            CreateControllers();

            Shortcuts = CreateShortcuts();
            SteamVR_Render.instance.trackingSpace = TrackingOrigin;
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

            Logger.Info("---- Initialize left tools");
            InitializeTools(Left, true);

            Logger.Info("---- Initialize right tools");
            InitializeTools(Right, false);
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

            foreach(var type in toolTypes)
            {
                controller.AddTool(type);
            }

            Logger.Info("{0} tools added" , toolTypes.Count());
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
                new KeyboardShortcut(new KeyStroke("Alt + KeypadMinus"), delegate { VR.Settings.IPDScale += Time.deltaTime; } ),
                new KeyboardShortcut(new KeyStroke("Alt + KeypadPlus"), delegate { VR.Settings.IPDScale -= Time.deltaTime; } ),
                //new KeyboardShortcut(new KeyStroke("Ctrl + F5"), delegate { VR.Camera.CopyFX(Camera.main); }, KeyMode.PressUp),

            };
        }
        
        protected override void OnUpdate()
        {
            base.OnUpdate();

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

                        Logger.Debug("Actor #{0} -- He: {1} -> {2} | Me: {3} -> {4}", i, hisPos, hisForward, myPos, myForward);
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
            
            foreach(var shortcut in Shortcuts)
            {
                shortcut.Evaluate();
            }
        }
    }
}
