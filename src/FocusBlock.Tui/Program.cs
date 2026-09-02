using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Drivers;

using IApplication app = Application.Create();
app.Init(DriverRegistry.Names.DOTNET);

Window window = new() { Title = "FocusBlock" };
window.Add(new Label
{
  Text = "Hello, FocusBlock!",
  X = Pos.Center(),
  Y = Pos.Center()
});

app.Run(window);
window.Dispose();


