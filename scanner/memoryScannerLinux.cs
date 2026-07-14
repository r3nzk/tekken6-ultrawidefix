using System.Diagnostics;

namespace tekken6ultrawidefix
{
    public class memoryScannerLinux : IMemoryScanner
    {
        public IntPtr FindPattern(Process process, byte[] pattern){
            //TODO
            return IntPtr.Zero;
        }

        public void WriteBytes(Process proc, IntPtr address, byte[] data){
            //TODO
        }
    }
}
