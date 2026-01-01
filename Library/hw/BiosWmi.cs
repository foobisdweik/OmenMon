using System;
using System.Management;
using System.Linq;

namespace OmenMon.Library.Hw
{
    // Handles safe, Ring3 interactions with the HP BIOS via WMI
    public static class BiosWmi
    {
        private const string WmiNamespace = "root\\HP\\InstrumentedBIOS";
        private const string WmiClass = "HP_BiosSettingInterface";

        // Converts high-level commands to HP BIOS calls
        // Returns true if successful
        public static bool SetPerformanceMode(string mode)
        {
            // Map generic modes to HP's specific internal strings
            string hpModeValue = mode.ToLower() switch
            {
                "performance" => "Performance",
                "default" => "Standard",
                "comfort" => "Cool",
                _ => "Standard"
            };

            return InvokeBiosSetting("System Performance Mode", hpModeValue);
        }

        public static bool SetFanMode(bool max)
        {
            // "Fan Speed Mode" is the BIOS setting key for many 2023/2024 Omens
            string value = max ? "Max" : "Auto";
            return InvokeBiosSetting("Fan Speed Mode", value);
        }

        private static bool InvokeBiosSetting(string name, string value)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {WmiClass}");
                using var items = searcher.Get();

                foreach (ManagementObject obj in items)
                {
                    // 1. Prepare parameters for the 'SetBiosSetting' method
                    var methodParams = obj.GetMethodParameters("SetBiosSetting");
                    methodParams["Name"] = name;
                    methodParams["Value"] = value;
                    methodParams["Password"] = "<utf-16/>"; // HP standard empty password encoding

                    // 2. Execute
                    var result = obj.InvokeMethod("SetBiosSetting", methodParams);
                    
                    // 3. Check Return Code (0 = Success)
                    uint returnCode = (uint)result["Return"];
                    if (returnCode == 0 || returnCode == 3010) // 3010 = Reboot Required (sometimes benign)
                    {
                        Log.Info($"WMI Success: Set {name} to {value}");
                        return true;
                    }
                    else
                    {
                        Log.Error($"WMI Error: Set {name} returned code {returnCode}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"WMI Critical Failure: {ex.Message}");
            }
            return false;
        }

        // Utility: Check if this machine even HAS the WMI interface
        public static bool IsSupported()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {WmiClass}");
                return searcher.Get().Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
