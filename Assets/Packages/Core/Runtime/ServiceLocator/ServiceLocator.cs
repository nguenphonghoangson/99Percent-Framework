namespace NinetyNine.Core
{
    /// <summary>
    ///     Global access point for MonoBehaviour views, which Unity constructs and so cannot receive services
    ///     through a constructor. Set by the composition root after <see cref="ModuleHost.Build" />; null before
    ///     boot and after shutdown. Plain C# classes take their dependencies as constructor arguments instead.
    /// </summary>
    public static class ServiceLocator
    {
        public static IServiceResolver Current { get; set; }
    }
}
