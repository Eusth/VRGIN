using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Core;

namespace VRGIN.Helpers
{
    public class VRCapturePanorama : CapturePanorama.CapturePanorama
    {
        private Camera _Camera;
        private IShortcut _Shortcut;

        protected override void OnStart()
        {
            // Get shaders
            fadeMaterial = UnityHelper.LoadFromAssetBundle<Material>(ResourceManager.Capture, "Fade material");
            convertPanoramaShader = UnityHelper.LoadFromAssetBundle<ComputeShader>(ResourceManager.Capture, "ConvertPanoramaShader");
            convertPanoramaStereoShader = UnityHelper.LoadFromAssetBundle<ComputeShader>(ResourceManager.Capture, "ConvertPanoramaStereoShader");
            textureToBufferShader = UnityHelper.LoadFromAssetBundle<ComputeShader>(ResourceManager.Capture, "TextureToBufferShader");

            captureStereoscopic = VR.Settings.Capture.Stereoscopic;
            interpupillaryDistance = SteamVR.instance.GetFloatProperty(ETrackedDeviceProperty.Prop_UserIpdMeters_Float) * VR.Settings.IPDScale;
            captureKey = KeyCode.None;

            _Shortcut = new MultiKeyboardShortcut(VR.Settings.Capture.Shortcut, delegate
            {

                if (!Capturing)
                {
                    string filenameBase = String.Format("{0}_{1:yyyy-MM-dd_HH-mm-ss-fff}", Application.productName, DateTime.Now);
                    VRLog.Info("Panorama capture key pressed, capturing " + filenameBase);
                    CaptureScreenshotAsync(filenameBase);;
                }
            });

            base.OnStart();

        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            _Shortcut.Evaluate();
        }

        public override Camera[] GetCaptureCameras()
        {
            return new Camera[] { _Camera };
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (_Camera)
            {
                Destroy(_Camera.gameObject);
            }
        }

        public override bool OnCaptureStart()
        {
            if (!_Camera)
            {
                // Clone camera if need be
                _Camera = VR.Camera.Clone(VR.Settings.Capture.WithEffects);
                _Camera.gameObject.SetActive(false);

                if(VR.Settings.Capture.HideGUI)
                {
                    _Camera.cullingMask &= ~(LayerMask.GetMask(VR.Context.GuiLayer));
                }
            }

            // Set camera position & orientation
            _Camera.transform.position = VR.Camera.Head.position;

            if (VR.Settings.Capture.SetCameraUpright)
            {
                var forward = Vector3.ProjectOnPlane(VR.Camera.Head.forward, Vector3.up).normalized;
                if (forward.magnitude < 0.1)
                {
                    forward = Vector3.forward;
                }
                _Camera.transform.rotation = Quaternion.LookRotation(forward);
            } else
            {
                _Camera.transform.rotation = VR.Camera.Head.rotation;
            }

            //if(VR.Settings.Capture.HideControllers)
            //{
            //}
            
            return true;
        }


        public override void AfterRenderPanorama()
        {
            base.AfterRenderPanorama();
        }
    }
}
