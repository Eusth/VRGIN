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
                timeAbandoned = Time.time;
                Gui.IsOwned = false;
                Gui.transform.SetParent(null, true);
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

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            var device = this.Controller;

            if (device.GetPressDown(EVRButtonId.k_EButton_Axis0))
            {
                MouseOperations.MouseEvent(MouseEventFlags.LeftDown);
                pressDownTime = Time.time;
            }

            if (device.GetPressUp(EVRButtonId.k_EButton_Grip))
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

            if (device.GetTouchDown(EVRButtonId.k_EButton_Axis0))
            {
                touchDownPosition = device.GetAxis();
                touchDownMousePosition = MouseOperations.GetClientCursorPosition();
            }
            if (device.GetTouch(EVRButtonId.k_EButton_Axis0) && (Time.time - pressDownTime) > 0.3f)
            {
                var P = touchDownMousePosition;
                var diff = device.GetAxis() - touchDownPosition;

                P.X = (int)(P.X + (diff.x * Screen.width * 0.25f));
                P.Y = (int)(P.Y + (-diff.y * Screen.height * 0.25f));

                MouseOperations.SetClientCursorPosition(P.X, P.Y);
            }

            if (device.GetPressUp(EVRButtonId.k_EButton_Axis0))
            {
                MouseOperations.MouseEvent(MouseEventFlags.LeftUp);
                pressDownTime = 0;
            }
        }

        public override List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>(new HelpText[] {
                HelpText.Create("Tap to click", FindAttachPosition("trackpad"), new Vector3(0, 0.02f, 0.05f)),
                HelpText.Create("Slide to move cursor", FindAttachPosition("trackpad"), new Vector3(0.05f, 0.02f, 0), new Vector3(0.015f, 0, 0))
            });
        }
    }
}
