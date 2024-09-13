#if UNIT_VCONTAINER
#nullable enable
namespace UniT.DI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using VContainer;
    using PreserveAttribute = UnityEngine.Scripting.PreserveAttribute;

    public sealed class VContainerWrapper : IDependencyContainer
    {
        private readonly IObjectResolver container;

        [Preserve]
        public VContainerWrapper(IObjectResolver container) => this.container = container;

        bool IDependencyContainer.TryResolve(Type type, [MaybeNullWhen(false)] out object instance) => this.container.TryResolve(type, out instance);

        bool IDependencyContainer.TryResolve<T>([MaybeNullWhen(false)] out T instance) => this.container.TryResolve(out instance);

        object IDependencyContainer.Resolve(Type type) => this.container.Resolve(type);

        T IDependencyContainer.Resolve<T>() => this.container.Resolve<T>();

        object[] IDependencyContainer.ResolveAll(Type type) => ((IEnumerable)this.container.Resolve(typeof(IEnumerable<>).MakeGenericType(type))).Cast<object>().ToArray();

        T[] IDependencyContainer.ResolveAll<T>() => this.container.Resolve<IEnumerable<T>>().ToArray();

        object IDependencyContainer.Instantiate(Type type) => this.container.Instantiate(type);

        T IDependencyContainer.Instantiate<T>() => this.container.Instantiate<T>();
    }
}

namespace VContainer
{
    using System;
    using System.Collections.Generic;
    using VContainer.Internal;

    public static class VContainerExtensions
    {
        public static RegistrationBuilder AsInterfacesAndSelf(this RegistrationBuilder registrationBuilder)
        {
            return registrationBuilder.AsImplementedInterfaces().AsSelf();
        }

        public static void AutoResolve(this IContainerBuilder builder, Type type)
        {
            builder.RegisterBuildCallback(container => container.Resolve(type));
        }

        public static void AutoResolve<T>(this IContainerBuilder builder)
        {
            builder.AutoResolve(typeof(T));
        }

        public static object Instantiate(this IObjectResolver container, Type type, IReadOnlyList<IInjectParameter>? parameters = null)
        {
            return InjectorCache.GetOrBuild(type).CreateInstance(container, parameters);
        }

        public static T Instantiate<T>(this IObjectResolver container, IReadOnlyList<IInjectParameter>? parameters = null)
        {
            return (T)container.Instantiate(typeof(T), parameters);
        }
    }
}
#endif