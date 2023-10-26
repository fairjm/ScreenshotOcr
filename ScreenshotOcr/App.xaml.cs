namespace ScreenshotOcr;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var windows = base.CreateWindow(activationState);
        windows.Width = 800;
        windows.Height = 800;
        return windows;
    }
}