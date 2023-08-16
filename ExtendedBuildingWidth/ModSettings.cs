using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace ExtendedBuildingWidth
{
    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    public class ModSettings : SingletonOptions<ModSettings>
    {
        [JsonProperty]
        [Option("Dynamic width range: Min", "Determines how big could be the dynamically created buildings", Format = "F0")]
        [Limit(2, 3)]
        public int MinWidth
        { get; set; }

        [JsonProperty]
        [Option("Dynamic width range: Max", "Determines how big could be the dynamically created buildings", Format = "F0")]
        [Limit(3, 15)]
        public int MaxWidth
        { get; set; }

        public ModSettings()
        {
            MinWidth = 2;
            MaxWidth = 10;
        }
    }
}