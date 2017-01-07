using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;
using VRGIN.Helpers;

namespace VRGIN.Visuals
{
    /// <summary>
    /// Cursor Quad that simulates a cursor when the game uses a hardware cursor.
    /// </summary>
    public class SimulatedCursor : ProtectedBehaviour
    {
        private Renderer _Renderer;
        private Canvas _Canvas;
        private CanvasScaler _Scaler;
        private Image _Cursor;
        private Texture2D _Sprite;
        private Texture2D _DefaultSprite;
        private Vector2 _Scale;

        /// <summary>
        /// Creates a new SimulatedCursor. Use this to make one.
        /// </summary>
        /// <returns></returns>
        public static SimulatedCursor Create()
        {
            var cursor = new GameObject("VRGIN_Cursor")
                .AddComponent<SimulatedCursor>();

            return cursor;
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            _DefaultSprite = UnityHelper.LoadImage("cursor.png");
            _Scale = new Vector2(_DefaultSprite.width, _DefaultSprite.height) * 0.5f;

            //_Canvas = gameObject.AddComponent<Canvas>();
            //_Canvas.sortingOrder = 100;
            //_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            //_Scaler = gameObject.AddComponent<CanvasScaler>();
            //_Scaler.dynamicPixelsPerUnit = 100;

            //_Cursor = new GameObject().AddComponent<Image>();
            //_Cursor.transform.SetParent(_Canvas.transform, false);

            //var texture = UnityHelper.LoadImage("cursor.png");
            //_Cursor.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, 100);

            //var rectTransform = _Cursor.GetComponent<RectTransform>();
            //rectTransform.anchorMin = Vector2.zero;
            //rectTransform.anchorMax = Vector2.zero;
            //rectTransform.pivot = new Vector2(0, 1);
            //rectTransform.sizeDelta = new Vector2(texture.width / 2, texture.height / 2);

            //gameObject.layer = LayerMask.NameToLayer(VR.Context.UILayer);
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

//        protected override void OnUpdate()
//        {
//            base.OnUpdate();

//#if UNITY_4_5
//            if (Screen.showCursor)
//#else
//            if (Cursor.visible)
//#endif
//            {
//                if (!_Canvas.enabled)
//                {
//                    _Canvas.enabled = true;
//                }

//                _Cursor.GetComponent<RectTransform>().anchoredPosition = Input.mousePosition;
//            }
//            else
//            {
//                _Canvas.enabled = false;
//            }
//        }

        void OnGUI()
        {
            // Just before the VRGUI hook kicks in
            GUI.depth = int.MinValue + 1;
#if UNITY_4_5
            if (Screen.showCursor)
#else
            if (Cursor.visible)
#endif
            {
                var pos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                
                GUI.DrawTexture(new Rect(pos.x, pos.y, _Scale.x, _Scale.y), _Sprite ?? _DefaultSprite);

                //_Cursor.GetComponent<RectTransform>().anchoredPosition = Input.mousePosition;
            }

        }

        public void SetCursor(Texture2D texture)
        {
            _Sprite = texture;
        }
    }
}
