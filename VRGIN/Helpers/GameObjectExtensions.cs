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

        public static IEnumerable<GameObject> Children(this GameObject gameObject)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                yield return gameObject.transform.GetChild(i).gameObject;
            }
        }

        public static IEnumerable<GameObject> Descendants(this GameObject gameObject)
        {
            Queue<GameObject> queue = new Queue<GameObject>();
            queue.Enqueue(gameObject);

            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();

                yield return obj;

                // Enqueue children
                foreach(var child in obj.Children())
                {
                    queue.Enqueue(child);
                }
            }
        }

        /// <summary>
        /// Makes a breadth-first search for a gameObject with a tag.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static IEnumerable<GameObject> FindGameObjectsByTag(this GameObject gameObject, string tag)
        {
            return gameObject.Children().Where(child => child.CompareTag(tag));
        }

        public static GameObject FIndGameObjectByTag(this GameObject gameObject, string tag)
        {
            return gameObject.FindGameObjectsByTag(tag).FirstOrDefault();
        }

    }
}
