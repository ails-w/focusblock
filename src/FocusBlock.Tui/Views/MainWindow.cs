using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace FocusBlock.Tui.Views;

public class MainWindow : Window
{
    public MenuBar MenuBar { get; }
    public StatusBar StatusBar { get; }
    public StatusView StatusView { get; }
    public BlockListView BlockListView { get; }
    public AddBlockView AddBlockView { get; }
    public View Content { get; private set; }

    public MainWindow()
    {
        Title = "FocusBlock";
        StatusView = new StatusView();
        BlockListView = new BlockListView();
        AddBlockView = new AddBlockView();

        MenuBar = new MenuBar([
            new MenuBarItem("_Block", [
                new MenuItem { Title = "_New Block", Action = () => ShowView(AddBlockView) },
                new MenuItem { Title = "_List", Action = () => ShowView(BlockListView) },
            ]),
            new MenuBarItem("_View", [
                new MenuItem { Title = "_Status", Action = () => ShowView(StatusView) },
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

        Content = StatusView;
        Content.X = 0;
        Content.Y = 1;
        Content.Width = Dim.Fill();
        Content.Height = Dim.Fill(1);

        Add(MenuBar, Content, StatusBar);
    }

    public void ShowView(View view)
    {
        if (view == Content)
        {
            return;
        }

        Remove(Content);
        Content = view;
        Add(view);
    }
}