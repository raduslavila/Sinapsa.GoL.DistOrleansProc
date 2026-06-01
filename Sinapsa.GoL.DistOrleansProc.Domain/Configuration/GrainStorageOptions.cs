namespace Sinapsa.GoL.DistOrleansProc.Domain.Configuration
{
    public sealed class GrainStorageOptions
    {
        public const string SectionName = nameof(GrainStorageOptions);

        public Dictionary<string, GrainStorageProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ChunkMemory"] = new GrainStorageProviderOptions(),
            ["PubSubStore"] = new GrainStorageProviderOptions()
        };
    }

    public sealed class GrainStorageProviderOptions
    {
        public GrainStorageProviderKind ProviderKind { get; set; } = GrainStorageProviderKind.Memory;
        public string? RedisConnectionString { get; set; }
    }

    public enum GrainStorageProviderKind
    {
        Memory = 0,
        Redis = 1
    }
}
