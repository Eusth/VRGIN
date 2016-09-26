using Leap.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Native;
using VRGIN.Visuals;

namespace VRGIN.Controls.LeapMotion
{
    public class LeapMenuHandler : ProtectedBehaviour
    {
        HandModel _Hand;
        private const int MOUSE_STABILIZER_THRESHOLD = 50; // pixels

        enum RelativePosition
        {
            Out,
            Hover,
            Behind
        }
        
        enum State
        {
            None,
            Hover,
            Press
        }
        
        class AnalyzationResult
        {
            public RelativePosition Position = RelativePosition.Out;
            public Vector3 ClosestPoint = Vector3.zero;
            public Vector2 TextureCoords;

            public AnalyzationResult() { }
        }

        // Height to control mouse pointer at
        const float HOVER_HEIGHT = 0.05f;
        const float MAX_DEPTH = 0.1f;
        const int FINGER_INDEX = 1;
        const int MAX_OVERLAP = 0;

        GUIQuad _Current;
        State _CurrentState = State.None;
        Vector2? mouseDownPosition;
        private Vector3 _ScaleVector;

        protected override void OnStart()
        {
            _Hand = GetComponent<HandModel>();
            _ScaleVector = new Vector2((float)VRGUI.Width / Screen.width, (float)VRGUI.Height / Screen.height);
            if (!_Hand)
            {
                VRLog.Error("Hand not found! Disabling...");
                enabled = false;
                return;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();


            if (!_Current)
            {
                foreach (var quad in GUIQuadRegistry.Quads)
                {
                    var result = AnalyzeQuad(quad);
                    if(result.Position == RelativePosition.Hover)
                    {
                        _Current = quad;
                        EnterState(State.Hover);
                        break;
                    }
                }
            } else
            {
                var result = AnalyzeQuad(_Current);

                if (result.TextureCoords != Vector2.zero)
                {
                    var newPos = new Vector2(result.TextureCoords.x * VRGUI.Width, (1 - result.TextureCoords.y) * VRGUI.Height);
                    if (!mouseDownPosition.HasValue || Vector2.Distance(mouseDownPosition.Value, newPos) > MOUSE_STABILIZER_THRESHOLD)
                    {
                        MouseOperations.SetClientCursorPosition((int)newPos.x, (int)newPos.y);
                        mouseDownPosition = null;
                    }
                }

                // Update state
                if (_CurrentState == State.Press)
                {
                    if (result.Position == RelativePosition.Out) {
                        EnterState(State.None);
                    } else if(result.Position == RelativePosition.Hover)
                    {
                        EnterState(State.Hover);
                    }
                }
                else if(_CurrentState == State.Hover)
                {
                    if(result.Position == RelativePosition.Behind)
                    {
                        EnterState(State.Press);
                    }
                    else if(result.Position == RelativePosition.Out)
                    {
                        EnterState(State.None);
                    }
                }
            }
        }

        void EnterState(State newState)
        {
            // LEAVE
            switch(_CurrentState)
            {
                case State.Press:
                    VR.Input.Mouse.LeftButtonUp();
                    mouseDownPosition = null;
                    break;
            }

            _CurrentState = newState;

            // ENTER
            switch(_CurrentState)
            {
                case State.Press:
                    mouseDownPosition = Vector3.Scale(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y), _ScaleVector);
                    VR.Input.Mouse.LeftButtonDown();
                    break;
                case State.None:
                    _Current = null;
                    break;
            }
        }

        AnalyzationResult AnalyzeQuad(GUIQuad quad)
        {
            var result = new AnalyzationResult();

            var quadCollider = quad.GetComponent<Collider>();
            if (quadCollider == null) return result;

            var normal = -quad.transform.forward;
            var pos = quad.transform.position;
            var tip = TipPosition;

            bool behind = Vector3.Dot(tip - pos, normal) < 0;

            // We're on the right side
            var dir = -normal;
            var origin = !behind ? tip : pos + Vector3.Reflect(tip - pos, normal);
            RaycastHit hitInfo;
            if (quadCollider.Raycast(new Ray(origin, dir), out hitInfo, 1.5f))
            {
                var maxHeight = behind ? MAX_DEPTH : HOVER_HEIGHT;

                if (hitInfo.distance <= maxHeight)
                {
                    // IN!
                    result.Position = behind ? RelativePosition.Behind : RelativePosition.Hover;
                }

                result.TextureCoords = hitInfo.textureCoord;
            }
            else
            {
                // OUT!
                result.Position = RelativePosition.Out;
            }

          


            //VRLog.Error(closestPoint);

            //RaycastHit hitInfo;
            //if(quadCollider.Raycast(new Ray(tip, (tip - closestPoint).normalized ), out hitInfo, 1.5f))
            //{
            //    result.TextureCoords = hitInfo.textureCoord;
            //} else
            //{
            //    VRLog.Error("Raycast missed its target!");
            //}


            return result;
        }

        Vector3 TipPosition
        {
            get
            {
                return _Hand.fingers[1].GetTipPosition();
            }
        }


    }
}
