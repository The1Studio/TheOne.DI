#nullable enable
namespace UniT.DI
{
    using System;

    public interface IDependencyContainer
    {
        public object Resolve(Type type);

        public T Resolve<T>();

        public object[] ResolveAll(Type type);

        public T[] ResolveAll<T>();

        public object Instantiate(Type type);

        public T Instantiate<T>();
    }
}