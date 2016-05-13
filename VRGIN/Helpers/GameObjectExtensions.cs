using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace VRGIN.Core.Helpers
{
    public static class GameObjectExtensions
    {
        public static IEnumerable<MonoBehaviour> GetCameraEffects(this GameObject go)
        {
            return go.GetComponents<MonoBehaviour>().Where(IsCameraEffect);
        }
        
        private static bool IsCameraEffect(MonoBehaviour component)
        {
            return IsImageEffect(component.GetType());
        }

        private static bool IsImageEffect(Type type)
        {
            return type != null && (type.Name.Contains("Effect") || IsImageEffect(type.BaseType));
        }

        public static T CopyComponentFrom<T>(this GameObject destination, T original) where T : Component
        {
            Type type = original.GetType();
            T copy = destination.AddComponent<T>();
            // Copied fields can be restricted with BindingFlags
            FieldInfo[] fields = type.GetFields( BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
       
            return copy;
        }

    }
}
