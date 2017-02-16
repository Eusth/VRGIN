using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers
{
    public class CameraConsumer : IScreenGrabber
    {
        private RenderTexture _Texture;
        private bool _SpareMainCamera;
        private bool _SoftMode;

        public bool Check(Camera camera)
        {
            return !camera.GetComponent("UICamera") && !camera.name.Contains("VR") && camera.targetTexture == null && (!camera.CompareTag("MainCamera") || !_SpareMainCamera);
        }

        public IEnumerable<RenderTexture> GetTextures()
        {
            yield return _Texture;
        }

        public void OnAssign(Camera camera)
        {
            if (_SoftMode)
            {
                camera.cullingMask = 0;
                camera.nearClipPlane = 1;
                camera.farClipPlane = 1;
            }
            else
            {
                camera.enabled = false;
            }
        }

        public CameraConsumer(bool spareMainCamera = false, bool softMode = false)
        {
            _SoftMode = softMode;
            _SpareMainCamera = spareMainCamera;
            _Texture = new RenderTexture(1, 1, 0);
        }
    }
}
