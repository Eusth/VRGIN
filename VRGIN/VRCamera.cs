using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core
{
    public class InitializeCameraEventArgs : EventArgs
    {
        public readonly Camera Camera;
        public readonly Camera Blueprint;

        public InitializeCameraEventArgs(Camera camera, Camera blueprint)
        {
            Camera = camera;
            Blueprint = blueprint;
        }
    }
    
    /// <summary>
    /// Handles the insertion of a OpenVR camera into an existing scene. The camera is controlled by a ControlMode.
    /// </summary>
    public class VRCamera : ProtectedBehaviour
    {
        private delegate void CameraOperation(Camera camera);

        private static VRCamera _Instance;
        public SteamVR_Camera SteamCam { get; private set; }
        public Camera Blueprint { get; private set; }

       
        public event EventHandler<InitializeCameraEventArgs> InitializeCamera = delegate { };

        public static VRCamera Instance
        {
            get
            {
                if(_Instance == null)
                {
                    _Instance = new GameObject("VR Camera").AddComponent<VRCamera>();
                }
                return _Instance;
            }
        }

        protected override void OnStart()
        {
            gameObject.AddComponent<SteamVR_Camera>();
            SteamCam = GetComponent<SteamVR_Camera>();
            SteamCam.Expand(); // Expand immediately!
        }

        public void Copy(Camera blueprint)
        {
            Blueprint = blueprint ?? GetComponent<Camera>();
            
            ApplyToCameras(targetCamera =>
            {
                targetCamera.nearClipPlane = Mathf.Clamp(0.01f, 0.001f, 0.01f);
                targetCamera.farClipPlane = Mathf.Clamp(100f, 50f, 200f);
                targetCamera.cullingMask = Blueprint.cullingMask & ~VRManager.Instance.Context.UILayerMask;
                targetCamera.clearFlags = Blueprint.clearFlags;
                targetCamera.backgroundColor = Blueprint.backgroundColor;
                //Console.WriteLine(ovrCamera.clearFlags);
                var skybox = Blueprint.GetComponent<Skybox>();
                if (skybox != null)
                {
                    var vrSkybox = targetCamera.gameObject.GetComponent<Skybox>();
                    if (vrSkybox == null) vrSkybox = vrSkybox.gameObject.AddComponent<Skybox>();

                    vrSkybox.material = skybox.material;
                }

                // Hook
                InitializeCamera(this, new InitializeCameraEventArgs(targetCamera, Blueprint));
            });

            SteamCam.origin.position = Vector3.zero;
            Blueprint.GetComponent<Camera>().cullingMask = 0;
        }

        private void ApplyToCameras(CameraOperation operation)
        {
            operation(SteamCam.GetComponent<Camera>());
            operation(SteamCam.head.GetComponent<Camera>());
        }
    }
}
