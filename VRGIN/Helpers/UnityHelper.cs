using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace VRGIN.Core.Helpers
{
    /// <summary>
    /// A collection of helper methods that can be used in the Unity context.
    /// </summary>
    public static class UnityHelper
    {
        internal static Shader GetShader(string name)
        {
#if UNITY_4_5
            var assetBundle = AssetBundle.CreateFromMemoryImmediate(Resource.steamvr);
            var shader = Shader.Instantiate(assetBundle.Load(name)) as Shader;
            assetBundle.Unload(false);
            return shader;
#else
            throw new NotImplementedException();
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
                Logger.Warn("File " + filePath + " does not exist");
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
    }
}
