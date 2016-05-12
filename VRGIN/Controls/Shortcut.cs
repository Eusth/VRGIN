using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRGIN.Core.Helpers;

namespace VRGIN.Core.Controls
{
    public class KeyboardShortcut : IShortcut
    {
        public KeyStroke KeyStroke { get; private set; }
        public Action Action { get; private set; }
        public KeyMode CheckMode { get; private set; }

        public KeyboardShortcut(KeyStroke keyStroke, Action action, KeyMode checkMode = KeyMode.Press)
        {
            KeyStroke = keyStroke;
            Action = action;
            CheckMode = checkMode;
        }

        public void Evaluate()
        {
            if(KeyStroke.Check(CheckMode))
            {
                Action();
            }
        }
    }
}
