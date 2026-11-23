using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaySuomi.Navigation
{
    /// <summary>
    /// Logs every empty [SerializeField] reference on a component. Without this, one would only
    /// show up as a null error when the code first used it.
    /// </summary>
    internal static class MissingReferences
    {
        private const BindingFlags OwnInstanceFields =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly;

        public static void Report(MonoBehaviour component)
        {
            for (
                Type type = component.GetType();
                type != typeof(MonoBehaviour);
                type = type.BaseType
            )
            {
                foreach (FieldInfo field in type.GetFields(OwnInstanceFields))
                {
                    if (field.IsDefined(typeof(SerializeField), false) && IsEmpty(field, component))
                    {
                        Debug.LogError($"{type.Name}: {field.Name} is not assigned.", component);
                    }
                }
            }
        }

        private static bool IsEmpty(FieldInfo field, MonoBehaviour component)
        {
            object value = field.GetValue(component);

            if (typeof(Object).IsAssignableFrom(field.FieldType))
            {
                return (Object)value == null;
            }

            if (typeof(Object).IsAssignableFrom(field.FieldType.GetElementType()))
            {
                foreach (Object element in (Array)value)
                {
                    if (element == null)
                    {
                        return true;
                    }
                }

                return ((Array)value).Length == 0;
            }

            return false;
        }
    }
}
