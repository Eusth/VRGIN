using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Text.RegularExpressions;


namespace VRGIN.Helpers
{
    public enum KeyMode
    {
        PressDown,
        PressUp,
        Press
    }
    
    public class KeyStroke
    {

        List<KeyCode> modifiers = new List<KeyCode>();
        List<KeyCode> keys = new List<KeyCode>();

        private KeyCode[] MODIFIER_LIST = new KeyCode[] {
            KeyCode.LeftAlt,
            KeyCode.RightAlt,
            KeyCode.LeftControl,
            KeyCode.RightControl,
            KeyCode.LeftShift,
            KeyCode.RightShift,
        };

        public KeyStroke(string strokeString)
        {
            var strokes = strokeString.ToUpper()
                .Split('+', '-')
                .Select(key => key.Trim()).ToArray();

            for (int i = 0; i < strokes.Length; i++)
            {
                string stroke = strokes[i];
                switch (stroke)
                {
                    case "CTRL":
                        AddStroke(KeyCode.LeftControl);
                        break;
                    case "ALT":
                        AddStroke(KeyCode.LeftAlt);
                        break;
                    case "SHIFT":
                        AddStroke(KeyCode.LeftShift);
                        break;
                    default:
                        try
                        {
                            if (Regex.IsMatch(stroke, @"^\d$"))
                            {
                                stroke = "Alpha" + stroke;
                            }
                            if (Regex.IsMatch(stroke, @"^(LEFT|RIGHT|UP|DOWN)$"))
                            {
                                stroke += "ARROW";
                            }

                            AddStroke((KeyCode)Enum.Parse(typeof(KeyCode), stroke, true));
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("FAILED TO PARSE KEY \"{0}\"", stroke);
                        }
                        break;
                }
            }
            Init();
        }

        public KeyStroke(IEnumerable<KeyCode> strokes)
        {
            foreach (var stroke in strokes)
                AddStroke(stroke);

            Init();
        }

        private void Init()
        {
            if (modifiers.Count > 0 && keys.Count == 0)
            {
                keys.AddRange(modifiers);
                modifiers.Clear();
            }
        }

        private void AddStroke(KeyCode stroke)
        {
            if (MODIFIER_LIST.Contains(stroke))
                modifiers.Add(stroke);
            else
                keys.Add(stroke);

        }

        public bool Check(KeyMode mode = KeyMode.PressDown)
        {
            if (modifiers.Count == 0 && keys.Count == 0) return false;

            return modifiers.All(key => Input.GetKey(key))
                && keys.All(key => (mode == KeyMode.Press 
                                    ? Input.GetKey(key) 
                                    : (mode == KeyMode.PressDown
                                        ? Input.GetKeyDown(key)
                                        : Input.GetKeyUp(key))))
                && MODIFIER_LIST.Except(modifiers).All(invalidModifier => !Input.GetKey(invalidModifier));
        }

        public override string ToString()
        {
            return string.Join("+", modifiers.Select(m => m.ToString()).Union(
                                        keys.Select(k => k.ToString())
                                    ).ToArray());
        }
    }
}
