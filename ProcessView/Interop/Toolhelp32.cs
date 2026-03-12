using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using ProcessView.Domain;

namespace ProcessView.Interop;

internal static class Toolhelp32
{
    private const uint TH32CS_SNAPPROCESS = 0x00000002;

    public static IReadOnlyList<ProcessInfo> TakeSnapshot()
    {
        var processes = new List<ProcessInfo>();

        IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1))
        {
            ThrowLastWin32Error("CreateToolhelp32Snapshot failed");
        }

        try
        {
            var entry = new PROCESSENTRY32();
            entry.dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>();

            if (!Process32First(snapshot, ref entry))
            {
                int error = Marshal.GetLastWin32Error();
                if (error == 0 || error == 18) // ERROR_NO_MORE_FILES
                {
                    return processes;
                }

                ThrowLastWin32Error("Process32First failed", error);
            }

            do
            {
                var name = entry.szExeFile ?? string.Empty;

                processes.Add(new ProcessInfo
                {
                    ProcessId = unchecked((int)entry.th32ProcessID),
                    ParentProcessId = unchecked((int)entry.th32ParentProcessID),
                    Name = name
                });

                entry.dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>();
            }
            while (Process32Next(snapshot, ref entry));

            int last = Marshal.GetLastWin32Error();
            if (last != 0 && last != 18) // ERROR_NO_MORE_FILES
            {
                ThrowLastWin32Error("Process32Next failed", last);
            }
        }
        finally
        {
            CloseHandle(snapshot);
        }

        return processes;
    }

    private static void ThrowLastWin32Error(string message, int? error = null)
    {
        int code = error ?? Marshal.GetLastWin32Error();
        throw new Win32Exception(code, $"{message}. Win32 error {code}");
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }
}

