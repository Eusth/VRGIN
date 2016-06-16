using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Native;
using VRGIN.Visuals;
using static VRGIN.Native.WindowsInterop;

namespace VRGIN.Controls.Handlers
{
    /// <summary>
    /// Handler that is in charge of the menu interaction with controllers
    /// </summary>
    public class MenuHandler : ProtectedBehaviour
    {
        private Controller _Controller;
        const float RANGE = 0.25f;
        private const int MOUSE_STABILIZER_THRESHOLD = 30; // pixels
        private Controller.Lock _LaserLock = Controller.Lock.Invalid;
        private LineRenderer Laser;
        private Vector2? mouseDownPosition;
        private GUIQuad _Target;
        MenuHandler _Other;
        ResizeHandler _ResizeHandler;

        protected override void OnStart()
        {
            base.OnStart();
            _Controller = GetComponent<Controller>();
            _Other = _Controller.Other.GetComponent<MenuHandler>();
            InitLaser();
        }

        /// <summary>
        /// Gets the attached controller input object.
        /// </summary>
        protected SteamVR_Controller.Device Device
        {
            get
            {
                return SteamVR_Controller.Input((int)_Controller.Tracking.index);
            }
        }

        void InitLaser()
        {
            Laser = new GameObject().AddComponent<LineRenderer>();
            Laser.transform.SetParent(transform, false);
            Laser.material = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
            Laser.material.renderQueue += 1000;
            Laser.SetColors(Color.cyan, Color.cyan);
            Laser.transform.localRotation = Quaternion.Euler(60, 0, 0);
            Laser.transform.position += Laser.transform.forward * 0.07f;
            Laser.SetVertexCount(2);
            Laser.useWorldSpace = true;
            Laser.SetWidth(0.002f, 0.002f);
        }


        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (LaserVisible)
            {
                if (IsResizing)
                {
                    Laser.SetPosition(0, Laser.transform.position);
                    Laser.SetPosition(1, Laser.transform.position);
                }
                else
                {
                    UpdateLaser();
                }

            }
            else if (_Controller.CanAcquireFocus())
            {
                CheckForNearMenu();
            }
        }

        private void OnDisable()
        {
            if (_LaserLock.IsValid)
            {
                // Release to be sure
                _LaserLock.Release();
            }
        }

        private void EnsureResizeHandler()
        {
            if (!_ResizeHandler)
            {
                _ResizeHandler = _Target.GetComponent<ResizeHandler>();
                if (!_ResizeHandler)
                {
                    _ResizeHandler = _Target.gameObject.AddComponent<ResizeHandler>();
                }
            }
        }

