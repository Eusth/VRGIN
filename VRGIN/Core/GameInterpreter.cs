using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core
{

    public enum CameraJudgement
    {
        Ignore,
        SubCamera,
        MainCamera,
        GUI,
        GUIAndCamera
    }
    /// <summary>
    /// Class that is responsible to collect all required data from the game 
    /// that is created or managed at runtime.
    /// </summary>
    public abstract class GameInterpreter : ProtectedBehaviour
    {
        /// <summary>
        /// Gets a list of actors in the game. Used frequently.
        /// </summary>
        public abstract IEnumerable<IActor> Actors { get; }

        public virtual bool IsEveryoneHeaded
        {
            get
            {
                return Actors.All(a => a.HasHead);
            }
        }

        protected override void OnLevel(int level)
        {
            base.OnLevel(level);

            VRLog.Info("Loaded level {0}", level);
        }

        /// <summary>
        /// Finds the first actor who has no head (= is impersonated) or NULL.
        /// </summary>
        /// <returns></returns>
        public virtual IActor FindImpersonatedActor()
        {
            return Actors.FirstOrDefault(a => !a.HasHead);
        }

        public virtual IActor FindNextActorToImpersonate()
        {
            var actors = Actors.ToList();
            var currentlyImpersonated = FindImpersonatedActor();
            
            if(currentlyImpersonated != null)
            {
                actors.Remove(currentlyImpersonated);
            }

            return actors.OrderByDescending(actor => Vector3.Dot((actor.Eyes.position - VR.Camera.transform.position).normalized, VR.Camera.SteamCam.head.forward)).FirstOrDefault();

            //return currentlyImpersonated != null
            //    ? actors[(actors.IndexOf(currentlyImpersonated) + 1) % actors.Count]
            //    : actors.FirstOrDefault();
        }



        /// <summary>
        /// Finds the main camera object.
        /// </summary>
        /// <returns></returns>
        public virtual Camera FindCamera()
        {
            return Camera.main;
        }


        /// <summary>
        /// Finds additional cameras that should be considered (i.e. added to the culling mask).
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Camera> FindSubCameras()
        {
            return Camera.allCameras.Where(c => c.targetTexture == null).Except(new Camera[] { Camera.main });
        }

        public CameraJudgement JudgeCamera(Camera camera)
        {
            if(camera.name.Contains("VRGIN") || camera.name == "poseUpdater")
            {
                return CameraJudgement.Ignore;
            } else
            {
                return JudgeCameraInternal(camera);
            }
        }

        protected virtual CameraJudgement JudgeCameraInternal(Camera camera)
        {
            bool guiInterested = VR.GUI.IsInterested(camera);
            if (camera.targetTexture == null)
            {
                if (guiInterested)
                {
                    return CameraJudgement.GUIAndCamera;
                }
                else if (camera.CompareTag("MainCamera"))
                {
                    return CameraJudgement.MainCamera;
                }
                else
                {
                    return CameraJudgement.SubCamera;
                }
            }
            return guiInterested ? CameraJudgement.GUI : CameraJudgement.Ignore;
        }
        /// <summary>
        /// Checks whether the collider is to be interpreted as body part.
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        public virtual bool IsBody(Collider collider)
        {
            return false;
        }

        /// <summary>
        /// Checks if a given canvas should be ignored.
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        public virtual bool IsIgnoredCanvas(Canvas canvas)
        {
            return false;
        }

        /// <summary>
        /// Checks whether an effect is eligible for VR. 
        /// </summary>
        /// <param name="effect"></param>
        /// <returns></returns>
        public virtual bool IsAllowedEffect(MonoBehaviour effect)
        {
            return true;
        }

        /// <summary>
        /// Gets the default culling mask that is always shown. Use <see cref="VRCamera.UpdateCameraConfig"/> to enforce a refresh.
        /// </summary>
        public virtual int DefaultCullingMask
        {
            get
            {
                return LayerMask.GetMask("Default");
            }
        }
    }
}
