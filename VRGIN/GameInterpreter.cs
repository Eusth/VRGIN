using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRGIN.Core
{
    public abstract class GameInterpreter : ProtectedBehaviour
    {
        /// <summary>
        /// Gets a list of actors in the game.
        /// </summary>
        public abstract IEnumerable<IActor> Actors { get; }

        public virtual bool IsEveryoneHeaded
        {
            get
            {
                return Actors.All(a => a.HasHead);
            }
        }
    }
}
