using Newtonsoft.Json;
using PeterHan.PLib.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExtendedBuildingWidth
{
    public class ExtendableConfigSettings
    {
        public string ConfigName { get; set; }
        public int MinWidth { get; set; }
        public int MaxWidth { get; set; }
        public float AnimStretchModifier { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    public class ModSettings : SingletonOptions<ModSettings>
    {
        [JsonProperty]
        public string ExtendableConfigSettings { get; set; }

        public ModSettings()
        {
            ExtendableConfigSettings = JsonConvert.SerializeObject(GenerateExtendableConfigSettingsList());
        }

        public static List<ExtendableConfigSettings> GetExtendableConfigSettingsList()
        {
            var result = JsonConvert.DeserializeObject<List<ExtendableConfigSettings>>(Instance.ExtendableConfigSettings);
            return result;
        }

        private static List<ExtendableConfigSettings> GenerateExtendableConfigSettingsList()
        {
            List<Type> list = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types = assemblies[i].GetTypes();
                if (types != null)
                {
                    list.AddRange(types);
                }
            }

            var typeFromHandle = typeof(IBuildingConfig);
            var extendableTypes = new List<Type>(
                list.Where(type =>
                    typeFromHandle.IsAssignableFrom(type)
                    && !type.IsAbstract
                    && !type.IsInterface
                    && type.Name.Contains("ConduitBridge")
                    ).ToList()
                );

            var result = new List<ExtendableConfigSettings>(
                extendableTypes.Select(x =>
                    new ExtendableConfigSettings()
                    {
                        ConfigName = x.FullName,
                        MinWidth = 2,
                        MaxWidth = 16,
                        AnimStretchModifier = 1.12f
                    }
                    )
                );
            return result;
        }

    }
}