using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VRGIN.Helpers
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

        public static int Level(this GameObject go)
        {
            return go.transform.parent ? go.transform.parent.gameObject.Level() + 1 : 0;
        }

        private static bool IsImageEffect(Type type)
        {
            return type != null && (type.Name.EndsWith("Effect") || type.Name.Contains("AmbientOcclusion") || IsImageEffect(type.BaseType));
        }

        public static U CopyComponentFrom<T, U>(this GameObject destination, T original) where T : Component where U : T
        {
            var type = typeof(T);
            U copy = destination.AddComponent<U>() as U;
            // Copied fields can be restricted with BindingFlags
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));

            }

            return copy;
        }

        public static T CopyComponentFrom<T>(this GameObject destination, T original) where T : Component
        {
            Type type = original.GetType();
            T copy = destination.AddComponent(type) as T;
            // Copied fields can be restricted with BindingFlags
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }

            return copy;
        }

        public static string GetPath(this Component component)
        {
            return component.transform.parent
                ? component.transform.parent.GetPath() + "/" + component.name
                : component.name;
        }

        public static IEnumerable<GameObject> Children(this GameObject gameObject)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                yield return gameObject.transform.GetChild(i).gameObject;
            }
        }

        public static IEnumerable<Transform> Children(this Transform transform)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                yield return transform.GetChild(i);
            }
        }

        public static IEnumerable<Transform> Ancestors(this Transform transform)
        {
            var t = transform;

            while (t.parent)
            {
                t = t.parent;
                yield return t;
            }
        }

        public static int Depth(this Transform transform)
        {
            return transform.Ancestors().Count();
        }

        public static IEnumerable<GameObject> Descendants(this GameObject gameObject)
        {
            Queue<GameObject> queue = new Queue<GameObject>();
            queue.Enqueue(gameObject);

            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();


                // Enqueue children
                foreach (var child in obj.Children())
                {
                    yield return child;
                    queue.Enqueue(child);
                }
            }
        }

        public static IEnumerable<Transform> Descendants(this Transform transform)
        {
            return transform.gameObject.Descendants().Select(d => d.transform);
        }


        public static Transform FindDescendant(this Transform transform, string name)
        {
            return transform.Descendants().FirstOrDefault(d => d.name == name);
        }

        public static Transform FindDescendant(this Transform transform, Regex name)
        {
            return transform.Descendants().FirstOrDefault(d => name.IsMatch(d.name));
        }

        /// <summary>
        /// Makes a breadth-first search for a gameObject with a tag.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static IEnumerable<GameObject> FindGameObjectsByTag(this GameObject gameObject, string tag)
        {
            return gameObject.Descendants().Where(child => child.CompareTag(tag));
        }

        public static GameObject FindGameObjectByTag(this GameObject gameObject, string tag)
        {
            return gameObject.FindGameObjectsByTag(tag).FirstOrDefault();
        }



    }
}
