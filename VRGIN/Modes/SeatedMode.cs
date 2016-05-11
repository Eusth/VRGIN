using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRGIN.Core.Controls;

namespace VRGIN.Core.Modes
{
    public class SeatedMode : ControlMode
    {
        public override void Impersonate(IActor actor)
        {
        }
        
        public override void OnDestroy()
        {
        }

        public override IEnumerable<Type> Tools
        {
            get
            {
                return base.Tools.Concat(new Type[] { typeof(MenuTool), typeof(WarpTool) });
            }
        }
    }
}
