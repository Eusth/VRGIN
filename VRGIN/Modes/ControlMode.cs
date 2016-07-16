using Leap.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Controls.LeapMotion;
using VRGIN.Controls.Speech;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.U46.Controls.Leap;
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

        /// <summary>
        /// Gets the left controller.
        /// </summary>
        public Controller Left { get; private set; }

        /// <summary>
        /// Gets the right controller.
        /// </summary>
        public Controller Right { get; private set; }

        /// <summary>
        /// Gets the left hand.
        /// </summary>
        public HandAttachments LeftHand { get; private set; }

        /// <summary>
        /// Gets the left hand.
        /// </summary>
        public HandModel LeftGraphicalHand { get; private set; }


        /// <summary>
        /// Gets the right hand.
        /// </summary>
        public HandAttachments RightHand { get; private set; }

        public HandModel RightGraphicalHand { get; private set; }


        public LeapServiceProvider LeapMotion { get; private set; }

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

        static int cnter = 0;
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

                if (VR.Settings.Leap)
                {
                    LeapMotion = CreateLeapHandController();
                    LeapMotion.transform.name = "Leap Motion Controller (" + (++cnter) + ")";
                    LeapMotion.transform.SetParent(steamCam.head.transform, false);
                    LeapMotion.transform.localRotation = Quaternion.Euler(-90f, 180f, 0);
                    LeapMotion.transform.localPosition += Vector3.forward * 0.08f;
                }
                
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

        private LeapServiceProvider CreateLeapHandController()
        {
            var serviceProvider =  new GameObject("LeapHandController").AddComponent<LeapServiceProvider>();
            var handController = serviceProvider.gameObject.AddComponent<LeapHandController>();
            var handPool = handController.gameObject.AddComponent<HandPool>();
            handController.gameObject.AddComponent<PinchController>();
            serviceProvider._isHeadMounted = true;

            LeftGraphicalHand = BuildGraphicalHand(Chirality.Left);
            RightGraphicalHand = BuildGraphicalHand(Chirality.Right);
            LeftHand = BuildAttachmentHand(Chirality.Left);
            RightHand = BuildAttachmentHand(Chirality.Right);
            
            handPool.ModelPool = new List<HandPool.ModelGroup>();
            handPool.ModelPool.Add(new HandPool.ModelGroup()
            {
                GroupName = "Graphics_Hands",
                CanDuplicate = false,
                IsEnabled = true,
                LeftModel = LeftGraphicalHand,
                RightModel = RightGraphicalHand,
                modelList = new List<IHandModel>(),
                modelsCheckedOut = new List<IHandModel>()
            });

            handPool.ModelPool.Add(new HandPool.ModelGroup()
            {
                GroupName = "Attachments",
                CanDuplicate = false,
                IsEnabled = true,
                LeftModel = LeftHand,
                RightModel = RightHand,
                modelList = new List<IHandModel>(),
                modelsCheckedOut = new List<IHandModel>()
            });

            LeftHand.transform.SetParent(handPool.transform, false);
            RightHand.transform.SetParent(handPool.transform, false);
            LeftGraphicalHand.transform.SetParent(handPool.transform, false);
            RightGraphicalHand.transform.SetParent(handPool.transform, false);


            return serviceProvider;
        }

        protected virtual HandModel BuildGraphicalHand(Chirality handedness)
        {
            //var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);


            var handObj = UnityHelper.LoadFromAssetBundle<GameObject>(
#if UNITY_4_5
                VRGIN.U46.U46.Resource.hands,
#else
                VRGIN.Resource.hands,
#endif
                "LoPoly_Rigged_Hand_" + handedness
            );

            var hand = SetUpRiggedHand(handObj, handedness);
            //hand._material = new Material(VR.Context.Materials.StandardShader);
            //hand._sphereMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            hand.gameObject.AddComponent<HandEnableDisable>();

            //Destroy(primitive);

            return hand;
        }

        /// <summary>
        /// Needed because of Unity's horrendous AssetBundle structure. Scripts are only expected in the main assembly, and since we're not there...
        /// </summary>
        /// <param name="hand"></param>
        private RiggedHand SetUpRiggedHand(GameObject handObj, Chirality handedness)
        {
            var hand = handObj.GetComponent<RiggedHand>();

            handObj.transform.localScale *= 0.01f;
            //handObj.transform.localScale *= 0.085f;
            if (hand)
            {
                hand.gameObject.AddComponent<LeapMenuHandler>();

                return hand;
            }

            hand = handObj.AddComponent<RiggedHand>();
            hand.gameObject.AddComponent<LeapMenuHandler>();

            handObj.AddComponent<HandEnableDisable>();
            hand.handedness = handedness;

            // Get references
            var thumb_meta = hand.gameObject.Descendants().First(d => d.name.EndsWith("thumb_meta", StringComparison.InvariantCultureIgnoreCase)).AddComponent<RiggedFinger>();
            var index_meta = hand.gameObject.Descendants().First(d => d.name.EndsWith("index_meta", StringComparison.InvariantCultureIgnoreCase)).AddComponent<RiggedFinger>();
            var ring_meta = hand.gameObject.Descendants().First(d => d.name.EndsWith("ring_meta")).AddComponent<RiggedFinger>();
            var middle_meta = hand.gameObject.Descendants().First(d => d.name.EndsWith("middle_meta")).AddComponent<RiggedFinger>();
            var pinky_meta = hand.gameObject.Descendants().First(d => d.name.EndsWith("pinky_meta")).AddComponent<RiggedFinger>();
            var wrist = hand.gameObject.Descendants().First(d => d.name.EndsWith("Wrist")).transform;
            var palm = hand.gameObject.Descendants().First(d => d.name.EndsWith("Palm")).transform;

            // Set up hand
            hand.fingers = new RiggedFinger[]
            {
                thumb_meta, index_meta, middle_meta, ring_meta, pinky_meta
            };

            hand.wristJoint = wrist;
            hand.palm = palm;
            hand.ModelPalmAtLeapWrist = true;
            hand.handModelPalmWidth = 0.085f;
            hand.UseMetaCarpals = true;
            var pointDir = Vector3.left * (hand.handedness == Chirality.Left ? 1 : -1);
            var palmDir = Vector3.up * (hand.handedness == Chirality.Left ? 1 : -1);
            hand.modelFingerPointing = pointDir;
            hand.modelPalmFacing = palmDir;

            // Set up fingers
            thumb_meta.fingerType = Leap.Finger.FingerType.TYPE_THUMB;
            index_meta.fingerType = Leap.Finger.FingerType.TYPE_INDEX;
            ring_meta.fingerType = Leap.Finger.FingerType.TYPE_RING;
            middle_meta.fingerType = Leap.Finger.FingerType.TYPE_MIDDLE;
            pinky_meta.fingerType = Leap.Finger.FingerType.TYPE_PINKY;

            thumb_meta.bones = new Transform[] { null, thumb_meta.transform }.Concat(thumb_meta.gameObject.Descendants().Select(d => d.transform).Take(2)).ToArray();
            index_meta.bones = new Transform[] { index_meta.transform }.Concat(index_meta.gameObject.Descendants().Select(d => d.transform).Take(3)).ToArray();
            ring_meta.bones = new Transform[] { ring_meta.transform }.Concat(ring_meta.gameObject.Descendants().Select(d => d.transform).Take(3)).ToArray();
            middle_meta.bones = new Transform[] { middle_meta.transform }.Concat(middle_meta.gameObject.Descendants().Select(d => d.transform).Take(3)).ToArray();
            pinky_meta.bones = new Transform[] { pinky_meta.transform }.Concat(pinky_meta.gameObject.Descendants().Select(d => d.transform).Take(3)).ToArray();
            
            foreach(var finger in new RiggedFinger[] { thumb_meta, index_meta, ring_meta, middle_meta, pinky_meta }) {
                finger.modelFingerPointing = pointDir;
                finger.modelPalmFacing = palmDir;
                finger.joints = new Transform[] { null, null, null };
            }
            
            foreach(var obj in handObj.Descendants())
            {
                VRLog.Info("{0}: {1}", obj.transform.name, obj.transform.localScale);
            }
            return hand;
        }


        protected virtual HandAttachments BuildAttachmentHand(Chirality handedness)
        {
            var hand = new GameObject("AHand_" + handedness).AddComponent<HandAttachments>();
            hand._handedness = handedness;
            hand.gameObject.AddComponent<HandEnableDisable>();
            hand.gameObject.AddComponent<WarpHandler>();

            // Create transforms
            hand.GrabPoint = UnityHelper.CreateGameObjectAsChild("GrabPoint", hand.transform, true);
            hand.Arm = UnityHelper.CreateGameObjectAsChild("Arm", hand.transform, true);
            hand.Thumb = UnityHelper.CreateGameObjectAsChild("Thumb", hand.transform, true);
            hand.Index = UnityHelper.CreateGameObjectAsChild("Index", hand.transform, true);
            hand.Middle = UnityHelper.CreateGameObjectAsChild("Middle", hand.transform, true);
            hand.Ring = UnityHelper.CreateGameObjectAsChild("Ring", hand.transform, true);
            hand.Pinky = UnityHelper.CreateGameObjectAsChild("Pinky", hand.transform, true);
            hand.PinchPoint = UnityHelper.CreateGameObjectAsChild("PinchPoint", hand.transform, true);
            hand.Palm = UnityHelper.CreateGameObjectAsChild("Palm", hand.transform, true);
            hand.OnBegin += delegate
            {
                if (!_ControllerFound)
                {
                    _ControllerFound = true;
                    ChangeModeOnControllersDetected();
                }
            };
            return hand;
        }
        
        public virtual void OnDestroy()
        {
            Destroy(ControllerManager);
            Destroy(Left);
            Destroy(Right);
            DestroyImmediate(LeapMotion.gameObject);


            if (Shortcuts != null)
            {
                foreach (var shortcut in Shortcuts)
                {
                    shortcut.Dispose();
                }
            }
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
                new VoiceShortcut(VoiceCommand.DecreaseScale, delegate { VR.Settings.IPDScale *= 1.2f; }), // Decrease / Increase scale of the *world* (inverse of camera scale!)
                new VoiceShortcut(VoiceCommand.IncreaseScale, delegate { VR.Settings.IPDScale *= 0.8f; }),
                new MultiKeyboardShortcut(new KeyStroke("Ctrl + C"), new KeyStroke("Ctrl + D"), delegate { UnityHelper.DumpScene("dump.json"); } ),
                new MultiKeyboardShortcut(new KeyStroke("Ctrl + C"), new KeyStroke("Ctrl + V"), ToggleUserCamera),
                new KeyboardShortcut(new KeyStroke("Alt + S"), delegate { VR.Settings.Save(); }),
                new VoiceShortcut(VoiceCommand.SaveSettings, delegate { VR.Settings.Save(); }),
                new KeyboardShortcut(new KeyStroke("Alt + L"), delegate { VR.Settings.Reload(); }),
                new VoiceShortcut(VoiceCommand.LoadSettings, delegate { VR.Settings.Reload(); }),
                new KeyboardShortcut(new KeyStroke("Ctrl + Alt + L"), delegate { VR.Settings.Reset(); }),
                new VoiceShortcut(VoiceCommand.ResetSettings, delegate { VR.Settings.Reset(); }),
                new VoiceShortcut(VoiceCommand.Impersonate, delegate { Impersonate(VR.Interpreter.Actors.FirstOrDefault()); }),
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