        private void EnsureNoResizeHandler()
        {
            if (_ResizeHandler)
            {
                DestroyImmediate(_ResizeHandler);
            }
            _ResizeHandler = null;
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (LaserVisible)
            {
                if (_Other.LaserVisible && _Other._Target == _Target)
                {
                    // No double input - this is handled by ResizeHandler
                    EnsureResizeHandler();
                }
                else
                {
                    EnsureNoResizeHandler();
                }

                if (!IsResizing)
                {
                    if (Device.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger))
                    {
                        MouseOperations.MouseEvent(MouseEventFlags.LeftDown);
                        mouseDownPosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    }
                    if (Device.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger))
                    {
                        MouseOperations.MouseEvent(MouseEventFlags.LeftUp);
                        mouseDownPosition = null;
                    }

                    if (Device.GetPressUp(EVRButtonId.k_EButton_Grip))
                    {
                        var menuTool = _Controller.GetComponent<MenuTool>();
                        if (menuTool && !menuTool.Gui)
                        {
                            menuTool.TakeGUI(_Target);
                            _Controller.ToolIndex = _Controller.Tools.IndexOf(menuTool);
                        }
                    }
                }
            }
        }
        bool IsResizing
        {
            get
            {
                return _ResizeHandler && _ResizeHandler.IsDragging;
            }
        }
        void CheckForNearMenu()
        {
            _Target = GUIQuadRegistry.Quads.FirstOrDefault(IsLaserable);
            if (_Target)
            {
                LaserVisible = true;
            }
        }

        bool IsLaserable(GUIQuad quad)
        {
            RaycastHit hit;
            return IsWithinRange(quad) && Raycast(quad, out hit);
        }

        float GetRange(GUIQuad quad)
        {
            return Mathf.Max(RANGE, quad.transform.localScale.sqrMagnitude * RANGE);
        }
        bool IsWithinRange(GUIQuad quad)
        {
            // Needs to be in another hierarchy
            if (quad.transform.parent == transform) return false;

            var normal = -quad.transform.forward;
            var otherPos = quad.transform.position;

            var myPos = Laser.transform.position;
            var laser = Laser.transform.forward;
            var heightOverMenu = -quad.transform.InverseTransformPoint(myPos).z;

            return heightOverMenu > 0 && heightOverMenu < GetRange(quad)
                && Vector3.Dot(normal, laser) < 0; // They have to point the other way

        }

        bool Raycast(GUIQuad quad, out RaycastHit hit)
        {
            var myPos = Laser.transform.position;
            var laser = Laser.transform.forward;
            var collider = quad.GetComponent<Collider>();
            var ray = new Ray(myPos, laser);
            // So far so good. Now raycast!
            return collider.Raycast(ray, out hit, GetRange(quad));
        }

        void UpdateLaser()
        {
            Laser.SetPosition(0, Laser.transform.position);
            Laser.SetPosition(1, Laser.transform.position + Laser.transform.forward);

            if (_Target && _Target.gameObject.activeInHierarchy)
            {
                RaycastHit hit;
                if (IsWithinRange(_Target) && Raycast(_Target, out hit))
                {
                    Laser.SetPosition(1, hit.point);

                    var newPos = new Vector2(hit.textureCoord.x * Screen.width, (1 - hit.textureCoord.y) * Screen.height);

                    if (!mouseDownPosition.HasValue || Vector2.Distance(mouseDownPosition.Value, newPos) > MOUSE_STABILIZER_THRESHOLD)
                    {
                        MouseOperations.SetClientCursorPosition((int)newPos.x, (int)newPos.y);
                        mouseDownPosition = null;
                    }
                }
                else
                {
                    // Out of view
                    LaserVisible = false;
                }
            }
            else
            {
                // May day, may day -- window is gone!
                LaserVisible = false;
            }
        }


        public bool LaserVisible
        {
            get
            {
                return Laser.gameObject.activeSelf;
            }
            set
            {
                if (value && !_LaserLock.IsValid)
                {
                    // Need to acquire focus!
                    _LaserLock = _Controller.AcquireFocus();
                    if (!_LaserLock.IsValid)
                    {
                        // Could not get focus, do nothing.
                        return;
                    }
                }
                else if (!value && _LaserLock.IsValid)
                {
                    // Need to release focus!
                    _LaserLock.Release();
                }

                // Toggle laser
                Laser.gameObject.SetActive(value);

                // Initialize start position
                if (value)
                {
                    Laser.SetPosition(0, Laser.transform.position);
                    Laser.SetPosition(1, Laser.transform.position);
                }
                else
                {
                    mouseDownPosition = null;
                }
            }
        }

        class ResizeHandler : ProtectedBehaviour
        {
            GUIQuad _Gui;
            Vector3? _StartLeft;
            Vector3? _StartRight;
            Vector3? _StartScale;
            Quaternion? _StartRotation;
            Vector3? _StartPosition;

            public bool IsDragging { get; private set; }
            protected override void OnStart()
            {
                base.OnStart();
                _Gui = GetComponent<GUIQuad>();
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();
                IsDragging = GetDevice(VR.Mode.Left).GetPress(EVRButtonId.k_EButton_SteamVR_Trigger) &&
                       GetDevice(VR.Mode.Right).GetPress(EVRButtonId.k_EButton_SteamVR_Trigger);

                if (IsDragging)
                {
                    if (_StartScale == null)
                    {
                        Initialize();
                    }
                    var newLeft = VR.Mode.Left.transform.position;
                    var newRight = VR.Mode.Right.transform.position;

                    var distance = Vector3.Distance(newLeft, newRight);
                    var originalDistance = Vector3.Distance(_StartLeft.Value, _StartRight.Value);
                    var originalDirection = _StartRight.Value - _StartLeft.Value;
                    var newDirection = newRight - newLeft;
                    var originalCenter = _StartLeft.Value + originalDirection * 0.5f;
                    var newCenter = newLeft + newDirection * 0.5f;

                    var rotation = Quaternion.FromToRotation(originalDirection, newDirection);

                    _Gui.transform.localScale = (distance / originalDistance) * _StartScale.Value;
                    _Gui.transform.localRotation = rotation * _StartRotation.Value;
                    _Gui.transform.position = _StartPosition.Value + (newCenter - originalCenter);

                }
                else
                {
                    _StartScale = null;
                }

            }
            private void Initialize()
            {
                _StartLeft = VR.Mode.Left.transform.position;
                _StartRight = VR.Mode.Right.transform.position;
                _StartScale = _Gui.transform.localScale;
                _StartRotation = _Gui.transform.localRotation;
                _StartPosition = _Gui.transform.position;
            }


            private SteamVR_Controller.Device GetDevice(Controller controller)
            {
                return SteamVR_Controller.Input((int)controller.Tracking.index);
            }
        }
    }
}
