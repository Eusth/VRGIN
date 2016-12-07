using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core
{
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
            yield break;
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
    }
}
