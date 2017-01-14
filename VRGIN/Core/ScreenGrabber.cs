using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core
{
    
    public interface IScreenGrabber
    {
        bool Check(Camera camera);
        IEnumerable<RenderTexture> GetTextures();
        void OnAssign(Camera camera);
    }

    public class ScreenGrabber : IScreenGrabber
    {
        public delegate bool JudgingMethod(Camera camera);

        // Predefined functions
        public static JudgingMethod FromList(IEnumerable<Camera> allowedCameras) => (Camera camera) => allowedCameras.Contains(camera);
        public static JudgingMethod FromList(params String[] allowedCameraNames) => (Camera camera) => allowedCameraNames.Contains(camera.name);


        private IList<Camera> _Cameras = new List<Camera>();
        private HashSet<Camera> _CheckedCameras = new HashSet<Camera>();
        public RenderTexture Texture { get; private set; }

        public int Height { get; private set; }
        public int Width { get; private set; }

        private JudgingMethod _Judge;

        public ScreenGrabber(int width, int height, JudgingMethod method)
        {
            Texture = new RenderTexture(width, height, 24, RenderTextureFormat.Default);
            Width = width;
            Height = height;

            _Judge = method;
        }

        public bool Check(Camera camera)
        {
            return _Judge(camera);
        }

        public IEnumerable<RenderTexture> GetTextures()
        {
            yield return Texture;
        }

        public void OnAssign(Camera camera)
        {
        }

    }
}
