﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers
{
    /// <summary>
    /// A collection of helper methods that can be used in the Unity context.
    /// </summary>
    public static class UnityHelper
    {
#if !UNITY_4_5
        private static AssetBundle _SteamVR;
        private static IDictionary<string, AssetBundle> _AssetBundles = new Dictionary<string, AssetBundle>();
#endif
        private static readonly MethodInfo _LoadFromMemory = typeof(AssetBundle).GetMethod("LoadFromMemory", new Type[] { typeof(byte[]) });
        private static readonly MethodInfo _CreateFromMemory = typeof(AssetBundle).GetMethod("CreateFromMemoryImmediate", new Type[] { typeof(byte[]) });


        private static Dictionary<Color, RayDrawer> _Rays = new Dictionary<Color, RayDrawer>();

        internal static Shader GetShader(string name)
        {
            return LoadFromAssetBundle<Shader>(
#if UNITY_4_5
                U46.U46.Resource.steamvr,
#else
                ResourceManager.SteamVR,
#endif
                name);
        }


        public static T LoadFromAssetBundle<T>(byte[] assetBundleBytes, string name) where T : UnityEngine.Object
        {
#if UNITY_4_5
            var assetBundle = AssetBundle.CreateFromMemoryImmediate(assetBundleBytes);
            //foreach(var asset in assetBundle.LoadAll())
            //{
            //    VRLog.Info(asset.name);
            //}
            VRLog.Info("Getting {0} from {1}", name, assetBundle.name);
            var obj = GameObject.Instantiate(assetBundle.Load(name)) as T;
            assetBundle.Unload(false);
            return obj;
#else
            var key = GetKey(assetBundleBytes);
            if (!_AssetBundles.ContainsKey(key))
            {
                _AssetBundles[key] = LoadAssetBundle(assetBundleBytes);
                if(_AssetBundles[key] == null)
                {
                    VRLog.Error("Looks like the asset bundle failed to load?");
                }
            } 

            try
            {
                VRLog.Info("Loading: {0} ({1})", name, key);
                //foreach (var asset in _AssetBundles[key].LoadAllAssets())
                //{
                //    VRLog.Info(asset.name);
                //}

                name = name.Replace("Custom/", "");
                var loadedAsset = _AssetBundles[key].LoadAsset<T>(name);
                if (!loadedAsset)
                {
                    VRLog.Error("Failed to load {0}", name);
                }

                return !typeof(Shader).IsAssignableFrom(typeof(T)) && !typeof(ComputeShader).IsAssignableFrom(typeof(T)) ? UnityEngine.Object.Instantiate<T>(loadedAsset) : loadedAsset;
            } catch(Exception e)
            {
                VRLog.Error(e);
                return null;
            }
#endif
        }

        private static AssetBundle LoadAssetBundle(byte[] bytes)
        {
            if (_LoadFromMemory != null)
            {
                return _LoadFromMemory.Invoke(null, new object[] { bytes }) as AssetBundle;
            } else if(_CreateFromMemory != null)
            {
                return _CreateFromMemory.Invoke(null, new object[] { bytes }) as AssetBundle;
            } else
            {
                VRLog.Error("Could not find a way to load AssetBundles!");
                return null;
            }
        }

        private static string CalculateChecksum(byte[] byteToCalculate)
        {
            int checksum = 0;
            foreach (byte chData in byteToCalculate)
            {
                checksum += chData;
            }
            checksum &= 0xff;
            return checksum.ToString("X2");
        }

        private static string GetKey(byte[] assetBundleBytes)
        {
            return CalculateChecksum(assetBundleBytes);
        }

        private static Dictionary<string, Transform> _DebugBalls = new Dictionary<string, Transform>();
        public static Transform GetDebugBall(string name)
        {
            Transform debugBall;
            if (!_DebugBalls.TryGetValue(name, out debugBall) || !debugBall)
            {
                debugBall = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                debugBall.transform.localScale *= 0.03f;
                _DebugBalls[name] = debugBall;
            }

            return debugBall;
        }

        public static void DrawDebugBall(Transform transform)
        {
            GetDebugBall(transform.GetInstanceID().ToString()).position = transform.position;
        }

        public static void DrawRay(Color color, Vector3 origin, Vector3 direction)
        {
            DrawRay(color, new Ray(origin, direction.normalized));
        }

        public static void DrawRay(Color color, Ray ray)
        {
            RayDrawer drawer;
            if(!_Rays.TryGetValue(color, out drawer) || !drawer)
            {
                drawer = RayDrawer.Create(color, ray);
                _Rays[color] = drawer;
            }

            drawer.Touch(ray);
        }

        public static Transform CreateGameObjectAsChild(string name, Transform parent, bool dontDestroy = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            if (dontDestroy)
            {
                GameObject.DontDestroyOnLoad(go);
            }

            return go.transform;
        }

        /// <summary>
        /// Loads an image from the images folder.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Texture2D LoadImage(string filePath)
        {
            string ovrDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Images");
            filePath = Path.Combine(ovrDirectory, filePath);

            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            }
            else
            {
                VRLog.Warn("File " + filePath + " does not exist");
            }
            return tex;
        }

        public static string[] GetLayerNames(int mask)
        {
            List<string> masks = new List<string>();
            for (int i = 0; i <= 31; i++) //user defined layers start with layer 8 and unity supports 31 layers
            {
                if ((mask & (1 << i)) != 0) masks.Add(LayerMask.LayerToName(i));
            }
            return masks.Select(m => m.Trim()).Where(m => m.Length > 0).ToArray();
        }


        public static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }

        public static void DumpScene(string path, bool onlyActive = false)
        {
            VRLog.Info("Dumping scene...");

            var rootArray = new JSONArray();
            foreach (var gameObject in UnityEngine.Object.FindObjectsOfType<GameObject>().Where(go => go.transform.parent == null))
            {
                rootArray.Add(AnalyzeNode(gameObject, onlyActive));
            }

            File.WriteAllText(path, rootArray.ToJSON(0));
            VRLog.Info("Done!");
        }

        public static void DumpObject(GameObject obj, string path)
        {
            VRLog.Info("Dumping object...");

            File.WriteAllText(path, AnalyzeNode(obj).ToJSON(0));

            VRLog.Info("Done!");
        }

        public static IEnumerable<GameObject> GetRootNodes()
        {
            return UnityEngine.Object.FindObjectsOfType<GameObject>().Where(go => go.transform.parent == null);
        }

        public static JSONClass AnalyzeComponent(Component c)
        {
            var comp = new JSONClass();

            foreach (var field in c.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                try
                {
                    var val = FieldToString(field.Name, field.GetValue(c));
                    if (val != null)
                    {
                        comp[field.Name] = val;
                    }
                }
                catch (Exception e)
                {
                    VRLog.Warn("Failed to get field {0}", field.Name);
                }
            }

            foreach (var prop in c.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                try
                {
                    if (prop.GetIndexParameters().Length == 0)
                    {
                        var val = FieldToString(prop.Name, prop.GetValue(c, null));
                        if (val != null)
                        {
                            comp[prop.Name] = val;
                        }
                    }
                }
                catch (Exception e)
                {
                    VRLog.Warn("Failed to get prop {0}", prop.Name);
                }
            }

            return comp;
        }

        public static JSONClass AnalyzeNode(GameObject go, bool onlyActive = false)
        {
            var obj = new JSONClass();

            obj["name"] = (go.name);
            obj["active"] = go.activeSelf.ToString();
            obj["tag"] = (go.tag);
            obj["layer"] = (LayerMask.LayerToName(go.gameObject.layer));
            obj["pos"] = go.transform.localPosition.ToString();
            obj["rot"] = go.transform.localEulerAngles.ToString();
            obj["scale"] = go.transform.localScale.ToString();

            var components = new JSONClass();
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null)
                {
                    VRLog.Warn("NULL component: " + c);
                    continue;
                }
               
                components[c.GetType().Name] = AnalyzeComponent(c);
            }


            var children = new JSONArray();
            foreach (var child in go.Children())
            {
                if (!onlyActive || child.activeInHierarchy)
                {
                    children.Add(AnalyzeNode(child, onlyActive));
                }
            }

            obj["Components"] = components;
            obj["Children"] = children;

            return obj;
        }

        private static string FieldToString(string memberName, object value)
        {
            if (value == null) return null;

            switch (memberName)
            {
                case "cullingMask":
                    return string.Join(", ", GetLayerNames((int)value));
                case "renderer":
                    return ((Renderer)value).material.shader.name;
                default:
                    if (value is Vector3)
                    {
                        var v = (Vector3)value;
                        return String.Format("({0:0.000}, {1:0.000}, {2:0.000})", v.x, v.y, v.z);
                    }
                    if (value is Vector2)
                    {
                        var v = (Vector2)value;
                        return String.Format("({0:0.000}, {1:0.000})", v.x, v.y);
                    }
                    return value.ToString();

            }
        }

        // -- COMPATIBILITY --
        public static void SetPropertyOrField<T>(T obj, string name, object value)
        {
            var prop = typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var field = typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (prop != null)
            {
                prop.SetValue(obj, value, null);
            }
            else if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                VRLog.Warn("Prop/Field not found!");
            }
        }

        public static object GetPropertyOrField<T>(T obj, string name)
        {
            var prop = typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var field = typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (prop != null)
            {
                return prop.GetValue(obj, null);
            }
            else if (field != null)
            {
                return field.GetValue(obj);
            }
            else
            {
                VRLog.Warn("Prop/Field not found!");
                return null;
            }
        }

        public static void SaveTexture(RenderTexture rt, string pngOutPath)
        {
            var oldRT = RenderTexture.active;
            try
            {
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();

                File.WriteAllBytes(pngOutPath, tex.EncodeToPNG());
                GameObject.Destroy(tex);
            }
            finally
            {
                RenderTexture.active = oldRT;
            }
        }
        
        private class RayDrawer : ProtectedBehaviour
        {

            public static RayDrawer Create(Color color, Ray ray)
            {
                var go = new GameObject("Ray Drawer (" + color + ")").AddComponent<RayDrawer>();
                go.gameObject.AddComponent<LineRenderer>();
                go._Ray = ray;
                go._Color = color;

                return go;
            }

            private Ray _Ray;
            private Color _Color;
            private float _LastTouch;
            private LineRenderer Renderer;

            public void Touch(Ray ray)
            {
                _LastTouch = Time.time;
                _Ray = ray;
                gameObject.SetActive(true);
            }
            protected override void OnStart()
            {
                base.OnStart();
                Renderer = GetComponent<LineRenderer>();
                Renderer.SetColors(_Color, _Color);
                Renderer.SetVertexCount(2);
                Renderer.useWorldSpace = true;
                Renderer.material = VR.Context.Materials.Unlit;
                Renderer.SetWidth(0.01f, 0.01f);
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();

                Renderer.SetPosition(0, Vector3.Distance(_Ray.origin, VR.Camera.transform.position) < 0.3f ? _Ray.origin + _Ray.direction * .3f : _Ray.origin);
                Renderer.SetPosition(1, _Ray.origin + _Ray.direction * 100f);
                CheckAge();
            }

            private void CheckAge()
            {
                if(Time.time - _LastTouch > 1f)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
