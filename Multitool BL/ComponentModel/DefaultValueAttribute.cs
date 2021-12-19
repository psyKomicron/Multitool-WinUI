using System;
using System.Diagnostics;
using System.Reflection;

namespace Multitool.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class DefaultValueAttribute : Attribute
    {
        private readonly object value;

        public DefaultValueAttribute(Type type, bool isPropertyValueType = false)
        {
            if (isPropertyValueType)
            {
                value = type;
            }
            else
            {
                try
                {
                    value = GetDefaultValue(type, null);
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Unable to get type default value. \n\tType: {type.FullName}\n{ex}");
                }
            }
        }

        public DefaultValueAttribute(object defaultValue)
        {
            value = defaultValue;
        }

        protected DefaultValueAttribute(Type type, params object[] ctorParams)
        {
            value = GetDefaultValue(type, ctorParams);
        }

        public object DefaultValue => value;

        private object GetDefaultValue(Type type, object[] constructorParameters)
        {
            ConstructorInfo ctorInfo = null;
            if (constructorParameters == null)
            {
                ctorInfo = type.GetConstructor(Array.Empty<Type>());
            }
            else
            {
                Type[] ctorTypes = new Type[constructorParameters.Length];
                for (int i = 0; i < constructorParameters.Length; i++)
                {
                    ctorTypes[i] = constructorParameters[i].GetType();
                }
                ctorInfo = type.GetConstructor(ctorTypes);
            }

            if (ctorInfo == null)
            {
                return null;
            }
            else
            {
                object value = null;
                try
                {
                    if (constructorParameters == null)
                    {
                        value = Convert.ChangeType(ctorInfo.Invoke(Array.Empty<object>()), type);
                    }
                    else
                    {
                        value = Convert.ChangeType(ctorInfo.Invoke(constructorParameters), type);
                    }
                }
                catch (InvalidCastException ex)
                {
                    Trace.TraceError(ex.ToString());
                }

                return value;
            }
        }
    }
}
