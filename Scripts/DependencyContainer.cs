namespace UniT.DI
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;

    public sealed class DependencyContainer
    {
        #region Constructor

        public DependencyContainer()
        {
            this.Add(this);
        }

        #endregion

        #region Manual Add

        public void Add(Type type, object instance)
        {
            this.GetCache(type).Add(instance);
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
            foreach (var @interface in instance.GetType().GetInterfaces().Append(instance.GetType()))
            {
                this.Add(@interface, instance);
            }
        }

        #endregion

        #region Auto Add

        public void Add(Type type)
        {
            this.Add(type, this.Instantiate(type));
        }

        public void AddInterfaces(Type type)
        {
            this.AddInterfaces(this.Instantiate(type));
        }

        public void AddInterfacesAndSelf(Type type)
        {
            this.AddInterfacesAndSelf(this.Instantiate(type));
        }

        #endregion

        #region Get

        public object Get(Type type)
        {
            return this.GetOrDefault(type) ?? throw new Exception($"No instance found for {type.Name}");
        }

        public object GetOrDefault(Type type)
        {
            return this.GetCache(type).SingleOrDefault();
        }

        public object[] GetAll(Type type)
        {
            return this.GetCache(type).ToArray();
        }

        #endregion

        #region Instantiate

        public object Instantiate(Type type)
        {
            if (type.IsAbstract) throw new Exception($"Cannot instantiate abstract type {type.Name}");
            if (type.ContainsGenericParameters) throw new Exception($"Cannot instantiate generic type {type.Name}");
            var ctor = type.GetConstructors().SingleOrDefault()
                ?? throw new Exception($"No constructor found for {type.Name}");
            return ctor.Invoke(this.ResolveParameters(ctor.GetParameters(), $"instantiating {type.Name}"));
        }

        public object Invoke(object obj, string methodName)
        {
            var method = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new Exception($"Method {methodName} not found on {obj.GetType().Name}");
            return method.Invoke(obj, this.ResolveParameters(method.GetParameters(), $"invoking {methodName} on {obj.GetType().Name}"));
        }

        #endregion

        #region Resolve

        private readonly ReadOnlyCollection<Type> supportedInterfaces = new ReadOnlyCollection<Type>(new[] { typeof(IEnumerable<>), typeof(ICollection<>), typeof(IList<>), typeof(IReadOnlyCollection<>), typeof(IReadOnlyList<>) });

        private readonly ReadOnlyCollection<Type> supportedConcreteTypes = new ReadOnlyCollection<Type>(new[] { typeof(Collection<>), typeof(List<>), typeof(ReadOnlyCollection<>) });

        private object[] ResolveParameters(ParameterInfo[] parameters, string context)
        {
            return parameters.Select(parameter =>
            {
                var parameterType = parameter.ParameterType;
                switch (parameterType)
                {
                    case { IsGenericType: true, IsInterface: true } when this.supportedInterfaces.Contains(parameterType.GetGenericTypeDefinition()):
                    {
                        return GetArray(parameterType.GetGenericArguments()[0]);
                    }
                    case { IsGenericType: true } when this.supportedConcreteTypes.Contains(parameterType.GetGenericTypeDefinition()):
                    {
                        return Activator.CreateInstance(parameterType, GetArray(parameterType.GetGenericArguments()[0]));
                    }
                    case { IsArray: true }:
                    {
                        return GetArray(parameterType.GetElementType());
                    }
                    default:
                    {
                        var instance = this.GetOrDefault(parameterType);
                        if (instance is null && !parameter.HasDefaultValue) throw new Exception($"Cannot resolve {parameterType.Name} for {parameter.Name} while {context}");
                        return instance ?? parameter.DefaultValue;
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

        public void Add<T>(T instance) => this.Add(typeof(T), instance);

        public void Add<T>() => this.Add(typeof(T));

        public void AddInterfaces<T>() => this.AddInterfaces(typeof(T));

        public void AddInterfacesAndSelf<T>() => this.AddInterfacesAndSelf(typeof(T));

        public T Get<T>() => (T)this.Get(typeof(T));

        public T GetOrDefault<T>() => (T)this.GetOrDefault(typeof(T));

        public T[] GetAll<T>() => this.GetAll(typeof(T)).Cast<T>().ToArray();

        public T Instantiate<T>() => (T)this.Instantiate(typeof(T));

        #endregion

        #region Cache

        private readonly Dictionary<Type, HashSet<object>> cache = new Dictionary<Type, HashSet<object>>();

        private HashSet<object> GetCache(Type type)
        {
            if (!this.cache.ContainsKey(type)) this.cache.Add(type, new HashSet<object>());
            return this.cache[type];
        }

        #endregion
    }
}