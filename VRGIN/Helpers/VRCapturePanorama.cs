using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;

namespace VRGIN.Helpers
{
    class VRCapturePanorama : CapturePanorama.CapturePanorama
    {
        private Camera _Camera;

        public VRCapturePanorama()
        {
            // Get shaders
            fadeMaterial = UnityHelper.LoadFromAssetBundle<Material>(Resource.capture, "Fade material");
            convertPanoramaShader = UnityHelper.LoadFromAssetBundle<ComputeShader>(Resource.capture, "ConvertPanoramaShader");
            convertPanoramaStereoShader = UnityHelper.LoadFromAssetBundle<ComputeShader>(Resource.capture, "ConvertPanoramaStereoShader");
            textureToBufferShader = UnityHelper.LoadFromAssetBundle<ComputeShader>(Resource.capture, "TextureToBufferShader");

            captureStereoscopic = true;
            interpupillaryDistance = SteamVR.instance.GetFloatProperty(ETrackedDeviceProperty.Prop_UserIpdMeters_Float) * VR.Settings.IPDScale;
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
                _Camera = VR.Camera.Clone();
                _Camera.gameObject.SetActive(false);
            }

            // Set camera position & orientation
            _Camera.transform.position = VR.Camera.Head.position;

            var forward = Vector3.ProjectOnPlane(VR.Camera.Head.forward, Vector3.up).normalized;
            if (forward.magnitude < 0.1)
            {
                forward = Vector3.forward;
            }
            _Camera.transform.rotation = Quaternion.LookRotation(forward);

            return true;
        }


        public override void AfterRenderPanorama()
        {
            base.AfterRenderPanorama();
        }
    }
}
