﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers
{
    /**
    * Manages the eye target for a single actor.
    */
    public class LookTargetController : ProtectedBehaviour
    {
        /// <summary>
        /// Gets the target for the gaze.
        /// </summary>
        public Transform Target { get; private set; }

        private Transform _RootNode;

        /// <summary>
        /// Gets or sets the offset in meters from the camera (shifts the eye focus)
        /// </summary>
        public float Offset = 0.5f;

        public static LookTargetController Attach<T>(DefaultActor<T> actor) where T : MonoBehaviour
        {
            var controller = actor.Actor.gameObject.AddComponent<LookTargetController>();
            controller._RootNode = actor.Eyes;

            return controller;
        }
    
        protected override void OnStart()
        {
            base.OnStart();
            CreateTarget();
        }

        private void CreateTarget()
        {
            Target = new GameObject("VRGIN_LookTarget").transform;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (_RootNode && VR.Camera.SteamCam.head.transform)
            {
                if(!Target)
                {
                    CreateTarget();
                }
                var camera = VR.Camera.SteamCam.head.transform;
                var dir = (camera.position - _RootNode.position).normalized;

                Target.transform.position = camera.position + dir * Offset;
            }
        }

        void OnDestroy()
        {
            // Character was destroyed, so destroy the created target!
            Destroy(Target.gameObject);
        }
    }
}
