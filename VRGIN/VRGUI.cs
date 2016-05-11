﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

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
      
        private static VRGUI _Instance;

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
            _VRGUICamera = new GameObject("_GUICamera").AddComponent<Camera>();
            _VRGUICamera.transform.position = Vector3.zero;
            _VRGUICamera.transform.rotation = Quaternion.identity;


            _VRGUICamera.cullingMask = LayerMask.GetMask("UI");
            _VRGUICamera.depth = 1;
            _VRGUICamera.nearClipPlane = 99f;
            _VRGUICamera.farClipPlane = 10000;
            _VRGUICamera.targetTexture = uGuiTexture;
            _Graphics = typeof(GraphicRegistry).GetField("m_Graphics", BindingFlags.NonPublic | BindingFlags.Instance);

            GameObject.DontDestroyOnLoad(_VRGUICamera);
            DontDestroyOnLoad(gameObject);

        }

        protected void CatchCanvas()
        {
#if UNITY_4_5
            var canvasList = ((_Graphics.GetValue(GraphicRegistry.instance) as IDictionary).Keys as ICollection<Canvas>)
                            .Where(c => c != null).SelectMany(canvas => canvas.gameObject.GetComponentsInChildren<Canvas>());
#else
            var canvasList = GameObject.FindObjectsOfType<Canvas>();
#endif

            foreach (var canvas in canvasList.Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay && c.worldCamera != _VRGUICamera))
            {
                if (canvas.name.Contains("TexFade")) continue;
                Console.WriteLine("Add {0} ({1}: {2})", canvas.name, canvas.sortingLayerName, LayerMask.LayerToName(canvas.gameObject.layer));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _VRGUICamera;

                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster)
                {
                    GameObject.DestroyImmediate(raycaster);
                    var newRaycaster = canvas.gameObject.AddComponent<SortingAwareGraphicRaycaster>();
                    newRaycaster.ignoreReversedGraphics = raycaster.ignoreReversedGraphics;
                    newRaycaster.blockingObjects = raycaster.blockingObjects;
                }
            }
        }

        protected override void OnUpdate()
        {
            if (_Listeners > 0)
            {
                //Console.WriteLine(Time.time);
                //var watch = System.Diagnostics.Stopwatch.StartNew();
                CatchCanvas();
                //Console.WriteLine(watch.ElapsedTicks);
            }
            if (_Listeners < 0)
            {
                Console.WriteLine("NUMBER DONT ADD UP!");
            }
        }


        internal void OnAfterGUI()
        {
            if (Event.current.type == EventType.Repaint)
                RenderTexture.active = _PrevRT;
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
                GUI.depth = 1000;

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
                GUI.depth = -1000;

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
                    return -Canvas.sortingOrder;
                }
            }
            public override int sortOrderPriority
            {
                get
                {
                    return -Canvas.sortingOrder;
                }
            }
        }
    }
}
