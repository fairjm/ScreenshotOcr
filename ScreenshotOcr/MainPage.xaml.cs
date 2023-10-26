using System.Diagnostics;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using ABI.Windows.UI.Notifications;
using ScreenshotOcr.Helpers;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using System.Drawing;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.Media;
using Microsoft.Maui.Handlers;

namespace ScreenshotOcr;

public partial class MainPage : ContentPage
{

    private OcrEngine _engine;
    private Timer _timer;

    public MainPage()
    {
        InitializeComponent();

#if DEBUG
        foreach (var lang in OcrEngine.AvailableRecognizerLanguages)
        {
            Debug.WriteLine(lang.LanguageTag);
        }
#endif
        Language l = new Language("zh-Hans-CN");
        //_engine = OcrEngine.TryCreateFromLanguage(l);
        _engine = OcrEngine.TryCreateFromUserProfileLanguages();

        var monitorInfo = MonitorEnumerationHelper.GetMonitors().First();

        int monitorWidth = (int)monitorInfo.ScreenSize.X;
        int monitorHeight = (int)monitorInfo.ScreenSize.Y;

        XEntry.Text = "0";
        YEntry.Text = "0";
        WidthEntry.Text = "" + monitorWidth;
        HeightEntry.Text = "" + monitorHeight;
    }

    void TimerCallback(object state)
    {
        try
        {
            Debug.WriteLine("callback run");
            ScreenShotAndOcr().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private byte[] _array;

    private async Task ScreenShotAndOcr()
    {
        var monitorInfos = MonitorEnumerationHelper.GetMonitors();
        var captureScreen = DesktopCaptureHelper.CaptureScreen();

        try
        {
            var monitorInfo = MonitorEnumerationHelper.GetMonitors().First();

            int monitorWidth = (int)monitorInfo.ScreenSize.X;
            int monitorHeight = (int)monitorInfo.ScreenSize.Y;

            var xEntryText = XEntry.Text ?? "0";
            var yEntryText = YEntry.Text ?? "0";
            var widthText = WidthEntry.Text ?? "" + monitorWidth;
            var heightText = HeightEntry.Text ?? "" + monitorHeight;

            int x = Int32.Parse(xEntryText);
            int y = Int32.Parse(yEntryText);
            int width = Int32.Parse(widthText);
            int height = Int32.Parse(heightText);

            width = Math.Min(width, monitorWidth - x);
            height = Math.Min(height, monitorHeight - y);

            if (width > 0 && height > 0)
            {
                var tmp = captureScreen.Clone(new System.Drawing.Rectangle(x, y, width, height), captureScreen.PixelFormat);
                captureScreen.Dispose();
                captureScreen = tmp;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

        using var memoryStream = new MemoryStream();
        captureScreen.Save(memoryStream, ImageFormat.Bmp);
        memoryStream.Position = 0;
        _array = memoryStream.ToArray(); 
        captureScreen.Dispose();

        await this.Dispatcher.DispatchAsync(async () =>
        {
            using var msCopy = new MemoryStream(_array);
            ScreenImage.Source = ImageSource.FromStream(() => new MemoryStream(_array));

            using var randomAccessStream = msCopy.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            var recognizeResult = await _engine.RecognizeAsync(bitmap);
            var recognizeResultText = recognizeResult.Text;
            Label.Text = recognizeResultText;

            var replace = recognizeResultText.Replace(" ", "");
            var checkText = CheckEntry.Text;
            if (checkText is not (null or ""))
            {
                if (replace.ToLower().Contains(checkText))
                {
                    await Toast.Make($"{checkText} 触发啦").Show();
                    IEnumerable<Locale> locales = await TextToSpeech.Default.GetLocalesAsync();
                    var localeList = locales.ToList();
                    var locale = localeList.ToList().FirstOrDefault(e => e.Language.ToLower().Contains("zh")) ??
                                 localeList.First();
                    var options = new SpeechOptions()
                    {
                        Pitch = 1.5f,   // 0.0 - 2.0
                        Volume = 0.75f, // 0.0 - 1.0
                        Locale = locale
                    };
                    await TextToSpeech.Default.SpeakAsync(checkText + "触发啦", options);
                }
            }
        });
    }

    private void OnStartBtnClicked(object sender, EventArgs e)
    {
        _timer = new Timer(TimerCallback, null, 0, 5000);
        StartBtn.IsEnabled = false;
        StopBtn.IsEnabled = true;
    }

    private void OnStopBtnClicked(object sender, EventArgs e)
    {
        _timer.Dispose();
        _timer = null;
        StartBtn.IsEnabled = true;
        StopBtn.IsEnabled = false;
    }
}