using FocusBlock.Tui.Services;
using FocusBlock.Tui.Views;

using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Views;

namespace FocusBlock.Tui;

public class FocusBlockApp
{
    private const int OkButtonIndex = 0;
    private const string DefaultSocketPath = "/run/focusblock/focusblock.sock";

    private readonly IApplication _app;
    private readonly EarlyStopService _earlyStop;
    public MainWindow MainWindow { get; }

    public FocusBlockApp(IApplication app)
    {
        _app = app;
        _earlyStop = new EarlyStopService(
            new IpcClient(DefaultSocketPath),
            new ChallengeSystem(),
            new TerminalEarlyStopPrompt(app));
        MainWindow = new MainWindow(OnEarlyStopAsync);
    }

    public void Run()
    {
        _app.Init(DriverRegistry.Names.DOTNET);
        _app.Run(MainWindow);
    }

    /// <summary>
    /// Runs the early-stop flow from the menu: ask for the app name, then let the daemon decide.
    /// </summary>
    /// <remarks>
    /// Invoked fire-and-forget from the menu action. The name dialog runs modally on the UI thread;
    /// the IPC round-trip is awaited off it, and the outcome message is marshalled back with
    /// <see cref="IApplication.Invoke(System.Action)"/> so it never blocks or touches the UI from a
    /// background thread.
    /// </remarks>
    private async Task OnEarlyStopAsync()
    {
        var dialog = new AppNameDialog();
        _app.Run(dialog);

        string? appName = dialog.Result == OkButtonIndex ? dialog.AppName : null;
        if (appName is null)
        {
            return;
        }

        bool granted = await _earlyStop.RequestEarlyStopAsync(appName);
        string message = granted
            ? $"Early stop granted for {appName}."
            : $"Early stop denied for {appName}.";

        _app.Invoke(() => MessageBox.Query(_app, "Early stop", message, "OK"));
    }
}