using OmenMon.Library.Hw;

namespace OmenMon.Library.Features
{
    public class FanControl
    {
        // Fan Offsets for Omen 16 (Generic)
        // These are only used if Ring0 is active
        private const ushort Fan1ManualOffset = 0x2E; // CPU Fan
        private const ushort Fan2ManualOffset = 0x2F; // GPU Fan

        public void SetFanSpeed(int percentage)
        {
            // Clamp percentage
            if (percentage > 100) percentage = 100;
            if (percentage < 0) percentage = 0;

            if (EcDriver.IsDriverAvailable)
            {
                // -- PRECISION MODE (Ring0) --
                // We can set exact RPM percentages
                byte hexValue = (byte)percentage;
                
                // Set Manual Mode first (usually 0x2D on this generation)
                EcDriver.Write(0x2D, 0x01); 
                
                // Write speeds
                EcDriver.Write(Fan1ManualOffset, hexValue);
                EcDriver.Write(Fan2ManualOffset, hexValue);
                
                Log.Info($"Fan Control: Set custom curve to {percentage}% via EC.");
            }
            else
            {
                // -- SAFE MODE (WMI) --
                // We cannot set exact % via WMI, only toggle Max/Auto
                // Threshold: If user asks for >80%, trigger Max mode. Otherwise Auto.
                bool triggerMax = percentage > 80;
                
                Log.Warning($"Fan Control: Ring0 unavailable. Fallback logic applied (Request: {percentage}%).");
                
                bool success = BiosWmi.SetFanMode(triggerMax);
                if (!success)
                {
                    Log.Error("Fan Control: Failed to set fan mode via WMI fallback.");
                }
            }
        }

        public void ResetToAuto()
        {
            if (EcDriver.IsDriverAvailable)
            {
                // Disable Manual Mode
                EcDriver.Write(0x2D, 0x00);
            }
            else
            {
                BiosWmi.SetFanMode(false); // Set to Auto
            }
        }
    }
}
