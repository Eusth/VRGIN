using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core
{
    public abstract class DefaultActorBehaviour<T> : ProtectedBehaviour, IActor where T : MonoBehaviour
    {
        public T Actor { get; protected set; }

        public static A Create<A>(T nativeActor) where A : DefaultActorBehaviour<T>
        {
            var actor = nativeActor.GetComponent<A>();
            if(!actor)
            {
                actor = nativeActor.gameObject.AddComponent<A>();
                actor.Initialize(nativeActor);
            }
            return actor;
        }
        
        public virtual bool IsValid
        {
            get
            {
                return Actor;
            }
        }

        public abstract Transform Eyes
        {
            get;
        }


        public abstract bool HasHead
        {
            get;
            set;
        }

        protected virtual void Initialize(T actor)
        {
            this.Actor = actor;
            VRLog.Info("Creating character {0}", actor.name);
        }
    }
}
