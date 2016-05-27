using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core.Helpers;
using VRGIN.Core.Visuals;

#if UNITY_4_5
using VRGIN.Core.Native;
#endif

namespace VRGIN.Core
{
    /// <summary>
    /// <para>Singleton class that records the 2D GUI and renders it into a RenderTexture. Will only render when at least one listener is present.</para>
    /// 
    /// This works in two layers:
    ///   - the new UI is caught by redirecting all canvas to be renderer by the VRGUI camera into <see cref="uGuiTexture"/>
    ///   - the old UI is caught by setting the global render texture to <see cref="nGuiTexture"/> while the system is rendering
    /// </summary>
    public class VRGUI : ProtectedBehaviour
    {

#if UNITY_4_5
        class CursorBlocker : ProtectedBehaviour
        {
    
            private bool _focused = false;

            protected override void OnAwake()
            {
                WindowManager.Activate();
                WindowManager.ConfineCursor();
            }

            void OnApplicationFocus(bool hasFocus)
            {
                _focused = hasFocus;
                WindowManager.ConfineCursor();
            }
        }    
#endif

        private static VRGUI _Instance;
        private IDictionary _Registry;

        /// <summary>
        /// Gets an instance of VRGUI.
        /// </summary>
        public static VRGUI Instance
        {
            get
            {
                if (!_Instance)
                {
                    _Instance = new GameObject("GUI").AddComponent<VRGUI>();

#if UNITY_4_5
                    _Instance.gameObject.AddComponent<CursorBlocker>();
#endif
                    if (VR.Context.SimulateCursor)
                    {
                        var cursor = SimulatedCursor.Create();
                        cursor.transform.SetParent(_Instance.transform, false);

                        Logger.Info("Cursor is simulated");
                    }
                }
                return _Instance;
            }
        }
        
        /// <summary>
        /// Gets the texture used for uGUI rendering. (Canvas)
        /// </summary>
        public RenderTexture uGuiTexture { get; private set; }

        /// <summary>
        /// Gets the texture used for nGUI rendering. (Legacy)
        /// </summary>
        public RenderTexture nGuiTexture { get; private set; }

        private FieldInfo _Graphics;

        private RenderTexture _PrevRT = null;

        private Camera _VRGUICamera;
        
        private int _Listeners;

        public void Listen()
        {
            _Listeners++;
        }

        public void Unlisten()
        {
            _Listeners--;
        }
        protected override void OnAwake()
        {
            uGuiTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Default);
            uGuiTexture.antiAliasing = 4;
            uGuiTexture.Create();

            nGuiTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.Default);
            nGuiTexture.Create();

            transform.localPosition = Vector3.zero;// new Vector3(0, 0, distance);
            transform.localRotation = Quaternion.identity;

            gameObject.AddComponent<FastGUI>();
            gameObject.AddComponent<SlowGUI>();

            // Add GUI camera
            _VRGUICamera = new GameObject("VRGIN_GUICamera").AddComponent<Camera>();
            _VRGUICamera.transform.position = Vector3.zero;
            _VRGUICamera.transform.rotation = Quaternion.identity;


            _VRGUICamera.cullingMask = VR.Context.UILayerMask;
            _VRGUICamera.depth = 1;
            _VRGUICamera.nearClipPlane = 99f;
            _VRGUICamera.farClipPlane = 10000;
            _VRGUICamera.targetTexture = uGuiTexture;
            _Graphics = typeof(GraphicRegistry).GetField("m_Graphics", BindingFlags.NonPublic | BindingFlags.Instance);
            _Registry = _Graphics.GetValue(GraphicRegistry.instance) as IDictionary;

            GameObject.DontDestroyOnLoad(_VRGUICamera);
            DontDestroyOnLoad(gameObject);
        }

        protected void CatchCanvas()
        {
            var canvasList = (_Registry.Keys as ICollection<Canvas>).Where(c => c).SelectMany(canvas => canvas.gameObject.GetComponentsInChildren<Canvas>());
            foreach (var canvas in canvasList.Where(c => (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera) && c.worldCamera != _VRGUICamera))
            {
                if(VR.Context.IgnoredCanvas.Contains(canvas.name)) continue;
                //if (canvas.name.Contains("TexFade")) continue;
                Logger.Info("Add {0} ({1}: {2})", canvas.name, canvas.sortingLayerName, LayerMask.LayerToName(canvas.gameObject.layer));

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _VRGUICamera;

#if UNITY_4_5
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster)
                {
                    GameObject.DestroyImmediate(raycaster);
                    var newRaycaster = canvas.gameObject.AddComponent<SortingAwareGraphicRaycaster>();

                    // These fields turned into properties in Unity 4.7+
                    UnityHelper.SetPropertyOrField(newRaycaster, "ignoreReversedGraphics", UnityHelper.GetPropertyOrField(raycaster, "ignoreReversedGraphics"));
                    UnityHelper.SetPropertyOrField(newRaycaster, "blockingObjects", UnityHelper.GetPropertyOrField(raycaster, "blockingObjects"));
                    UnityHelper.SetPropertyOrField(newRaycaster, "m_BlockingMask", UnityHelper.GetPropertyOrField(raycaster, "m_BlockingMask"));
                }
#endif
            }
        }

        protected override void OnUpdate()
        {
#if !UNITY_4_5
            Cursor.lockState = CursorLockMode.Confined;
#endif
            if (_Listeners > 0)
            {
                //Logger.Info(Time.time);
                //var watch = System.Diagnostics.Stopwatch.StartNew();
                CatchCanvas();
                //Logger.Info(watch.ElapsedTicks);
            }
            if (_Listeners < 0)
            {
                Logger.Warn("Numbers don't add up!");
            }
        }


        internal void OnAfterGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                RenderTexture.active = _PrevRT;
            }
        }

        internal void OnBeforeGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {

                _PrevRT = RenderTexture.active;
                RenderTexture.active = nGuiTexture;

                GL.Clear(true, true, Color.clear);
            }
        }

        /// <summary>
        /// Notifies VRGUI when the legacy GUI starts rendering.
        /// </summary>
        private class FastGUI : MonoBehaviour {
            private void OnGUI()
            {
                GUI.depth = int.MaxValue;

                if (Event.current.type == EventType.Repaint)
                {
                    SendMessage("OnBeforeGUI");
                }
            }
        }

        /// <summary>
        /// Notifies VRGUI when the legacy GUI stops rendering.
        /// </summary>
        private class SlowGUI : MonoBehaviour
        {
            private void OnGUI()
            {
                GUI.depth = int.MinValue;

                if (Event.current.type == EventType.Repaint)
                {
                    SendMessage("OnAfterGUI");
                }
            }
        }

        /**
         * Makes sure that canvas are sorted the right order.
         */
        private class SortingAwareGraphicRaycaster : GraphicRaycaster
        {
            private Canvas _Canvas;
            private Canvas Canvas
            {
                get
                {
                    if (_Canvas != null)
                        return _Canvas;

                    _Canvas = GetComponent<Canvas>();
                    return _Canvas;
                }
            }

            public override int priority
            {
                get
                {
                    return GetOrder();
                }
            }
            public override int sortOrderPriority
            {
                get
                {
                    return GetOrder();
                }
            }

            public override int renderOrderPriority
            {
                get
                {
                    return GetOrder();
                }
            }

            private int GetOrder()
            {
                if (Canvas.gameObject.layer != LayerMask.NameToLayer(VR.Context.UILayer)) return int.MinValue;
                return -Canvas.sortingOrder;
            }
        }
    }
}
