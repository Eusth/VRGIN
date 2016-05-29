using System;
using System.Collections;
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
        private RenderTexture _MiniTexture;

        public event EventHandler<InitializeCameraEventArgs> InitializeCamera = delegate { };

        public static VRCamera Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new GameObject("VRGIN_Camera").AddComponent<AudioListener>().gameObject.AddComponent<VRCamera>();
                }
                return _Instance;
            }
        }

        protected override void OnAwake()
        {
            _MiniTexture = new RenderTexture(1, 1, 0);
            _MiniTexture.Create();

            gameObject.AddComponent<SteamVR_Camera>();
            SteamCam = GetComponent<SteamVR_Camera>();
            SteamCam.Expand(); // Expand immediately!

            // Set render scale to the value defined by the user
            SteamVR_Camera.sceneResolutionScale = VR.Settings.RenderScale;

            var legacyAnchor = new GameObject("CenterEyeAnchor");
            legacyAnchor.transform.SetParent(SteamCam.head);

            DontDestroyOnLoad(SteamCam.origin.gameObject);
        }

        public void Copy(Camera blueprint)
        {
            Logger.Info("Copying camera: {0}", blueprint ? blueprint.name : "NULL");
            Blueprint = blueprint ?? GetComponent<Camera>();

            int cullingMask = Blueprint.cullingMask;
            if (cullingMask == 0)
            {
                cullingMask = int.MaxValue;
            }
            else
            {
                // Apply additional culling masks
                foreach (var subCamera in VR.Interpreter.FindSubCameras())
                {
                    if (!subCamera.name.Contains(SteamCam.baseName))
                    {
                        cullingMask |= subCamera.cullingMask;
                    }
                }
            }

            cullingMask &= ~(VRManager.Instance.Context.UILayerMask | LayerMask.GetMask(VR.Context.HMDLayer));

            Logger.Info("The camera sees {0}", string.Join(", ", UnityHelper.GetLayerNames(cullingMask)));

            // Apply to both the head camera and the VR camera
            ApplyToCameras(targetCamera =>
            {
                targetCamera.nearClipPlane = 0.01f;
                targetCamera.farClipPlane = Blueprint.farClipPlane;
                targetCamera.cullingMask = cullingMask;
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

            if (Blueprint != GetComponent<Camera>())
            {
                //StartCoroutine(ExecuteDelayed(delegate { CopyFX(Blueprint); }));
                //CopyFX(Blueprint);

                Blueprint.cullingMask = 0;
                Blueprint.targetTexture = _MiniTexture;
                //Blueprint.gameObject.AddComponent<BlacklistThrottler>();

                // Highlander principle
                var listener = Blueprint.GetComponent<AudioListener>();
                if (listener)
                {
                    Destroy(listener);
                }
            }
        }

        private IEnumerator ExecuteDelayed(Action action)
        {
            yield return null;
            try
            {
                action();
            }
            catch (Exception e)
            {
                Logger.Error(e);
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
                DestroyImmediate(fx);
            }
            int comps = gameObject.GetComponents<Component>().Length;

            Logger.Info("Copying FX to {0}...", gameObject.name);
            // Rebuild
            foreach (var fx in blueprint.gameObject.GetCameraEffects())
            {
                //if (fx.GetType().Name.Contains("ColorCurves")) continue;
                Logger.Info("Copy FX: {0} (enabled={1})", fx.GetType().Name, fx.enabled);
                var attachedFx = gameObject.CopyComponentFrom(fx);
                if (attachedFx)
                {
                    Logger.Info("Attached!");
                }
                attachedFx.enabled = fx.enabled;
            }

            Logger.Info("That's all.");

            SteamCam.ForceLast();
            SteamCam = GetComponent<SteamVR_Camera>();
            Logger.Info("{0} components before the additions, {1} after", comps, gameObject.GetComponents<Component>().Length);
        }

        private void ApplyToCameras(CameraOperation operation)
        {
            operation(SteamCam.GetComponent<Camera>());
            //operation(SteamCam.head.GetComponent<Camera>());
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (SteamCam.origin)
            {
                SteamCam.origin.localScale = Vector3.one * VR.Settings.IPDScale;
            }
        }

        public void Refresh()
        {
            CopyFX(Blueprint);
        }
    }
}
