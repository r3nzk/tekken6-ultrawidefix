using System.Diagnostics;

namespace tekken6ultrawidefix{
    public interface IMemoryScanner{
        IntPtr FindPattern(Process process, byte[] pattern);
        void WriteBytes(Process proc, IntPtr address, byte[] data);
    }
}
