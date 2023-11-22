namespace UniT.DI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class DependencyContainer
    {
        #region Manual Add

        public static void Add(Type type, object instance)
        {
            GetCache(type).Add(instance);
        }

        public static void Add(object instance)
        {
            Add(instance.GetType(), instance);
        }

        public static void AddInterfaces(object instance)
        {
            foreach (var @interface in instance.GetType().GetInterfaces())
            {
                Add(@interface, instance);
            }
        }

        public static void AddInterfacesAndSelf(object instance)
        {
            foreach (var @interface in instance.GetType().GetInterfaces().Append(instance.GetType()))
            {
                Add(@interface, instance);
            }
        }

        #endregion

        #region Auto Add

        public static void Add(Type type)
        {
            Add(type, Instantiate(type));
        }

        public static void AddInterfaces(Type type)
        {
            AddInterfaces(Instantiate(type));
        }

        public static void AddInterfacesAndSelf(Type type)
        {
            AddInterfacesAndSelf(Instantiate(type));
        }

        #endregion

        #region Get

        public static object Get(Type type)
        {
            return GetOrDefault(type) ?? throw new($"No instance found for {type.Name}");
        }

        public static object GetOrDefault(Type type)
        {
            return GetCache(type).SingleOrDefault();
        }

        public static object[] GetAll(Type type)
        {
            return GetCache(type).ToArray();
        }

        #endregion

        #region Instantiate

        public static object Instantiate(Type type)
        {
            if (type.IsAbstract) throw new($"Cannot instantiate abstract type {type.Name}");
            if (type.ContainsGenericParameters) throw new($"Cannot instantiate generic type {type.Name}");
            var ctor = type.GetConstructors().SingleOrDefault()
                ?? throw new($"No constructor found for {type.Name}");
            return ctor.Invoke(ResolveParameters(ctor.GetParameters(), $"instantiating {type.Name}"));
        }

        public static object Invoke(object obj, string methodName)
        {
            var method = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new($"Method {methodName} not found on {obj.GetType().Name}");
            return method.Invoke(obj, ResolveParameters(method.GetParameters(), $"invoking {methodName} on {obj.GetType().Name}"));
        }

        #endregion

        #region Generic

        public static void Add<T>() => Add(typeof(T));

        public static void AddInterfaces<T>() => AddInterfaces(typeof(T));

        public static void AddInterfacesAndSelf<T>() => AddInterfacesAndSelf(typeof(T));

        public static T Get<T>() => (T)Get(typeof(T));

        public static T GetOrDefault<T>() => (T)GetOrDefault(typeof(T));

        public static T[] GetAll<T>() => GetAll(typeof(T)).Cast<T>().ToArray();

        public static T Instantiate<T>() => (T)Instantiate(typeof(T));

        #endregion

        #region Private

        private static readonly Dictionary<Type, HashSet<object>> Cache = new();

        private static HashSet<object> GetCache(Type type)
        {
            if (!Cache.ContainsKey(type)) Cache.Add(type, new());
            return Cache[type];
        }

        private static object[] ResolveParameters(ParameterInfo[] parameters, string context)
        {
            return parameters.Select(parameter =>
            {
                var parameterType = parameter.ParameterType;
                switch (parameterType)
                {
                    case { IsGenericType: true } when typeof(IEnumerable<>).IsAssignableFrom(parameterType.GetGenericTypeDefinition()):
                    {
                        return GetArray(parameterType.GetGenericArguments()[0]);
                    }
                    case { IsArray: true }:
                    {
                        return GetArray(parameterType.GetElementType());
                    }
                    default:
                    {
                        var instance = GetOrDefault(parameterType);
                        if (instance is null && !parameter.HasDefaultValue) throw new($"Cannot resolve {parameterType.Name} for {parameter.Name} while {context}");
                        return instance ?? parameter.DefaultValue;
                    }
                }
            }).ToArray();

            static Array GetArray(Type type)
            {
                var instances = GetAll(type);
                var array     = Array.CreateInstance(type, instances.Length);
                instances.CopyTo(array, 0);
                return array;
            }
        }

        #endregion
    }
}