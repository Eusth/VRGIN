using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRGIN.Controls
{
    public interface IShortcut : IDisposable
    {
        void Evaluate();
    }
}
