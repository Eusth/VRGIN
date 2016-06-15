using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using VRGIN.Controls.Handlers;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Native;
using static VRGIN.Native.WindowsInterop;

namespace VRGIN.Controls
{

    public abstract class Controller : ProtectedBehaviour
    {
        public class Lock
        {
            public bool IsValid { get; private set; }
            private Controller _Controller;

            public static Lock Invalid
            {
                get
                {
                    return new Lock();
                }
            }
            private Lock()
            {
                IsValid = false;
            }

            internal Lock(Controller controller)
            {
                IsValid = true;
                _Controller = controller;
                _Controller._Lock = this;
                _Controller.OnLock();
            }

            public void Release()
            {
                if (IsValid)
                {
                    _Controller._Lock = null;
                    _Controller.OnUnlock();
                    IsValid = false;
                }
                else
                {
                    VRLog.Warn("Tried to release an invalid lock!");
                }
            }
        }

        private bool _Started = false;

        public SteamVR_TrackedObject Tracking;
        protected SteamVR_RenderModel Model;
        protected BoxCollider Collider;

        private Vector2? mouseDownPosition;
        private float? appButtonPressTime;

        private LineRenderer Laser;

        public List<Tool> Tools = new List<Tool>();

        public Controller Other;
        private const int MOUSE_STABILIZER_THRESHOLD = 30; // pixels
        private const float APP_BUTTON_TIME_THRESHOLD = 0.5f; // seconds
        private bool helpShown;
        private List<HelpText> helpTexts;

        private Canvas _Canvas;
        private Lock _Lock;
        private Lock _LaserLock;


        public RumbleManager Rumble { get; private set; }

        /// <summary>
        /// Tries to acquire the focus of the controller, meaning that tools will be temporarily halted.
        /// </summary>
        /// <param name="lockObj">Lock object to fill. Will be assigned NULL when it failed.</param>
        /// <returns>Whether or not the process was successful.</returns>
        [Obsolete("Use TryAcquireFocus() or AcquireFocus()")]
        public bool AcquireFocus(out Lock lockObj)
        {
            return TryAcquireFocus(out lockObj);
        }

