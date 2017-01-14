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

        public bool Check(Camera camera)
        {
            return !camera.GetComponent("UICamera") && !camera.name.Contains("VR");
        }

        public IEnumerable<RenderTexture> GetTextures()
        {
            yield return _Texture;
        }

        public void OnAssign(Camera camera)
        {
            camera.enabled = false;
        }

        public CameraConsumer()
        {
            _Texture = new RenderTexture(1, 1, 0);
        }
    }
}
