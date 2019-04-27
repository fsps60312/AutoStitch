using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using OpenCL.Net.Extensions;
using CL = OpenCL.Net;

namespace AutoStitch
{
    public static class MyCL
    {
        static class Console
        {
            public static void WriteLine(object s) { System.Diagnostics.Trace.WriteLine(s); }
        }
        public static CL.Context context { get { Setup();return _context; } }
        public static CL.Device device { get { Setup();return _device; } }
        private static CL.Context _context;
        private static CL.Device _device;
        private static bool setuped = false;
        private static void Setup()
        {
            if (!setuped)
            {
                setuped = true;
                CL.ErrorCode error;
                CL.Platform[] platforms = CL.Cl.GetPlatformIDs(out error);
                List<CL.Device> devicesList = new List<CL.Device>();

                CheckErr(error, "Cl.GetPlatformIDs");

                foreach (CL.Platform platform in platforms)
                {
                    string platformName = CL.Cl.GetPlatformInfo(platform, CL.PlatformInfo.Name, out error).ToString();
                    Console.WriteLine("Platform: " + platformName);
                    CheckErr(error, "Cl.GetPlatformInfo");
                    //We will be looking only for GPU devices
                    foreach (CL.Device device in CL.Cl.GetDeviceIDs(platform, CL.DeviceType.Gpu, out error))
                    {
                        CheckErr(error, "Cl.GetDeviceIDs");
                        Console.WriteLine("Device: " + device.ToString());
                        devicesList.Add(device);
                    }
                }

                if (devicesList.Count <= 0)
                {
                    Console.WriteLine("No devices found.");
                    return;
                }
                Console.WriteLine($"found {devicesList.Count} device(s)");
                foreach (var device in devicesList)
                {
                    var buffer = CL.Cl.GetDeviceInfo(device, CL.DeviceInfo.Name, out error);
                    CheckErr(error, "Cl.GetDeviceInfo");
                    Console.WriteLine($"\t{buffer}");
                }

                _device = devicesList[0];

                if (CL.Cl.GetDeviceInfo(_device, CL.DeviceInfo.ImageSupport,
                          out error).CastTo<CL.Bool>() == CL.Bool.False)
                {
                    Console.WriteLine("No image support.");
                    return;
                }
                _context = CL.Cl.CreateContext(null, 1, new[] { _device }, ContextNotify, IntPtr.Zero, out error);    //Second parameter is amount of devices
                CheckErr(error, "Cl.CreateContext");
            }
        }
        public static void CheckErr(CL.ErrorCode err, string name)
        {
            //Console.WriteLine($"{name}...");
            if (err != CL.ErrorCode.Success)
            {
                Console.WriteLine("ERROR: " + name + " (" + err.ToString() + ")");
            }
        }
        private static void ContextNotify(string errInfo, byte[] data, IntPtr cb, IntPtr userData)
        {
            Console.WriteLine("OpenCL Notification: " + errInfo);
        }
        public unsafe static void memcpy(ref int[] source, ref byte[] target)
        {
            System.Diagnostics.Trace.Assert(Marshal.SizeOf(typeof(int)) * source.Length == target.Length);
            fixed (int* ptr = source)
            {
                Marshal.Copy((IntPtr)ptr, target, 0, target.Length);
            }
        }
        public unsafe static void memcpy(ref double[,] source, ref byte[] target)
        {
            System.Diagnostics.Trace.Assert(Marshal.SizeOf(typeof(double)) * source.Length == target.Length);
            fixed (double* ptr = source)
            {
                Marshal.Copy((IntPtr)ptr, target, 0, target.Length);
            }
        }
        public unsafe static void memcpy(ref double[] source, ref byte[] target)
        {
            System.Diagnostics.Trace.Assert(Marshal.SizeOf(typeof(double)) * source.Length == target.Length);
            fixed (double* ptr = source)
            {
                Marshal.Copy((IntPtr)ptr, target, 0, target.Length);
            }
        }
        public unsafe static void memcpy(ref byte[] source, ref double[,] target)
        {
            System.Diagnostics.Trace.Assert(source.Length == Marshal.SizeOf(typeof(double)) * target.Length);
            fixed (double* ptr = target)
            {
                Marshal.Copy(source, 0, (IntPtr)ptr, source.Length);
            }
        }
        public unsafe static void memcpy(ref byte[] source, ref int[] target)
        {
            System.Diagnostics.Trace.Assert(source.Length == Marshal.SizeOf(typeof(int)) * target.Length);
            fixed (int* ptr = target)
            {
                Marshal.Copy(source, 0, (IntPtr)ptr, source.Length);
            }
        }
    }
}
