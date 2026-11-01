using Velopack;
using WomierDriver.App.Services;

namespace WomierDriver.App;

/// <summary>
/// Entry point. Velopack's install, update and uninstall hooks have to run before WPF starts; for those the
/// process exits inside <see cref="VelopackApp.Run"/>.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main()
    {
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => AutostartManager.PointAtThisExecutable())
            .OnBeforeUninstallFastCallback(_ => AutostartManager.RemoveIfThisExecutable())
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
