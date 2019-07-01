using Newtonsoft.Json;

namespace adifpush
{
    class AdifLineModel
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "adif";

        [JsonProperty("string")]
        public string @String { get; set; }
    }
}