        /// <summary>
        /// Tries to acquire the focus of the controller, meaning that tools will be temporarily halted.
        /// </summary>
        /// <param name="lockObj">Lock object to fill. Will be assigned NULL when it failed.</param>
        /// <returns>Whether or not the process was successful.</returns>
        public bool TryAcquireFocus(out Lock lockObj)
        {
            lockObj = null;

            if (_Lock == null || !_Lock.IsValid)
            {
                lockObj = new Lock(this);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to acquire the focus of the controller, meaning that tools will be temporarily halted.
        /// </summary>
        /// <returns>The lock object. Might be valid or invalid.</returns>
        public Lock AcquireFocus()
        {
            Lock lockObj;
            if(TryAcquireFocus(out lockObj))
            {
                return lockObj;
            } else
            {
                return Lock.Invalid;
            }
        }

        protected virtual void OnLock()
        {
            ToolEnabled = false;
        }

        protected virtual void OnUnlock()
        {
            ToolEnabled = true;
        }

        protected virtual void OnDestroy()
        {
            GameObject.Destroy(gameObject);
        }

        protected void SetUp()
        {
            Tracking = gameObject.AddComponent<SteamVR_TrackedObject>();
            Rumble = gameObject.AddComponent<RumbleManager>();
            gameObject.AddComponent<BodyRumbleHandler>();

            Laser = new GameObject().AddComponent<LineRenderer>();
            Laser.transform.SetParent(transform, false);
            Laser.material = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
            Laser.material.renderQueue += 1000;
            Laser.SetColors(Color.cyan, Color.cyan);
            Laser.transform.localRotation = Quaternion.Euler(60, 0, 0);
            Laser.transform.position += Laser.transform.forward * 0.07f;
            Laser.SetVertexCount(2);
            Laser.useWorldSpace = true;
            Laser.SetWidth(0.002f, 0.002f);

            // Add model
            Model = new GameObject("Model").AddComponent<SteamVR_RenderModel>();
            Model.shader = VRManager.Instance.Context.Materials.StandardShader;
            if (!Model.shader)
            {
                VRLog.Warn("Shader not found");
            }
            Model.transform.SetParent(transform, false);
            //Model.verbose = true;

            BuildCanvas();

            // Add Physics
            Collider = new GameObject("Collider").AddComponent<BoxCollider>();
            Collider.transform.SetParent(transform, false);
            Collider.center = new Vector3(0, -0.02f, -0.06f);
            Collider.size = new Vector3(-0.05f, 0.05f, 0.2f);
            Collider.isTrigger = true;

            gameObject.AddComponent<Rigidbody>().isKinematic = true;
        }



        protected override void OnAwake()
        {
            base.OnAwake();
            SetUp();
        }

        public void AddTool(Type toolType)
        {
            if (toolType.IsSubclassOf(typeof(Tool)) && !Tools.Any(tool => toolType.IsAssignableFrom(tool.GetType())))
            {
                var newTool = gameObject.AddComponent(toolType) as Tool;
                Tools.Add(newTool);
                CreateToolCanvas(newTool);

                if (_Started)
                {
                    newTool.enabled = false;
                }
            }
        }

        public void AddTool<T>() where T : Tool
        {
            AddTool(typeof(T));
        }

        public virtual int ToolIndex { get; set; }

        public Tool ActiveTool
        {
            get
            {
                if (ToolIndex >= Tools.Count) return null;
                return Tools[ToolIndex];
            }
        }

        public virtual IList<Type> ToolTypes
        {
            get { return new List<Type>(); }
        }

        protected override void OnStart()
        {
            int i = 0;
            foreach (var tool in Tools)
            {
                if (i++ != ToolIndex && tool)
                {
                    tool.enabled = false;
                    VRLog.Info("Kill tool #{0} ({1})", i - 1, ToolIndex);
                }
                else
                {
                    VRLog.Info("Enable Tool #{0}", i - 1);
                    if (tool.enabled) tool.enabled = false;
                    tool.enabled = true;
                }
            }

            _Started = true;
        }

        protected override void OnUpdate()
        {
            //if(ActiveTool.enabled != Tracking.isValid)
            //{
            //    ActiveTool.enabled = Tracking.isValid && !LaserVisible;
            //}

            if (Laser.gameObject.activeSelf)
            {
                Laser.SetPosition(0, Laser.transform.position);
                Laser.SetPosition(1, Laser.transform.position + Laser.transform.forward);
            }
            //Logger.Info(transform.position);
            if (Other)
            {
                if (Other.ActiveTool != null && Other.ActiveTool is MenuTool)
                {

                    var menuTool = Other.ActiveTool as MenuTool;

                    if (menuTool.Gui)
                    {
                        float range = 0.25f;

                        var normal = -menuTool.Gui.transform.forward;
                        var otherPos = menuTool.Gui.transform.position;

                        var myPos = Laser.transform.position;
                        var laser = Laser.transform.forward;
                        var heightOverMenu = -menuTool.Gui.transform.InverseTransformPoint(myPos).z;

                        bool laserVisible = heightOverMenu > 0 && heightOverMenu < range
                            && Vector3.Dot(normal, laser) < 0; // They have to point the other way

                        if (laserVisible)
                        {
                            // So far so good. Now raycast!
                            RaycastHit hit;
                            if (Physics.Raycast(myPos, laser, out hit, range, LayerMask.GetMask(VRManager.Instance.Context.GuiLayer)))
                            {
                                Laser.SetPosition(1, hit.point);

                                var newPos = new Vector2(hit.textureCoord.x * Screen.width, (1 - hit.textureCoord.y) * Screen.height);

                                if (!mouseDownPosition.HasValue || Vector2.Distance(mouseDownPosition.Value, newPos) > MOUSE_STABILIZER_THRESHOLD)
                                {
                                    MouseOperations.SetClientCursorPosition((int)newPos.x, (int)newPos.y);
                                    mouseDownPosition = null;
                                }
                                laserVisible = true;
                            }
                            else
                            {
                                laserVisible = false;
                            }
                        }

                        LaserVisible = laserVisible;
                    }
                    else
                    {
                        LaserVisible = false;
                    }

                }
                else if (LaserVisible)
                {
                    LaserVisible = false;
                }

            }
        }



        public bool LaserVisible
        {
            get
            {
                return Laser.gameObject.activeSelf;
            }
            set
            {
                if (value && _LaserLock == null)
                {
                    if (!AcquireFocus(out _LaserLock))
                    {
                        // Could not get focus, do nothing.
                        return;
                    }
                }
                else if (!value && _LaserLock != null)
                {
                    _LaserLock.Release();
                    _LaserLock = null;
                }

                Laser.gameObject.SetActive(value);

                if (value)
                {
                    Laser.SetPosition(0, Laser.transform.position);
                }
            }
        }

        public bool ToolEnabled
        {
            get
            {
                return ActiveTool != null && ActiveTool.enabled;
            }
            set
            {
                if (ActiveTool != null)
                {
                    ActiveTool.enabled = value;
                    if (!value)
                    {
                        HideHelp();
                    }
                }
            }

        }

        protected override void OnFixedUpdate()
        {
            var device = SteamVR_Controller.Input((int)Tracking.index);

            if (LaserVisible)
            {
                if (device.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    MouseOperations.MouseEvent(MouseEventFlags.LeftDown);
                    mouseDownPosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                }
                if (device.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    MouseOperations.MouseEvent(MouseEventFlags.LeftUp);
                    mouseDownPosition = null;
                }
            }
            else if (_Lock == null)
            {
                if (device.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
                {
                    appButtonPressTime = Time.time;
                }
                if (device.GetPress(EVRButtonId.k_EButton_ApplicationMenu) && (Time.time - appButtonPressTime) > APP_BUTTON_TIME_THRESHOLD)
                {
                    ShowHelp();
                    appButtonPressTime = null;
                }
                if (device.GetPressUp(EVRButtonId.k_EButton_ApplicationMenu))
                {
                    if (helpShown)
                    {
                        HideHelp();
                    }
                    else
                    {
                        if (ActiveTool)
                        {
                            ActiveTool.enabled = false;
                        }

                        ToolIndex = (ToolIndex + 1) % Tools.Count;

                        if (ActiveTool)
                        {
                            ActiveTool.enabled = true;
                        }
                    }
                    appButtonPressTime = null;

                }
            }

        }

        public void StartRumble(IRumbleSession session)
        {
            Rumble.StartRumble(session);
        }

        private void HideHelp()
        {
            if (helpShown)
            {
                helpTexts.ForEach(h => Destroy(h.gameObject));
                helpShown = false;
            }
        }

        private void ShowHelp()
        {
            if (ActiveTool != null)
            {
                helpTexts = ActiveTool.GetHelpTexts();
                helpShown = true;
            }
        }

        private void BuildCanvas()
        {

            var canvas = _Canvas = new GameObject().AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.SetParent(transform, false);

            // Copied straight out of Unity
            canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 950);
            canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 950);

            canvas.transform.localPosition = new Vector3(0, -0.02725995f, 0.0279f);
            canvas.transform.localRotation = Quaternion.Euler(30, 180, 180);
            canvas.transform.localScale = new Vector3(4.930151e-05f, 4.930148e-05f, 0);

            canvas.gameObject.layer = 0;

            // Hack for alpha order
            var circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            circle.transform.SetParent(transform, false);
            circle.transform.localScale = new Vector3(0.05f, 0, 0.05f);
            circle.transform.localPosition = new Vector3(0, -0.0303f, 0.0142f);
            circle.transform.localRotation = Quaternion.Euler(60, 0, 0);
            circle.GetComponent<Collider>().enabled = false;
        }

        private void CreateToolCanvas(Tool tool)
        {
            var img = new GameObject().AddComponent<Image>();
            img.transform.SetParent(_Canvas.transform, false);

            var texture = tool.Image;
            img.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            // Maximize
            img.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            img.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);

            img.color = Color.cyan;


            tool.Icon = img.gameObject;
            tool.Icon.SetActive(false);
            tool.Icon.layer = 0;
        }


    }
}
