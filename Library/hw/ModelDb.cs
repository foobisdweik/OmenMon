using System.Collections.Generic;

namespace OmenMon.Library.Hw
{
    public class ModelDb
    {
        public static ModelProfile? GetProfile(string boardId, string productName)
        {
            // 1. Try Exact Board ID Match
            if (Profiles.TryGetValue(boardId, out var profile))
            {
                return profile;
            }

            // 2. Loose Matching for your Omen 16 (16-b1085nr)
            // This allows the app to load even if the BoardID isn't in the hardcoded dictionary
            if (productName.Contains("OMEN") && productName.Contains("16"))
            {
                return new ModelProfile
                {
                    Name = "Generic Omen 16 (Auto-Detected)",
                    FanMax = 5500, // Safe defaults for 16-inch models
                    HasFourZoneRgb = true,
                    EcBase = 0x62
                };
            }

            return null;
        }

        // Known Board IDs
        private static readonly Dictionary<string, ModelProfile> Profiles = new()
        {
            // Existing Omen 15/17 models...
            { "8600", new ModelProfile { Name = "Omen 15 (2020)", FanMax = 5000 } },
            
            // ADDED: Omen 16 Specifics
            // 89C3 is a common board ID for Omen 16s. 
            { "89C3", new ModelProfile 
                { 
                    Name = "HP Omen 16-b Series", 
                    FanMax = 5800, // 16-b usually goes higher
                    HasFourZoneRgb = true,
                    KeyMap = KeyMaps.StandardOmen2022
                } 
            }
        };
    }

    public class ModelProfile
    {
        public string Name { get; set; } = "Unknown";
        public int FanMax { get; set; } = 4500;
        public bool HasFourZoneRgb { get; set; } = false;
        public int EcBase { get; set; } = 0x62;
        public KeyMap KeyMap { get; set; } = KeyMaps.Legacy;
    }
    
    public enum KeyMap { Legacy, StandardOmen2022 }
}
