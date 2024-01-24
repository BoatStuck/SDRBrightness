using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ScreenBrightnessSetter
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
    public class MonitorInfo
    {
        public int cbSize = Marshal.SizeOf(typeof(MonitorInfo));
        public Rect rcMonitor = new Rect();
        public Rect rcWork = new Rect();
        public int dwFlags = 0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] szDevice = new char[32];
    }

    class Program
	{
		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		static extern IntPtr LoadLibrary(string lpFileName);
		[DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		static extern IntPtr GetProcAddress(IntPtr hModule, int address);

        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);
        
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hmonitor, [In, Out] MonitorInfo info);

        private delegate void DwmpSDRToHDRBoostPtr(IntPtr monitor, double brightness);


        delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        /// <summary>
        /// The struct that contains the display information
        /// </summary>
        public class DisplayInfo
        {
            public string Availability { get; set; }
            public string ScreenHeight { get; set; }
            public string ScreenWidth { get; set; }
            public Rect MonitorArea { get; set; }
            public Rect WorkArea { get; set; }
            public IntPtr MonitorHandle { get; set; }

        }

        /// <summary>
        /// Collection of display information
        /// </summary>
        public class DisplayInfoCollection : List<DisplayInfo>
        {
        }

        /// <summary>
        /// Returns the number of Displays using the Win32 functions
        /// </summary>
        /// <returns>collection of Display Info</returns>
        public static DisplayInfoCollection GetDisplays()
        {
            DisplayInfoCollection col = new DisplayInfoCollection();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
                {
                    MonitorInfo mi = new MonitorInfo();
                    mi.cbSize = Marshal.SizeOf(mi);
                    bool success = GetMonitorInfo(hMonitor, mi);
                    if (success)
                    {
                        DisplayInfo di = new DisplayInfo();
                        di.ScreenWidth = (mi.rcMonitor.right - mi.rcMonitor.left).ToString();
                        di.ScreenHeight = (mi.rcMonitor.bottom - mi.rcMonitor.top).ToString();
                        di.MonitorArea = mi.rcMonitor;
                        di.WorkArea = mi.rcWork;
                        di.Availability = mi.dwFlags.ToString();
                        di.MonitorHandle = hMonitor;
                        col.Add(di);
                    }
                    return true;
                }, IntPtr.Zero);
            return col;
        }



        static void Main(string[] args)
		{
			double brightness = 1.0;
			double maxBrightness = 6.0;
			double minBrightness = 1.0;

            double argBrightness;

            if (args.Length > 0)
			{
				if (!double.TryParse(args[0], out argBrightness))
				{
					// .. error with input
					Console.WriteLine("Cannot parse, exiting: " + args[0]);
					return;
				}
			} else
			{
                Console.WriteLine("Enter desired brightness from 1.0 to 6.0:");
                if (!double.TryParse(Console.ReadLine(), out argBrightness))
                {
                    // .. error with input
                    Console.WriteLine("Cannot parse input, exiting");
                    return;
                }

            }

            Console.WriteLine("Parsed " + argBrightness);
            if (argBrightness < minBrightness)
                argBrightness = minBrightness;
            else if (argBrightness > maxBrightness)
                argBrightness = maxBrightness;

            brightness = argBrightness;
            Console.WriteLine("Setting brightness " + brightness);

            var hmodule_dwmapi = LoadLibrary("dwmapi.dll");
			DwmpSDRToHDRBoostPtr changeBrightness = Marshal.GetDelegateForFunctionPointer<DwmpSDRToHDRBoostPtr>(GetProcAddress(hmodule_dwmapi, 171));

            DisplayInfoCollection monitors = GetDisplays();
            foreach(DisplayInfo monitor in monitors)
            {
                Console.WriteLine("Changing brightness for monitor handle: " + monitor.MonitorHandle + " to: " + brightness);
                changeBrightness(monitor.MonitorHandle, brightness);
            }
        }
    }
}