using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRGIN.Core;

namespace VRGIN.Helpers
{
    public class CameraMonitor : ProtectedBehaviour
    {
        Stopwatch _Stopwatch = new Stopwatch();

        public void OnPreCull()
        {
            _Stopwatch.Reset();
            _Stopwatch.Start();
        }
        
        public void OnPreRender()
        {
            _Stopwatch.Stop();
            VRLog.Info("{0}: Cull {1}ms", gameObject.name, _Stopwatch.Elapsed.TotalMilliseconds);
            _Stopwatch.Reset();
            _Stopwatch.Start();
        }

        public void OnPostRender()
        {
            _Stopwatch.Stop();
            VRLog.Info("{0}: Render {1}ms", gameObject.name, _Stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}
