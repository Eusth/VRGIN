using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Native;
using VRGIN.Visuals;
using static SteamVR_Controller;
using static VRGIN.Native.WindowsInterop;

namespace VRGIN.Controls.Tools
{
    public class MenuTool : Tool
    {
        /// <summary>
        /// GUI that is attached to this controller
        /// </summary>
        public GUIQuad Gui { get; private set; }

        private float pressDownTime;
        private Vector2 touchDownPosition;
        private POINT touchDownMousePosition;
        private float timeAbandoned;

        private double _DeltaX = 0;
        private double _DeltaY = 0;

        public void TakeGUI(GUIQuad quad)
        {
            if (quad && !Gui && !quad.IsOwned)
            {
                Gui = quad;
                Gui.transform.parent = transform;
                Gui.transform.SetParent(transform, true);
                Gui.transform.localPosition = new Vector3(0, 0.05f, -0.06f);
                Gui.transform.localRotation = Quaternion.Euler(90, 0, 0);

                quad.IsOwned = true;
            }
        }

        public void AbandonGUI()
        {
            if (Gui)
            {
                timeAbandoned = Time.unscaledTime;
                Gui.IsOwned = false;
                Gui.transform.SetParent(VR.Camera.SteamCam.origin, true);
                Gui = null;
            }
        }

        public override Texture2D Image
        {
            get
            {
                return UnityHelper.LoadImage("icon_settings.png");
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            Gui = GUIQuad.Create();
            Gui.transform.parent = transform;
            Gui.transform.localScale = Vector3.one * .3f;
            Gui.transform.localPosition = new Vector3(0, 0.05f, -0.06f);
            Gui.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Gui.IsOwned = true;
            DontDestroyOnLoad(Gui.gameObject);

        }

        protected override void OnStart()
        {
            base.OnStart();

        }

        protected override void OnDestroy()
        {
            DestroyImmediate(Gui.gameObject);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Gui)
            {
                Gui.gameObject.SetActive(false);
            }

        }
        protected override void OnEnable()
        {
            base.OnEnable();

            if (Gui)
            {
                Gui.gameObject.SetActive(true);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            var device = this.Controller;

            if (device.GetPressDown(ButtonMask.Touchpad | ButtonMask.Trigger))
            {
                VR.Input.Mouse.LeftButtonDown();
                pressDownTime = Time.unscaledTime;
            }

            if (device.GetPressUp(ButtonMask.Grip))
            {
                if (Gui)
                {
                    AbandonGUI();
                }
                else
                {
                    TakeGUI(GUIQuadRegistry.Quads.FirstOrDefault(q => !q.IsOwned));
                }
            }

            if (device.GetTouchDown(ButtonMask.Touchpad))
            {
                touchDownPosition = device.GetAxis();
                touchDownMousePosition = MouseOperations.GetClientCursorPosition();
            }
            if (device.GetTouch(ButtonMask.Touchpad) && (Time.unscaledTime - pressDownTime) > 0.3f)
            {
                var pos = device.GetAxis();
                var diff = pos - (VR.HMD == HMDType.Oculus ? Vector2.zero : touchDownPosition);
                var factor = VR.HMD == HMDType.Oculus ? Time.unscaledDeltaTime * 5 : 1f;
                // We can only move by integral number of pixels, so accumulate them until we have an integral value
                _DeltaX += (diff.x * VRGUI.Width * 0.1 * factor);
                _DeltaY += (-diff.y * VRGUI.Height * 0.2 * factor);
                
                int deltaX = (int)(_DeltaX > 0 ? Math.Floor(_DeltaX) : Math.Ceiling(_DeltaX));
                int deltaY = (int)(_DeltaY > 0 ? Math.Floor(_DeltaY) : Math.Ceiling(_DeltaY));

                _DeltaX -= deltaX;
                _DeltaY -= deltaY;

                VR.Input.Mouse.MoveMouseBy(deltaX, deltaY);
                touchDownPosition = pos;
            }

            if (device.GetPressUp(ButtonMask.Touchpad | ButtonMask.Trigger))
            {
                VR.Input.Mouse.LeftButtonUp();
                pressDownTime = 0;
            }
        }

        public override List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>(new HelpText[] {
                HelpText.Create("Tap to click", FindAttachPosition("trackpad"), new Vector3(0, 0.02f, 0.05f)),
                HelpText.Create("Slide to move cursor", FindAttachPosition("trackpad"), new Vector3(0.05f, 0.02f, 0), new Vector3(0.015f, 0, 0)),
                HelpText.Create("Attach/Remove menu", FindAttachPosition("lgrip"), new Vector3(-0.06f, 0.0f, -0.05f))

            });
        }
    }
}
