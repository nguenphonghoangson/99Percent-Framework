namespace NinetyNine.Core
{
    /// <summary>
    ///     Format-agnostic (de)serialisation. JsonUtility is the default implementation (Persistence); Newtonsoft,
    ///     MemoryPack or Protobuf plug in behind the same contract without touching any module.
    /// </summary>
    public interface ISerializer
    {
        string Serialize<T>(T value);

        T Deserialize<T>(string data);
    }
}
