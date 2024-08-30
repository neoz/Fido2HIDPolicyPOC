using System;
using System.Threading;
using System.Management;
using System.Security.Principal;
using System.Runtime.InteropServices;
using HidLibrary;
using System.Collections.Generic;
using System.Linq;

namespace POC
{
    
    internal class Program
    {
        private const short FIDO_USAGE_PAGE = unchecked((short)0xF1D0);
        private const short FIDO_USAGE = unchecked((short)0x01);
        
        // Lock the screen using PInvoke
        [DllImport("user32.dll")]
        static extern bool LockWorkStation();
        
        
        public static List<HidDevice> GetAllFIDODevices()
        {
            return HidDevices.Enumerate()
                .Where(d => d.Capabilities?.UsagePage == FIDO_USAGE_PAGE && d.Capabilities?.Usage == FIDO_USAGE)
                .OrderBy(d => d.DevicePath)
                .ToList();
        }
        
        public static List<string> GetAllFIDODevicePaths()
        {
            List<string> devices = new List<string>();
            devices.AddRange(GetAllFIDODevices().Select(d => d.DevicePath).ToList());
            return devices;
        }
        
        public static HidDevice Find(string devicePath)
        {
            return GetAllFIDODevices().FirstOrDefault(d => d.DevicePath.Equals(devicePath, StringComparison.InvariantCultureIgnoreCase));
        }
        public static bool IsDeviceConnected(string devicePath)
        {
            if (Find(devicePath) == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        
        public static void Main(string[] args)
        {
            var fido2DeviceList = GetAllFIDODevicePaths();
            Console.WriteLine("FIDO2 devices found: " + fido2DeviceList.Count);
            foreach (var dev in fido2DeviceList)
            {
                Console.WriteLine(dev);
            }
            Console.WriteLine("Monitoring for device removal...");
            while (true)
            {
                // Check if any of the devices have been removed
                // If a device has been removed, lock the workstation
                var removedDevices = new List<string>();
                foreach (var dev in fido2DeviceList)
                {
                    if (!IsDeviceConnected(dev))
                    {
                        Console.WriteLine("Device removed: " + dev);
                        removedDevices.Add(dev);
                    }
                }
                
                if (removedDevices.Count > 0)
                {
                    // remove the removed devices from the list
                    foreach (var dev in removedDevices)
                    {
                        fido2DeviceList.Remove(dev);
                    }
                    // Lock the workstation
                    LockWorkStation();
                }
                
                var count = fido2DeviceList.Count;
                // Wait for a short interval before checking again
                Thread.Sleep(1000);
                // Re-enumerate the devices and add any new devices to the list
                fido2DeviceList.AddRange(GetAllFIDODevicePaths().Where(d => !fido2DeviceList.Contains(d)));
                if (fido2DeviceList.Count > count)
                {
                    Console.WriteLine("New device found: " + fido2DeviceList.Last());
                }
            }
        }
    }
}

