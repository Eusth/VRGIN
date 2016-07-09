using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Visuals;

namespace VRGIN.U46.Helpers
{
    public class GuiScaler
    {
        GUIQuad _Gui;
        Vector3? _StartLeft;
        Vector3? _StartRight;
        Vector3? _StartScale;
        Quaternion? _StartRotation;
        Vector3? _StartPosition;
        Quaternion _StartRotationController;
        Vector3? _OffsetFromCenter;

        Transform _Left;
        Transform _Right;

        public GuiScaler(GUIQuad gui, Transform left, Transform right)
        {
            _Gui = gui;
            _Left = left;
            _Right = right;
            _StartLeft = left.position;
            _StartRight = right.position;
            _StartScale = _Gui.transform.localScale;
            _StartRotation = _Gui.transform.localRotation;
            _StartPosition = _Gui.transform.position;
            _StartRotationController = GetAverageRotation();

            var originalDistance = Vector3.Distance(_StartLeft.Value, _StartRight.Value);
            var originalDirection = _StartRight.Value - _StartLeft.Value;
            var originalCenter = _StartLeft.Value + originalDirection * 0.5f;
            _OffsetFromCenter = _Gui.transform.position - originalCenter;
        }

        Vector3 TopLeft
        {
            get
            {
                return _Left.position;
            }
        }

        Vector3 BottomRight
        {
            get
            {
                return _Right.position;
            }
        }

        Vector3 Center
        {
            get
            {
                return Vector3.Lerp(TopLeft, BottomRight, 0.5f);
            }
        }

        Vector3 Up
        {
            get
            {
                return Vector3.Lerp((VR.Camera.Head.position - TopLeft).normalized, (VR.Camera.Head.position - BottomRight).normalized, 0.5f);
            }
        }


        //Vector3 BottomLeft
        //{
        //    get
        //    {

        //        return Center (TopLeft - BottomRight) * 0.5f
        //    }
        //}




        public void Update()
        {
            if (!_Left || !_Right) return;
            var distance = Vector3.Distance(_Left.position, _Right.position);
            var originalDistance = Vector3.Distance(_StartLeft.Value, _StartRight.Value);
            var newDirection = _Right.position - _Left.position;
            var newCenter = _Left.position + newDirection * 0.5f;

            // It would probably be easier than that but Quaternions have never been a strength of mine...
            var inverseOriginRot = Quaternion.Inverse(VR.Camera.SteamCam.origin.rotation);
            var avgRot = GetAverageRotation();
            var rotation = (inverseOriginRot * avgRot) * Quaternion.Inverse(inverseOriginRot * _StartRotationController);

            _Gui.transform.localScale = (distance / originalDistance) * _StartScale.Value;
            _Gui.transform.localRotation = rotation * _StartRotation.Value;
            _Gui.transform.position = newCenter + (avgRot * Quaternion.Inverse(_StartRotationController)) * _OffsetFromCenter.Value;
        }

        private Quaternion GetAverageRotation()
        {
            var right = (_Right.position - _Left.position).normalized;
            var up = Vector3.Lerp(_Left.forward, _Right.forward, 0.5f);
            var forward = Vector3.Cross(right, up).normalized;

            return Quaternion.LookRotation(forward, up);
        }


    }
}
