using FocusBlock.Tui.Views;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;

namespace FocusBlock.Tui;

public class FocusBlockApp
{
    private readonly IApplication _app;
    public MainWindow MainWindow { get; }

    public FocusBlockApp(IApplication app)
    {
        _app = app;
        MainWindow = new MainWindow();
    }

    public void Run()
    {
        _app.Init(DriverRegistry.Names.DOTNET);
        _app.Run(MainWindow);
    }
}