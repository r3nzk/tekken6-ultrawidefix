using System.Diagnostics;
using System.Runtime.InteropServices;

namespace tekken6ultrawidefix
{
    public class memoryScannerWin : IMemoryScanner
    {
        //dll imports for memory manipulation
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out memoryInfo lpBuffer, uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct memoryInfo{
            public IntPtr baseAddress;
            public IntPtr allocationBase;
            public uint allocationProtect;
            public IntPtr regionSize;
            public uint state;
            public uint protect;
            public uint type;
        }

        public IntPtr FindPattern(Process process, byte[] pattern){
            long currentAddress = 0;
            long maxAddress = 0x7FFFFFFF0000;
            
            byte[] buffer = new byte[1024 * 5120];

            while(currentAddress < maxAddress){
                memoryInfo memInfo;
                int queryResult = VirtualQueryEx(process.Handle, (IntPtr)currentAddress, out memInfo, (uint)Marshal.SizeOf(typeof(memoryInfo)));
               
                if(queryResult == 0)
                    break;

                //check if the region is committed
                bool isCommitted = memInfo.state == 0x1000;
                bool isGuardModifier = (memInfo.protect & 0x100) != 0;
                bool isNoAccess = (memInfo.protect & 0x01) != 0;
                bool isReadable = !isGuardModifier && !isNoAccess;

                long regionSize = (long)memInfo.regionSize;

                if(isCommitted && isReadable && regionSize > 0){
                    int bytesToRead = (int)Math.Min(buffer.Length, regionSize);
                    int bytesRead = 0;

                    bool success = ReadProcessMemory(process.Handle, (IntPtr)currentAddress, buffer, bytesToRead, out bytesRead);

                    if(success && bytesRead >= pattern.Length){
                        for(int i = 0; i <= bytesRead - pattern.Length; i += 4){
                            if(buffer[i] != pattern[0] || buffer[i + 1] != pattern[1])
                                continue;

                            bool match = true;
                            //scan
                            for(int j = 2; j < pattern.Length; j++){
                                if(buffer[i + j] != pattern[j]){
                                    match = false;
                                    break;
                                }
                            }

                            if(match){
                                return IntPtr.Add((IntPtr)currentAddress, i);
                            }
                        }
                    }
                }
                
                //jump
                long nextAddress = (long)memInfo.baseAddress + regionSize;
               
                if(nextAddress <= currentAddress)
                    break; 

                currentAddress = nextAddress;
            }

            return IntPtr.Zero;
        }

        public void WriteBytes(Process proc, IntPtr address, byte[] data){
            int bytesWritten = 0;
            WriteProcessMemory(proc.Handle, address, data, data.Length, out bytesWritten);
        }
    }
}
