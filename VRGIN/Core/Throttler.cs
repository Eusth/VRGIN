using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core
{
    /// <summary>
    /// Keeps all Behaviours on a GameObject disabled except for Transform.
    /// </summary>
    class WhitelistThrottler : ProtectedBehaviour
    {
        /// <summary>
        /// Components that should not be disabled.
        /// </summary>
        public HashSet<Type> Exceptions = new HashSet<Type>();

        protected override void OnStart()
        {
            Exceptions.Add(typeof(Transform));
            Exceptions.Add(typeof(ProtectedBehaviour));
            base.OnStart();
        }

        protected override void OnUpdate()
        {
            foreach (var behaviour in GetComponents<Behaviour>().Where(c => !Exceptions.Contains(c.GetType())))
            {
                behaviour.enabled = false;
            }
            base.OnUpdate();
        }
    }

    class BlacklistThrottler : ProtectedBehaviour
    {
        /// <summary>
        /// Components that should be disabled.
        /// </summary>
        public HashSet<Type> Targets = new HashSet<Type>();

        protected override void OnStart()
        {
            Targets.Add(typeof(Camera));
            base.OnStart();
        }

        protected override void OnUpdate()
        {
            foreach (var behaviour in GetComponents<Behaviour>().Where(c => Targets.Contains(c.GetType())))
            {
                behaviour.enabled = false;
            }
            base.OnUpdate();
        }
    }
}
