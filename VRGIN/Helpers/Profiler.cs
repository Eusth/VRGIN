using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;

namespace VRGIN.Helpers
{
    public class Profiler : ProtectedBehaviour
    {
        private const int DEFAULT_SAMPLE_COUNT = 30;
        private const float INTERVAL_TIME = 0.01f;


        public delegate void Callback();

        private Callback _Callback;
        private double _CurrentInterval;

        public static void FindHotPaths(Callback callback)
        {
            if (!GameObject.Find("Profiler"))
            {
                var profiler = new GameObject("Profiler").AddComponent<Profiler>();
                profiler._Callback = callback;
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            //UnityEngine.Profiler.logFile = "E:\\profiler.log";
            //UnityEngine.Profiler.enabled = true;
            StartCoroutine(Measure());
        }

        private IEnumerator Measure()
        {

            List<GameObject> queue = UnityHelper.GetRootNodes().Except(new GameObject[] { gameObject }).Where(n => !n.name.StartsWith("VRGIN") && !n.name.StartsWith("[")).ToList();

            yield return StartCoroutine(MeasureFramerate(DEFAULT_SAMPLE_COUNT));
            double startInterval = _CurrentInterval;


            VRLog.Info("Starting to profile! This might take a while...");

            while (queue.Count > 0)
            {
                var obj = queue.First();
                queue.RemoveAt(0);

                // Ignore
                if (!obj.activeInHierarchy) continue;

                obj.SetActive(false);
                yield return StartCoroutine(MeasureFramerate(DEFAULT_SAMPLE_COUNT));
                obj.SetActive(true);

                // How much faster it is without this GO
                double impact = startInterval / _CurrentInterval;
                VRLog.Info("{0}{1}: {2:0.00}", string.Join("", Enumerable.Repeat(" ", obj.transform.Depth()).ToArray()), obj.name, impact);

                if (impact > 1.15f)
                {
                    queue.InsertRange(0, obj.Children());

                    // Do the same for components
                    foreach (var component in obj.GetComponents<Behaviour>().Where(c => c.enabled))
                    {
                        component.enabled = false;
                        yield return StartCoroutine(MeasureFramerate(DEFAULT_SAMPLE_COUNT));
                        component.enabled = true;
                        // How much faster it is without this comp
                        impact = startInterval / _CurrentInterval;
                        VRLog.Info("{0}{1} [{2}]: {3:0.000}", string.Join("", Enumerable.Repeat(" ", obj.transform.Depth()).ToArray()), obj.name, component.GetType().Name, impact);
                    }
                }
                yield return null;

            }
            VRLog.Info("Done!");

            _Callback();
            Destroy(gameObject);
        }

        private IEnumerator MeasureFramerate(int sampleCount)
        {
            yield return new WaitForSeconds(INTERVAL_TIME);


            long[] samples = new long[sampleCount];

            yield return null;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < sampleCount; i++)
            {
                stopwatch.Reset();
                stopwatch.Start();
                yield return null;
                samples[i] = stopwatch.ElapsedMilliseconds;
            }

            _CurrentInterval = samples.Average();

            yield return new WaitForSeconds(INTERVAL_TIME);

        }
    }
}
