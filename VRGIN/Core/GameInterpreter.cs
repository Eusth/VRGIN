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
    }
}
