using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRGIN.Core
{
    /// <summary>
    /// Behaviour that wraps a few of Unity's methods in try/catch blocks.
    /// </summary>
    public class ProtectedBehaviour : MonoBehaviour
    {
        private static IDictionary<string, long> PerformanceTable = new Dictionary<string, long>();

        private string GetKey(string method)
        {
            return String.Format("{0}#{1}",GetType().FullName,method);
        }
        protected void Start()
        {
            SafelyCall(OnStart);
        }

        protected void Awake()
        {
            SafelyCall(OnAwake);
        }

        protected void Update()
        {
            SafelyCall(OnUpdate);
        }

        protected void LateUpdate()
        {
            SafelyCall(OnLateUpdate);
        }

        protected void FixedUpdate()
        {
            SafelyCall(OnFixedUpdate);
        }

        protected void OnLevelWasLoaded(int level)
        {
            SafelyCall(delegate { OnLevel(level); });
        }

        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnLateUpdate() { }
        protected virtual void OnFixedUpdate() { }
        protected virtual void OnAwake() { }
        protected virtual void OnLevel(int level) { }


        private void SafelyCall(Action action)
        {
            try
            {
                //StackFrame frame = new StackFrame(1);
                //var method = frame.GetMethod();
                //var key = GetKey(method.Name);

                //var stopWatch = Stopwatch.StartNew();
                
                action();

                //stopWatch.Stop();
                //if(!PerformanceTable.ContainsKey(key))
                //{
                //    PerformanceTable[key] = 0L;
                //}
                //PerformanceTable[key] += stopWatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        protected static void DumpTable()
        {
            Logger.Info("DUMP");
            var builder = new StringBuilder();

            var enumerator = PerformanceTable.GetEnumerator();
            while(enumerator.MoveNext())
            {
                builder.AppendFormat("{1}ms: {0}\n", enumerator.Current.Key, enumerator.Current.Value);
            }

            File.WriteAllText("performance.txt", builder.ToString());
        }
    }
}
