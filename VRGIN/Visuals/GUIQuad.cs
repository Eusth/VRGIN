using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Modes;

namespace VRGIN.Visuals
{
    public static class GUIQuadRegistry
    {
        static HashSet<GUIQuad> _Quads = new HashSet<GUIQuad>();

        public static IEnumerable<GUIQuad> Quads
        {
            get
            {
                return _Quads;
            }
        }

        internal static void Register(GUIQuad quad)
        {
            _Quads.Add(quad);
        }

        internal static void Unregister(GUIQuad quad)
        {
            _Quads.Remove(quad);
        }

    }
    public class GUIQuad : ProtectedBehaviour
    {
#if !UNITY_4_5
        private Renderer renderer;
#endif
        public bool IsOwned = false;
        private IScreenGrabber _Source;
        public static GUIQuad Create(IScreenGrabber source = null)
        {
            source = source ?? VR.GUI;

            VRLog.Info("Create GUI");
            var gui = GameObject.CreatePrimitive(PrimitiveType.Quad).AddComponent<GUIQuad>();
            gui.name = "GUIQuad";
            
            if(source != VR.GUI)
            {
                gui.gameObject.SetActive(false);
                gui._Source = source;
                gui.gameObject.SetActive(true);
            }

            gui.UpdateGUI();
            return gui;
        }


        protected override void OnAwake()
        {
#if !UNITY_4_5
            renderer = GetComponent<Renderer>();
#endif
            _Source = VR.GUI;
            transform.localPosition = Vector3.zero;// new Vector3(0, 0, distance);
            transform.localRotation = Quaternion.identity;
            gameObject.layer = LayerMask.NameToLayer(VRManager.Instance.Context.GuiLayer);
        }

        protected override void OnStart()
        {
            base.OnStart();
            UpdateAspect();
        }

        protected virtual void OnEnable()
        {
            if (IsGUISource())
            {
                VRLog.Info("Start listening to GUI ({0})", name);
                GUIQuadRegistry.Register(this);
                VR.GUI.Listen();
            }
        }

        protected virtual void OnDisable()
        {
            if (IsGUISource())
            {
                VRLog.Info("Stop listening to GUI ({0})", name);
                GUIQuadRegistry.Unregister(this);
                VR.GUI.Unlisten();
            }
        }

        private bool IsGUISource()
        {
            return _Source == VR.GUI;
        }

        public virtual void UpdateAspect()
        {
            var height = transform.localScale.y;
            var width = height / Screen.height * Screen.width;

            transform.localScale = new Vector3(width, height, 1);
        }

        public virtual void UpdateGUI()
        {
            //VRLog.Info();
            //renderGUI = false;
            UpdateAspect();
            if (!renderer) VRLog.Warn("No renderer!");
            try
            {
                renderer.receiveShadows = false;
#if UNITY_4_5
                renderer.castShadows = false;
#else
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
#endif

                var textures = _Source.GetTextures();
                VRLog.Info("Updating GUI {0} with {1} textures", name, textures.Count());

                if (textures.Count() >= 2)
                {
                    renderer.material = VR.Context.Materials.UnlitTransparentCombined;
                    renderer.material.SetTexture("_MainTex", textures.FirstOrDefault());
                    renderer.material.SetTexture("_SubTex", textures.Last());
                }
                else
                {
                    renderer.material = VR.Context.Materials.UnlitTransparent;
                    renderer.material.SetTexture("_MainTex", textures.FirstOrDefault());
                }
            }
            catch (Exception e)
            {
                VRLog.Info(e);
            }
        }
    }
}
