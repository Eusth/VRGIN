using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;

namespace VRGIN.Controls
{
    public class HelpText : ProtectedBehaviour
    {

        private Vector3 _TextOffset;
        private Vector3 _LineOffset;
        private Vector3 _HeightVector;
        private Vector3 _MovementVector;


        private Transform _Target;
        private string _Text;
        private static Material S_Material;

        private LineRenderer _Line;

        public static HelpText Create(string text, Transform target, Vector3 textOffset, Vector3? lineOffset = null)
        {
            var ht = new GameObject().AddComponent<HelpText>();
            ht._Text = text;
            ht._Target = target;

            // Normalize coordinates
            ht._TextOffset =  textOffset;
            ht._LineOffset = lineOffset.HasValue ? lineOffset.Value : Vector3.zero;

            var difference = lineOffset.HasValue
                    ? (textOffset - lineOffset.Value)
                    : textOffset;

            ht._HeightVector = Vector3.Project(difference, Vector3.up);
            ht._MovementVector = Vector3.ProjectOnPlane(difference, Vector3.up);

            return ht;
        }

        protected override void OnStart()
        {
            base.OnStart();

            transform.SetParent(_Target, false);
            // Build canvas
            var canvas = new GameObject().AddComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.WorldSpace;

            // Copied straight out of Unity
            canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);
            canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 70);

            transform.rotation = _Target.parent.rotation;

            canvas.transform.localScale = new Vector3(0.0001549628f, 0.0001549627f, 0);
            canvas.transform.localPosition = _TextOffset;
            canvas.transform.localRotation = Quaternion.Euler(90, 180, 180);

            //var img = canvas.gameObject.AddComponent<Image>();
            //var outline = canvas.gameObject.AddComponent<Outline>();

            // ----

            // Build text
            var text = new GameObject().AddComponent<Text>();
            text.transform.SetParent(canvas.transform, false);
            text.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            text.GetComponent<RectTransform>().anchorMax = Vector2.one;
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = 40;
            text.resizeTextMinSize = 1;
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.alignment = TextAnchor.MiddleCenter;

            text.text = _Text; // What a line...
            // ---

            // Build line renderer
            _Line = gameObject.AddComponent<LineRenderer>();
            _Line.material = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
            //_Line.material.renderQueue;
            _Line.SetColors(Color.cyan, Color.cyan);
            _Line.useWorldSpace = false;
            _Line.SetVertexCount(4);
            _Line.SetWidth(0.001f, 0.001f);

            var inverse = Quaternion.Inverse(_Target.localRotation);

            _Line.SetPosition(0, _LineOffset + _HeightVector * 0.1f);
            _Line.SetPosition(1, _LineOffset + _HeightVector * 0.5f + _MovementVector * 0.2f);
            _Line.SetPosition(2, _TextOffset - _HeightVector * 0.5f - _MovementVector * 0.2f);
            _Line.SetPosition(3, _TextOffset - _HeightVector * 0.1f);
            // ---

            // Build background
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = _TextOffset - Vector3.up * 0.001f;
            quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale = new Vector3(0.05539737f, 0.009849964f, 0);

            if(!S_Material)
            {

                S_Material = VRManager.Instance.Context.Materials.Unlit;
                S_Material.color = Color.white;

            }
            quad.transform.GetComponent<Renderer>().sharedMaterial = S_Material;

            quad.GetComponent<Collider>().enabled = false;
            //
        }


        protected override void OnUpdate()
        {
            base.OnUpdate();
        }
    }
}
