using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core.Helpers;

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
                    _Instance = new GameObject("VR Camera").AddComponent<AudioListener>().gameObject.AddComponent<VRCamera>();
                }
                return _Instance;
            }
        }

        protected override void OnAwake()
        {
            gameObject.AddComponent<SteamVR_Camera>();
            SteamCam = GetComponent<SteamVR_Camera>();
            SteamCam.Expand(); // Expand immediately!
            
            DontDestroyOnLoad(SteamCam.origin.gameObject);
        }

        public void Copy(Camera blueprint)
        {
            Logger.Info("Copying camera: {0}", blueprint ? blueprint.name : "NULL" );
            Blueprint = blueprint ?? GetComponent<Camera>();

            int cullingMask = Blueprint.cullingMask;
            if(cullingMask == 0)
            {
                cullingMask = int.MaxValue;
            }

            // Apply to both the head camera and the VR camera
            ApplyToCameras(targetCamera =>
            {
                targetCamera.nearClipPlane = Mathf.Clamp(0.01f, 0.001f, 0.01f);
                targetCamera.farClipPlane = Mathf.Clamp(100f, 50f, 200f);
                targetCamera.cullingMask = cullingMask & ~(VRManager.Instance.Context.UILayerMask | LayerMask.GetMask(VR.Context.HMDLayer));
                targetCamera.clearFlags = Blueprint.clearFlags;
                targetCamera.backgroundColor = Blueprint.backgroundColor;
                //Logger.Info(ovrCamera.clearFlags);
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

            //CopyFX(Blueprint);
            if (Blueprint != GetComponent<Camera>())
            {
                Blueprint.GetComponent<Camera>().cullingMask = 0;

                // Highlander principle
                var listener = Blueprint.GetComponent<AudioListener>();
                if(listener)
                {
                    Destroy(listener);
                }
            }
        }

        /// <summary>
        /// Doesn't really work yet.
        /// </summary>
        /// <param name="blueprint"></param>
        public void CopyFX(Camera blueprint)
        {
            // Clean
            foreach (var fx in gameObject.GetCameraEffects())
            {
                Logger.Info("DESTROY {0}", fx.GetType().Name);
                DestroyImmediate(fx);
            }

            Logger.Info("Copying FX to {0}...", gameObject.name);
            // Rebuild
            foreach (var fx in blueprint.gameObject.GetCameraEffects())
            {
                Logger.Info("Copy FX: {0} (enabled={1})", fx.GetType().Name, fx.enabled);
                var attachedFx = gameObject.CopyComponentFrom(fx);
                attachedFx.enabled = fx.enabled;
            }

            Logger.Info("That's all.");

            SteamCam.ForceLast();
            SteamCam = GetComponent<SteamVR_Camera>();
        }

        private void ApplyToCameras(CameraOperation operation)
        {
            operation(SteamCam.GetComponent<Camera>());
            operation(SteamCam.head.GetComponent<Camera>());
        }

        public void Refresh()
        {
            CopyFX(Blueprint);
        }
    }
}
