#nullable enable
namespace UniT.DI
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using UniT.Extensions;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class DependencyContainer : IDependencyContainer
    {
        #region Constructor

        private readonly Dictionary<Type, HashSet<object>> cache = new Dictionary<Type, HashSet<object>>();

        public DependencyContainer()
        {
            this.AddInterfaces(this);
        }

        #endregion

        #region IDependencyContainer

        bool IDependencyContainer.TryResolve(Type type, [MaybeNullWhen(false)] out object instance) => this.TryGet(type, out instance);

        bool IDependencyContainer.TryResolve<T>([MaybeNullWhen(false)] out T instance) => this.TryGet(out instance);

        object IDependencyContainer.Resolve(Type type) => this.Get(type);

        T IDependencyContainer.Resolve<T>() => this.Get<T>();

        object[] IDependencyContainer.ResolveAll(Type type) => this.GetAll(type);

        T[] IDependencyContainer.ResolveAll<T>() => this.GetAll<T>();

        object IDependencyContainer.Instantiate(Type type, params object?[] @params) => this.Instantiate(type, @params);

        T IDependencyContainer.Instantiate<T>(params object?[] @params) => this.Instantiate<T>(@params);

        #endregion

        #region Manual Add

        public void Add(Type type, object instance)
        {
            this.cache.GetOrAdd(type).Add(instance);
        }

        public void Add(object instance)
        {
            this.Add(instance.GetType(), instance);
        }

        public void AddInterfaces(object instance)
        {
            foreach (var @interface in instance.GetType().GetInterfaces())
            {
                this.Add(@interface, instance);
            }
        }

        public void AddInterfacesAndSelf(object instance)
        {
            foreach (var @interface in instance.GetType().GetInterfaces().Prepend(instance.GetType()))
            {
                this.Add(@interface, instance);
            }
        }

        #endregion

        #region Auto Add

        public void Add(Type type, params object?[] @params)
        {
            this.Add(type, this.Instantiate(type, @params));
        }

        public void AddInterfaces(Type type, params object?[] @params)
        {
            this.AddInterfaces(this.Instantiate(type, @params));
        }

        public void AddInterfacesAndSelf(Type type, params object?[] @params)
        {
            this.AddInterfacesAndSelf(this.Instantiate(type, @params));
        }

        #endregion

        #region Get

        public bool Contains(Type type)
        {
            return this.cache.ContainsKey(type);
        }

        public object Get(Type type)
        {
            return this.TryGet(type, out var instance) ? instance : throw new Exception($"No instance found for {type.Name}");
        }

        public bool TryGet(Type type, [MaybeNullWhen(false)] out object instance)
        {
            if (this.cache.GetOrDefault(type)?.SingleOrDefault() is { } obj)
            {
                instance = obj;
                return true;
            }
            instance = null;
            return false;
        }

        public object[] GetAll(Type type)
        {
            return this.cache.GetOrDefault(type)?.ToArray() ?? Array.Empty<object>();
        }

        #endregion

        #region Instantiate

        public object Instantiate(Type type, params object?[] @params)
        {
            if (type.IsAbstract) throw new InvalidOperationException($"Cannot instantiate abstract type {type.Name}");
            if (type.ContainsGenericParameters) throw new InvalidOperationException($"Cannot instantiate generic type {type.Name}");
            var constructor = type.GetSingleConstructor();
            return constructor.Invoke(this.ResolveParameters(constructor.GetParameters(), @params, $"instantiating {type.Name}"));
        }

        public object Invoke(object obj, MethodInfo method, params object[] @params)
        {
            return method.Invoke(obj, this.ResolveParameters(method.GetParameters(), @params, $"invoking {method.Name} on {obj.GetType().Name}"));
        }

        public object Invoke(object obj, string methodName, params object[] @params)
        {
            var method = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new Exception($"Method {methodName} not found on {obj.GetType().Name}");
            return this.Invoke(obj, method, @params);
        }

        #endregion

        #region Resolve

        private static readonly HashSet<Type> SupportedInterfaces = new HashSet<Type>() { typeof(IEnumerable<>), typeof(ICollection<>), typeof(IList<>), typeof(IReadOnlyCollection<>), typeof(IReadOnlyList<>) };

        private static readonly HashSet<Type> SupportedConcreteTypes = new HashSet<Type>() { typeof(Collection<>), typeof(List<>), typeof(ReadOnlyCollection<>) };

        private object[] ResolveParameters(ParameterInfo[] parameters, object?[] @params, string context)
        {
            return parameters.Select(parameter =>
            {
                var parameterType = parameter.ParameterType;
                if (@params.FirstOrDefault(param => parameterType.IsInstanceOfType(param)) is { } param) return param;
                switch (parameterType)
                {
                    case { IsGenericType: true, IsInterface: true } when SupportedInterfaces.Contains(parameterType.GetGenericTypeDefinition()):
                    {
                        return GetArray(parameterType.GetGenericArguments()[0]);
                    }
                    case { IsGenericType: true } when SupportedConcreteTypes.Contains(parameterType.GetGenericTypeDefinition()):
                    {
                        return Activator.CreateInstance(parameterType, GetArray(parameterType.GetGenericArguments()[0]));
                    }
                    case { IsArray: true }:
                    {
                        return GetArray(parameterType.GetElementType()!);
                    }
                    default:
                    {
                        if (this.TryGet(parameterType, out var instance)) return instance;
                        if (parameter.HasDefaultValue) return parameter.DefaultValue;
                        throw new Exception($"Cannot resolve {parameterType.Name} for {parameter.Name} while {context}");
                    }
                }
            }).ToArray();

            Array GetArray(Type type)
            {
                var instances = this.GetAll(type);
                var array     = Array.CreateInstance(type, instances.Length);
                instances.CopyTo(array, 0);
                return array;
            }
        }

        #endregion

        #region Generic

        public void Add<T>(T instance) where T : notnull => this.Add(typeof(T), instance);

        public void Add<T>(params object?[] @params) => this.Add(typeof(T), @params);

        public void AddInterfaces<T>(params object?[] @params) => this.AddInterfaces(typeof(T), @params);

        public void AddInterfacesAndSelf<T>(params object?[] @params) => this.AddInterfacesAndSelf(typeof(T), @params);

        public bool Contains<T>() => this.Contains(typeof(T));

        public T Get<T>() => (T)this.Get(typeof(T));

        public bool TryGet<T>([MaybeNullWhen(false)] out T instance)
        {
            if (this.TryGet(typeof(T), out var obj))
            {
                instance = (T)obj;
                return true;
            }
            instance = default;
            return false;
        }

        public T[] GetAll<T>() => this.GetAll(typeof(T)).Cast<T>().ToArray();

        public T Instantiate<T>(params object?[] @params) => (T)this.Instantiate(typeof(T), @params);

        #endregion

        #region Add From

        public void AddFromResource<T>(string path) where T : Object => this.Add(LoadResource<T>(path));

        public void AddInterfacesFromResource<T>(string path) where T : Object => this.AddInterfaces(LoadResource<T>(path));

        public void AddInterfacesAndSelfFromResource<T>(string path) where T : Object => this.AddInterfacesAndSelf(LoadResource<T>(path));

        public void AddFromComponentInNewPrefabResource<T>(string path) where T : Component => this.Add(InstantiatePrefabResource<T>(path));

        public void AddInterfacesFromComponentInNewPrefabResource<T>(string path) where T : Component => this.AddInterfaces(InstantiatePrefabResource<T>(path));

        public void AddInterfacesAndSelfFromComponentInNewPrefabResource<T>(string path) where T : Component => this.AddInterfacesAndSelf(InstantiatePrefabResource<T>(path));

        public void AddFromComponentInNewPrefab<T>(T prefab) where T : Component => this.Add(InstantiatePrefab(prefab));

        public void AddInterfacesFromComponentInNewPrefab<T>(T prefab) where T : Component => this.AddInterfaces(InstantiatePrefab(prefab));

        public void AddInterfacesAndSelfFromComponentInNewPrefab<T>(T prefab) where T : Component => this.AddInterfacesAndSelf(InstantiatePrefab(prefab));

        public void AddFromComponentInHierarchy<T>() where T : Object => this.Add(Object.FindObjectOfType<T>(true));

        public void AddInterfacesFromComponentInHierarchy<T>() where T : Object => this.AddInterfaces(Object.FindObjectOfType<T>(true));

        public void AddInterfacesAndSelfFromComponentInHierarchy<T>() where T : Object => this.AddInterfacesAndSelf(Object.FindObjectOfType<T>(true));

        public void AddAllFromComponentInHierarchy<T>() where T : Object => Object.FindObjectsOfType<T>(true).ForEach(this.Add);

        public void AddAllInterfacesFromComponentInHierarchy<T>() where T : Object => Object.FindObjectsOfType<T>(true).ForEach(this.AddInterfaces);

        public void AddAllInterfacesAndSelfFromComponentInHierarchy<T>() where T : Object => Object.FindObjectsOfType<T>(true).ForEach(this.AddInterfacesAndSelf);

        private static T LoadResource<T>(string path) where T : Object => Resources.Load<T>(path) ?? throw new ArgumentOutOfRangeException($"Failed to load {path}");

        private static T InstantiatePrefab<T>(T prefab) where T : Component => Object.Instantiate(prefab).DontDestroyOnLoad();

        private static T InstantiatePrefabResource<T>(string path) where T : Component => InstantiatePrefab(LoadResource<GameObject>(path).GetComponentOrThrow<T>());

        #endregion
    }
}