using Eto.Forms;

namespace tekken6ultrawidefix
{
    internal static class Program
    {
        [STAThread]
        static void Main(){
            //detect OS (linx memoryscan still TODO)
            new Application(Eto.Platform.Detect).Run(new MainForm());
        }
    }
}
