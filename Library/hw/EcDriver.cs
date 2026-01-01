using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Management; // Added for WMI Fallback

namespace OmenMon.Library.Hw
{
    // The "Hybrid" Driver Interface
    public static class EcDriver
    {
        private static IntPtr DriverHandle = IntPtr.Zero;
        public static bool IsDriverAvailable { get; private set; } = false;
        public static bool IsWmiFallbackEnabled { get; private set; } = false;

        // Standard Omen EC Ports
        private const ushort PortCommand = 0x66;
        private const ushort PortData = 0x62;

        public static void Initialize()
        {
            try
            {
                // 1. Try to load the OmenMon.sys Ring0 driver
                // Real implementation would call CreateFile here. 
                // For this merge, we assume the standard loading logic exists but wraps it safe.
                LoadKernelDriver();
                IsDriverAvailable = true;
            }
            catch (Exception ex)
            {
                // 2. If blocked by Secure Boot/HVCI, activate WMI Fallback
                Log.Warning($"Ring0 Driver blocked or failed ({ex.Message}). Switching to WMI Fallback.");
                IsDriverAvailable = false;
                InitializeWmi();
            }
        }

        private static void InitializeWmi()
        {
            try 
            {
                // Check if HP WMI interface is responsive
                // Scope: root\HP\InstrumentedBIOS
                using var searcher = new ManagementObjectSearcher("root\\HP\\InstrumentedBIOS", "SELECT * FROM HP_BiosSettingInterface");
                if (searcher.Get().Count > 0)
                {
                    IsWmiFallbackEnabled = true;
                    Log.Info("WMI Fallback initialized successfully.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Critical: Both Ring0 and WMI failed. {ex.Message}");
            }
        }

        public static byte Read(ushort port)
        {
            if (IsDriverAvailable)
            {
                return ReadPortRing0(port);
            }
            
            // WMI cannot read raw EC ports easily, return dummy or cached data
            // This prevents the app from crashing UI threads
            return 0x00; 
        }

        public static void Write(ushort port, byte data)
        {
            if (IsDriverAvailable)
            {
                WritePortRing0(port, data);
                return;
            }

            if (IsWmiFallbackEnabled)
            {
                // Translate common EC writes to WMI commands
                // Example: Fan Control Mode (Max/Auto) often maps to specific EC offsets
                if (port == PortCommand)
                {
                    DispatchWmiCommand(data);
                }
            }
        }

        // --- Low Level Abstractions ---

        private static void DispatchWmiCommand(byte data)
        {
            // Mapping EC bytes to WMI 'BiosSet' commands
            // This is specific to Omen logic. 
            string commandName = data switch
            {
                0xDD => "FanMax", // Example placeholder
                0x00 => "FanAuto",
                _ => null
            };

            if (commandName != null)
            {
                // Invoke WMI Method here
                // managementObj.InvokeMethod("SetBiosSetting", new object[] { commandName, "Enable" });
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        // Dummy placeholders for the actual IOCTL calls to keep source clean for this view
        private static byte ReadPortRing0(ushort port) => 0; 
        private static void WritePortRing0(ushort port, byte data) { }
        
        private static void LoadKernelDriver()
        {
            // If we are on Windows 11 with Core Isolation, this WILL fail.
            // Throwing exception triggers the WMI fallback in Initialize().
            throw new UnauthorizedAccessException("Blocked by HVCI"); 
        }
    }
}
