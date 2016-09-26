using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Core;

namespace VRGIN.Visuals
{
    public class PlayerCamera : ProtectedBehaviour
    {
        private SteamVR_RenderModel model;
        private Controller controller;
        private bool tracking;
        private static Vector3 S_Position;
        private static Quaternion S_Rotation;


        private Vector3 posOffset;
        private Quaternion rotOffset;

        public static bool Created { get; private set; }

        public static PlayerCamera Create()
        {
            Created = true;
            return GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<PlayerCamera>();
        }

        internal static void Remove()
        {
            if (Created)
            {
                Destroy(GameObject.FindObjectOfType<PlayerCamera>().gameObject);
                Created = false;
            }
        }
        
        protected void OnEnable()
        {
            VRGUI.Instance.Listen();
        }

        protected void OnDisable()
        {
            VRGUI.Instance.Unlisten();
        }

        protected override void OnAwake()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(transform, false);
            var sphere2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere2.transform.SetParent(transform, false);

            transform.localScale = 0.3f * Vector3.one;
            transform.localScale = new Vector3(0.2f, 0.2f, 0.4f);

            sphere.transform.localScale = Vector3.one * 0.3f;
            sphere.transform.localPosition = Vector3.forward * 0.5f;
            sphere2.transform.localScale = Vector3.one * 0.3f;
            sphere2.transform.localPosition = Vector3.up * 0.5f;

            GetComponent<Collider>().isTrigger = true;

            // Disable head camera, which is usually used for screen output
            model = new GameObject("Model").AddComponent<SteamVR_RenderModel>();
            model.transform.SetParent(VR.Camera.SteamCam.head, false);
            model.shader = VR.Context.Materials.StandardShader;
            model.SetDeviceIndex((int)OpenVR.k_unTrackedDeviceIndex_Hmd);
            model.gameObject.layer = LayerMask.NameToLayer(VR.Context.InvisibleLayer);

            var cam = gameObject.AddComponent<Camera>();
            cam.depth = 1;
            cam.nearClipPlane = 0.3f;
            cam.cullingMask = int.MaxValue & ~VR.Context.UILayerMask;

            transform.position = S_Position;
            transform.rotation = S_Rotation;
        }

        protected override void OnUpdate()
        {
            S_Position = transform.position;
            S_Rotation = transform.rotation;

            CheckInput();
        }

        protected void CheckInput()
        {
            if (controller)
            {
                if (!tracking && SteamVR_Controller.Input((int)controller.Tracking.index).GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    tracking = true;


                    posOffset = transform.position - controller.transform.position;
                    rotOffset = Quaternion.Inverse(controller.transform.rotation) * transform.rotation;
                }
                else if (tracking)
                {
                    if (SteamVR_Controller.Input((int)controller.Tracking.index).GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger))
                    {
                        tracking = false;
                    }
                    else
                    {
                        transform.position = controller.transform.position + posOffset;
                        transform.rotation = controller.transform.rotation * rotOffset;
                    }
                }
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            GetComponent<Renderer>().material.color = Color.red;
            controller = other.GetComponentInParent<Controller>();
            controller.ToolEnabled = false;
        }

        public void OnTriggerExit()
        {
            GetComponent<Renderer>().material.color = Color.white;
            controller.ToolEnabled = true;

            if (!tracking)
                controller = null;
        }


    }
}
