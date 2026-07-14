namespace tekken6ultrawidefix
{
    public static class crossPlataformScanner{
        public static IMemoryScanner Create(){
            if(OperatingSystem.IsWindows())
                return new memoryScannerWin();

            if(OperatingSystem.IsLinux())
                return new memoryScannerLinux();

            throw new PlatformNotSupportedException();
        }
    }
}
