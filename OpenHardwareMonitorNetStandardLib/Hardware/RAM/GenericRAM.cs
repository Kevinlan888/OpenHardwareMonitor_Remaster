/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware.RAM
{
    internal class GenericRAM : Hardware
    {
        private Sensor loadSensor;
        private Sensor appLoadSensor;
        private Sensor usedMemory;
        private Sensor appUsedMemory;
        private Sensor availableMemory;

        public GenericRAM(string name, ISettings settings)
          : base(name, new Identifier("ram"), settings)
        {
            loadSensor = new Sensor("Memory", 0, SensorType.Load, this, settings);
            ActivateSensor(loadSensor);

            appLoadSensor = new Sensor("App used memory", 0, SensorType.Load, this, settings);
            ActivateSensor(appLoadSensor);

            appUsedMemory = new Sensor("App used memory", 0, SensorType.SmallData, this, settings);
            ActivateSensor(appUsedMemory);

            usedMemory = new Sensor("Used Memory", 0, SensorType.Data, this,
              settings);
            ActivateSensor(usedMemory);

            availableMemory = new Sensor("Available Memory", 1, SensorType.Data, this,
              settings);
            ActivateSensor(availableMemory);
        }

        public override HardwareType HardwareType
        {
            get
            {
                return HardwareType.RAM;
            }
        }

        public override void Update()
        {
            NativeMethods.MemoryStatusEx status = new NativeMethods.MemoryStatusEx();
            NativeMethods.PROCESS_MEMORY_COUNTERS processMemCnt = new NativeMethods.PROCESS_MEMORY_COUNTERS();
            processMemCnt.cb = (uint)Marshal.SizeOf(typeof(NativeMethods.PROCESS_MEMORY_COUNTERS));

            status.Length = checked((uint)Marshal.SizeOf(
                typeof(NativeMethods.MemoryStatusEx)));

            if (!NativeMethods.GlobalMemoryStatusEx(ref status))
                return;

            if (!NativeMethods.GetProcessMemoryInfo(System.Diagnostics.Process.GetCurrentProcess().Handle, out processMemCnt, processMemCnt.cb))
                return;

            loadSensor.Value = 100.0f -
              (100.0f * status.AvailablePhysicalMemory) /
              status.TotalPhysicalMemory;

            appLoadSensor.Value = (100.0f * processMemCnt.WorkingSetSize) /
              status.TotalPhysicalMemory;

            appUsedMemory.Value = processMemCnt.WorkingSetSize / 1024 / 1024;

            usedMemory.Value = (float)(status.TotalPhysicalMemory
              - status.AvailablePhysicalMemory) / (1024 * 1024 * 1024);

            availableMemory.Value = (float)status.AvailablePhysicalMemory /
              (1024 * 1024 * 1024);
        }

        private class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct MemoryStatusEx
            {
                public uint Length;
                public uint MemoryLoad;
                public ulong TotalPhysicalMemory;
                public ulong AvailablePhysicalMemory;
                public ulong TotalPageFile;
                public ulong AvailPageFile;
                public ulong TotalVirtual;
                public ulong AvailVirtual;
                public ulong AvailExtendedVirtual;
            }

            [StructLayout(LayoutKind.Sequential, Size = 72)]
            public struct PROCESS_MEMORY_COUNTERS
            {
                public uint cb;
                public uint PageFaultCount;
                public UInt64 PeakWorkingSetSize;
                public UInt64 WorkingSetSize;
                public UInt64 QuotaPeakPagedPoolUsage;
                public UInt64 QuotaPagedPoolUsage;
                public UInt64 QuotaPeakNonPagedPoolUsage;
                public UInt64 QuotaNonPagedPoolUsage;
                public UInt64 PagefileUsage;
                public UInt64 PeakPagefileUsage;
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GlobalMemoryStatusEx(
              ref NativeMethods.MemoryStatusEx buffer);

            [DllImport("psapi.dll", SetLastError = true)]
            internal static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS counters, uint size);
        }
    }
}
