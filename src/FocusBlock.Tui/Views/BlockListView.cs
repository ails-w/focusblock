using System.Collections.ObjectModel;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace FocusBlock.Tui.Views;

public class BlockListView : View
{
    private readonly ListView _listView;
    private readonly ObservableCollection<string> _apps = new();

    public IReadOnlyList<string> Apps => _apps;

    public BlockListView()
    {
        _listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        _listView.SetSource(_apps);
        Add(_listView);
    }

    public void ShowApps(IEnumerable<string> apps)
    {
        _apps.Clear();
        foreach (string app in apps)
        {
            _apps.Add(app);
        }
    }
}