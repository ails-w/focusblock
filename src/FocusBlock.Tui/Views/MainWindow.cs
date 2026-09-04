using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace FocusBlock.Tui.Views;

public class MainWindow : Window
{
    public MenuBar MenuBar { get; }
    public StatusBar StatusBar { get; }

    public MainWindow()
    {
        Title = "FocusBlock";
        MenuBar = new MenuBar([
            new MenuBarItem("_Block", [
                new MenuItem { Title = "_New Block", Action = () => { } },
                new MenuItem { Title = "_List", Action = () => { } },
            ]),
            new MenuBarItem("_View", [
                new MenuItem { Title = "_Status", Action = () => { } },
                new MenuItem { Title = "_Metrics", Action = () => { } },
            ]),
            new MenuBarItem("_Settings", [
                new MenuItem { Title = "_Config", Action = () => { } },
            ]),
            new MenuBarItem("_Help", [
                new MenuItem { Title = "_About", Action = () => { } },
            ]),
        ]);
        StatusBar = new StatusBar([
            new Shortcut { Title = "F1 _Help", Key = Key.F1 },
            new Shortcut { Title = "Ctrl+Q _Quit", Key = Key.Q.WithCtrl },
        ]);
        Add(MenuBar, StatusBar);
    }
}