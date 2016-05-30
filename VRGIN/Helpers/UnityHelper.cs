using System;
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
#endif

        internal static Shader GetShader(string name)
        {
#if UNITY_4_5
            var assetBundle = AssetBundle.CreateFromMemoryImmediate(VRGIN.U46.Resource.steamvr);
            var shader = Shader.Instantiate(assetBundle.Load(name)) as Shader;
            assetBundle.Unload(false);
            return shader;
#else
            if(!_SteamVR)
            {
                _SteamVR = AssetBundle.LoadFromMemory(Resource.steamvr);
            } 

            try
            {
                name = name.Replace("Custom/", "");
                return _SteamVR.LoadAsset<Shader>(name);
            } catch(Exception e)
            {
                VRLog.Error(e);
                return null;
            }
#endif
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

        public static void DumpScene(string path)
        {
            VRLog.Info("Dumping scene...");
            
            var rootArray = new JSONArray();
            foreach (var gameObject in UnityEngine.Object.FindObjectsOfType<GameObject>().Where(go => go.transform.parent == null))
            {
                rootArray.Add(AnalyzeNode(gameObject));
            }

            File.WriteAllText(path, rootArray.ToJSON(0));
            VRLog.Info("Done!");

        }

        private static JSONClass AnalyzeNode(GameObject go)
        {

            var obj = new JSONClass();
            obj["name"] = (go.name);
            obj["active"] = go.activeSelf.ToString();
            obj["tag"] = (go.tag);
            obj["layer"] = (LayerMask.LayerToName(go.gameObject.layer));

            var components = new JSONClass();
            foreach(var c in go.GetComponents<Component>())
            {
                var comp = new JSONClass();

                foreach(var field in c.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    try {
                        var val = FieldToString(field.Name, field.GetValue(c));
                        if (val != null)
                        {
                            comp[field.Name] = val;
                        }
                    } catch (Exception e)
                    {
                        VRLog.Warn("Failed to get field {0}", field.Name);
                    }
                }

                //foreach (var prop in c.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                //{
                //    try
                //    {
                //        var val = FieldToString(prop.Name, prop.GetValue(c, null));
                //        if (val != null)
                //        {
                //            comp[prop.Name] = val;
                //        }
                //    } catch(Exception e)
                //    {
                //        Logger.Warn("Failed to get prop {0}", prop.Name);
                //    }
                //}
                
                components[c.GetType().Name] = comp;
            }


            var children = new JSONArray();
            foreach (var child in go.Children())
            {
                children.Add(AnalyzeNode(child));
            }

            obj["Components"] = components;
            obj["Children"] = children;

            return obj;
        }

        private static string FieldToString(string memberName, object value)
        {
            if (value == null) return null;

            switch(memberName)
            {
                case "cullingMask":
                    return string.Join(", ", GetLayerNames((int)value));
                case "renderer":
                    return ((Renderer)value).material.shader.name;
                default:
                    if(value is Vector3)
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
            
            if(prop != null)
            {
                prop.SetValue(obj, value, null);
            } else if(field != null)
            {
                field.SetValue(obj, value);
            } else
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
    }
}
