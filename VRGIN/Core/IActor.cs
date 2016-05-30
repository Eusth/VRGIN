using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core
{
    /// <summary>
    /// Represents an actor that takes part in the game.
    /// </summary>
    public interface IActor
    {
        /// <summary>
        /// Gets whether or not this object is still valid.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Gets the transform used for the eyes. 
        /// z+ = forward
        /// y+ = up
        /// </summary>
        Transform Eyes { get; }

        /// <summary>
        /// Gets or sets whether or not this actor still has his head.
        /// </summary>
        bool HasHead
        {
            get;
            set;
        }   


    }
}
