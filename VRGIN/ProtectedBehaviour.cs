using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core
{
    public class ProtectedBehaviour : MonoBehaviour
    {
        protected void Start()
        {
            SafelyCall(OnStart);
        }

        protected void Awake()
        {
            SafelyCall(OnAwake);
        }

        protected void Update()
        {
            SafelyCall(OnUpdate);
        }

        protected void LateUpdate()
        {
            SafelyCall(OnLateUpdate);
        }

        protected void FixedUpdate()
        {
            SafelyCall(OnFixedUpdate);
        }

        protected void OnLevelWasLoaded(int level)
        {
            SafelyCall(delegate { OnLevel(level); });
        }

        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnLateUpdate() { }
        protected virtual void OnFixedUpdate() { }
        protected virtual void OnAwake() { }
        protected virtual void OnLevel(int level) { }


        private void SafelyCall(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
