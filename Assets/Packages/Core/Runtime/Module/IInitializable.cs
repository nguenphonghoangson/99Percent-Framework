namespace NinetyNine.Core
{
    /// <summary>
    ///     Called once after every module and feature is installed, in registration order — so a service may
    ///     read any service installed before it. Disposal (<see cref="System.IDisposable" />) runs in reverse.
    /// </summary>
    public interface IInitializable
    {
        void Initialize();
    }
}
