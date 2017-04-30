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
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

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
