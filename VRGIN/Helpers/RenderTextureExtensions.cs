using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Helpers
{
    public static class RenderTextureExtensions
    {
        public static void SaveToFile(this RenderTexture renderTexture, string name)
        {
            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            var bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(name, bytes);
            UnityEngine.Object.Destroy(tex);
            RenderTexture.active = currentActiveRT;
        }
    }
}
