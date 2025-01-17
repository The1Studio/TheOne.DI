﻿#if THEONE_ZENJECT
#nullable enable
namespace TheOne.DI
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using UnityEngine.Scripting;
    using Zenject;

    public sealed class ZenjectWrapper : IDependencyContainer
    {
        private readonly DiContainer container;

        [Preserve]
        public ZenjectWrapper(DiContainer container) => this.container = container;

        bool IDependencyContainer.TryResolve(Type type, [MaybeNullWhen(false)] out object instance)
        {
            if (this.container.TryResolve(type) is { } obj)
            {
                instance = obj;
                return true;
            }
            instance = null;
            return false;
        }

        bool IDependencyContainer.TryResolve<T>([MaybeNullWhen(false)] out T instance)
        {
            if (this.container.TryResolve(typeof(T)) is { } obj)
            {
                instance = (T)obj;
                return true;
            }
            instance = default;
            return false;
        }

        object IDependencyContainer.Resolve(Type type) => this.container.Resolve(type);

        T IDependencyContainer.Resolve<T>() => this.container.Resolve<T>();

        object[] IDependencyContainer.ResolveAll(Type type) => this.container.ResolveAll(type).Cast<object>().ToArray();

        T[] IDependencyContainer.ResolveAll<T>() => this.container.ResolveAll<T>().ToArray();

        object IDependencyContainer.Instantiate(Type type, params object?[] @params) => this.container.Instantiate(type, @params);

        T IDependencyContainer.Instantiate<T>(params object?[] @params) => this.container.Instantiate<T>(@params);
    }

    public static class ZenjectExtensions
    {
        public static void BindDependencyContainer(this DiContainer container)
        {
            if (container.HasBinding<IDependencyContainer>()) return;
            container.BindInterfacesTo<ZenjectWrapper>().AsSingle();
        }
    }
}
#endif