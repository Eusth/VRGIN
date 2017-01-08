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
            public bool IsInvalidating { get; private set; }
            public bool IsValid { get; private set; }
            private Controller _Controller;

            public static readonly Lock Invalid = new Lock();

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

            public void SafeRelease()
            {
                if (IsValid)
                {
                    IsInvalidating = true;
                }
                else
                {
                    VRLog.Warn("Tried to release an invalid lock!");
                }
            }
        }

        private bool _Started = false;

        public SteamVR_TrackedObject Tracking;
        public SteamVR_RenderModel Model { get; private set; }
        protected BoxCollider Collider;

        private float? appButtonPressTime;


        public List<Tool> Tools = new List<Tool>();

        public Controller Other;
        private const float APP_BUTTON_TIME_THRESHOLD = 0.5f; // seconds
        private bool helpShown;
        private List<HelpText> helpTexts;

        private Canvas _Canvas;
        private Lock _Lock = Lock.Invalid;
        private GameObject _AlphaConcealer;


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

            if (CanAcquireFocus())
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
            if (TryAcquireFocus(out lockObj))
            {
                return lockObj;
            }
            else
            {
                return Lock.Invalid;
            }
        }

        public bool CanAcquireFocus()
        {
            return _Lock == null || !_Lock.IsValid;
        }

        protected virtual void OnLock()
        {
            ToolEnabled = false;
            _AlphaConcealer.SetActive(false);
        }

        protected virtual void OnUnlock()
        {
            ToolEnabled = true;
            _AlphaConcealer.SetActive(true);
        }

        protected virtual void OnDestroy()
        {
            GameObject.Destroy(gameObject);

            SteamVR_Utils.Event.Remove("render_model_loaded", _OnRenderModelLoaded);
        }

        protected void SetUp()
        {
            SteamVR_Utils.Event.Listen("render_model_loaded", _OnRenderModelLoaded);

            Tracking = gameObject.AddComponent<SteamVR_TrackedObject>();
            Rumble = gameObject.AddComponent<RumbleManager>();
            gameObject.AddComponent<BodyRumbleHandler>();
            gameObject.AddComponent<MenuHandler>();

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

        private void _OnRenderModelLoaded(object[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    var renderModel = args[0] as SteamVR_RenderModel;
                    if (renderModel && renderModel.transform.IsChildOf(transform))
                    {
                        VRLog.Info("Render model loaded!");
                        gameObject.SendMessageToAll("OnRenderModelLoaded");
                        OnRenderModelLoaded();
                    }
                }
            } catch(Exception e)
            {
                VRLog.Error(e);
            }
        }

        private void OnRenderModelLoaded()
        {
            //PlaceCanvas();
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

                newTool.enabled = false;
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
                    VRLog.Info("Disable tool #{0} ({1})", i - 1, ToolIndex);
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

        //protected override void OnUpdate()
        //{
        //    //if(ActiveTool.enabled != Tracking.isValid)
        //    //{
        //    //    ActiveTool.enabled = Tracking.isValid && !LaserVisible;
        //    //}

        //    //Logger.Info(transform.position);

        //}

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

        /// <summary>
        /// Gets whether or not the attached controlller is tracking.
        /// </summary>
        public bool IsTracking
        {
            get
            {
                return Tracking && Tracking.isValid;
            }
        }

        /// <summary>
        /// Gets the attached controller input object.
        /// </summary>
        public SteamVR_Controller.Device Input
        {
            get
            {
                return SteamVR_Controller.Input((int)Tracking.index);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            var device = SteamVR_Controller.Input((int)Tracking.index);

            if (_Lock != null && _Lock.IsInvalidating)
            {
                TryReleaseLock();
            }

            if (_Lock == null || !_Lock.IsValid)
            {
                if (device.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
                {
                    appButtonPressTime = Time.unscaledTime;
                }
                if (device.GetPress(EVRButtonId.k_EButton_ApplicationMenu) && (Time.unscaledTime - appButtonPressTime) > APP_BUTTON_TIME_THRESHOLD)
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

        private void TryReleaseLock()
        {
            var input = Input;
            foreach(var value in Enum.GetValues(typeof(EVRButtonId)).OfType<EVRButtonId>())
            {
                if (input.GetPress(value))
                    return;
            }

            // Release
            _Lock.Release();
        }

        public void StartRumble(IRumbleSession session)
        {
            Rumble.StartRumble(session);
        }

        public void StopRumble(IRumbleSession session)
        {
            Rumble.StopRumble(session);
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
            _AlphaConcealer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _AlphaConcealer.transform.SetParent(transform, false);
            _AlphaConcealer.transform.localScale = new Vector3(0.05f, 0, 0.05f);
            _AlphaConcealer.transform.localPosition = new Vector3(0, -0.0303f, 0.0142f);
            _AlphaConcealer.transform.localRotation = Quaternion.Euler(60, 0, 0);
            _AlphaConcealer.GetComponent<Collider>().enabled = false;
        }

        //private void PlaceCanvas()
        //{
        //    if (Model.renderModelName.Contains("cv1"))
        //    {
        //        var attachPosition = FindAttachPosition("y_button");
        //        _Canvas.transform.SetParent(attachPosition, false);
        //        _Canvas.transform.localPosition = new Vector3(0, 0, Model.renderModelName.Contains("left") ? 0.0007f : -0.0007f);
        //        _Canvas.transform.localRotation = Quaternion.identity;
        //        _Canvas.transform.localScale = new Vector3(4.930151e-05f, 4.930148e-05f, 0);
        //    }
        //    else
        //    {
        //        _Canvas.transform.localPosition = new Vector3(0, -0.02725995f, 0.0279f);
        //        _Canvas.transform.localRotation = Quaternion.Euler(30, 180, 180);
        //        _Canvas.transform.localScale = new Vector3(4.930151e-05f, 4.930148e-05f, 0);
        //    }
        //}

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

        public Transform FindAttachPosition(params string[] names)
        {
            var node = transform.GetComponentsInChildren<Transform>().Where(t => names.Contains(t.name)).FirstOrDefault();
            if (node == null) return null;
            return node.Find("attach");
        }
    }
}