#if UNIT_ZENJECT
#nullable enable
namespace UniT.DI
{
    using System;
    using System.Linq;
    using UnityEngine.Scripting;
    using Zenject;

    public sealed class ZenjectContainer : IDependencyContainer
    {
        private readonly DiContainer container;

        [Preserve]
        public ZenjectContainer(DiContainer container) => this.container = container;

        object IDependencyContainer.Resolve(Type type) => this.container.Resolve(type);

        T IDependencyContainer.Resolve<T>() => this.container.Resolve<T>();

        object[] IDependencyContainer.ResolveAll(Type type) => this.container.ResolveAll(type).Cast<object>().ToArray();

        T[] IDependencyContainer.ResolveAll<T>() => this.container.ResolveAll<T>().ToArray();

        object IDependencyContainer.Instantiate(Type type) => this.container.Instantiate(type);

        T IDependencyContainer.Instantiate<T>() => this.container.Instantiate<T>();
    }
}
#endif