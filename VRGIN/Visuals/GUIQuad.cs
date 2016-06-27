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
        public static GUIQuad Create()
        {
            VRLog.Info("Create GUI");
            var gui = GameObject.CreatePrimitive(PrimitiveType.Quad).AddComponent<GUIQuad>();
            gui.name = "GUIQuad";

            gui.UpdateGUI();

            return gui;
        }


        protected override void OnAwake()
        {
#if !UNITY_4_5
            renderer = GetComponent<Renderer>();
#endif

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
            VRLog.Info("Listen!");
            GUIQuadRegistry.Register(this);

            VRGUI.Instance.Listen();
        }

        protected virtual void OnDisable()
        {
            VRLog.Info("Unlisten!");
            GUIQuadRegistry.Unregister(this);

            VRGUI.Instance.Unlisten();
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
                renderer.material = VR.Context.Materials.UnlitTransparentCombined;
                renderer.material.SetTexture("_MainTex", VRGUI.Instance.uGuiTexture);
                renderer.material.SetTexture("_SubTex", VRGUI.Instance.IMGuiTexture);
            }
            catch (Exception e)
            {
                VRLog.Info(e);
            }
        }
    }
}
