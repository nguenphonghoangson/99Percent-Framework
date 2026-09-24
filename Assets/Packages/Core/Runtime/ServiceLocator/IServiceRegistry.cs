namespace NinetyNine.Core
{
    public interface IServiceResolver
    {
        /// <summary>Throws when <typeparamref name="T" /> is missing — for hard dependencies.</summary>
        T Require<T>() where T : class;

        /// <summary>For optional dependencies (e.g. IAP is not installed on a build without a store).</summary>
        bool TryGet<T>(out T service) where T : class;

        bool Has<T>() where T : class;
    }

    public interface IServiceRegistry : IServiceResolver
    {
        void Register<T>(T service) where T : class;
    }
}
