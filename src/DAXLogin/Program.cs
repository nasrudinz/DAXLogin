using System;
using System.Threading;
using Velopack;

namespace DAXLogin
{
    public static class Program
    {
        public const string ActivateEventName = "DAXLogin.Activate";

        [STAThread]
        public static void Main(string[] args)
        {
            // Must run first: handles installer/updater hooks and exits when invoked by them.
            VelopackApp.Build().Run();

            using var mutex = new Mutex(true, "DAXLogin.SingleInstance", out bool isFirst);
            if (!isFirst)
            {
                // Hand over to the running instance (it opens the default profile or shows its window).
                try { EventWaitHandle.OpenExisting(ActivateEventName).Set(); }
                catch (WaitHandleCannotBeOpenedException) { }
                return;
            }

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
