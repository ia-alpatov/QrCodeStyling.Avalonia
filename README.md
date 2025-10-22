## About

QrCodeStyling.Avalonia is a simple library, written in C#.NET, which enables you to create beautiful QR codes.
<br />
<br />
[![NuGet](https://img.shields.io/nuget/v/QrCodeStyling.Avalonia.svg)](https://www.nuget.org/packages/QrCodeStyling.Avalonia) [![downloads](https://img.shields.io/nuget/dt/QrCodeStyling.Avalonia)](https://www.nuget.org/packages/QrCodeStyling.Avalonia)  ![Size](https://img.shields.io/github/repo-size/ia-alpatov/QrCodeStyling.Avalonia.svg) 
***
## Example

![Example gif](./readme_media/QRCodeStyling.gif)

```xaml
<avalonia:QrCode
                ImagePaddingModules="0.2"
                Image="{Binding Image}"
                CornerDots="{Binding QrCornerDotsStyle}"
                Dot="{Binding QrDotStyle}"
                Width="{Binding QrCodeSize}"
                Height="{Binding QrCodeSize}"
                Padding="{Binding QrCodePadding}"
                Data="{Binding QrCodeString}"
                CornerRadius="{Binding CornerRadius}"
                ErrorCorrection="{Binding QrCodeEccLevel}">

                <avalonia:QrCode.Foreground>
                    <LinearGradientBrush>
                        <GradientStop Offset="0"
                                      Color="{Binding QrCodeForegroundColor1}" />
                        <GradientStop Offset="1"
                                      Color="{Binding QrCodeForegroundColor2}" />
                    </LinearGradientBrush>
                </avalonia:QrCode.Foreground>

                <avalonia:QrCode.Background>
                    <LinearGradientBrush>
                        <GradientStop Offset="0"
                                      Color="{Binding QrCodeBackgroundColor1}" />
                        <GradientStop Offset="1"
                                      Color="{Binding QrCodeBackgroundColor2}" />
                    </LinearGradientBrush>
                </avalonia:QrCode.Background>
            </avalonia:QrCode>
```

```csharp

public partial class MainWindowViewModel : ViewModelBase
{
    private const string Chars = "qwertyuiopasdfghjklzxcvbnm";
    private string? _qrCodeString;

    private double _qrCodeSize = 250;

    private Thickness _qrCodePadding = new(10);

    private CornerRadius _CornerRadius = new(12);

    private Color _qrCodeForegroundColor1;
    private Color _qrCodeForegroundColor2;

    private Color _qrCodeBackgroundColor1;
    private Color _qrCodeBackgroundColor2;

    private QrCode.EccLevel _qrCodeEccLevel;

    private QrCode.DotType _qrDotStyle;

    private QrCode.CornerDotsType _qrCornerDotsStyle;

    private Bitmap _image = null;

    public string? QrCodeString
    {
        get => _qrCodeString;
        set => this.SetProperty(ref _qrCodeString, value);
    }

    public Thickness QrCodePadding
    {
        get => _qrCodePadding;
        set => this.SetProperty(ref _qrCodePadding, value);
    }

    public double QrCodeSize
    {
        get => _qrCodeSize;
        set => this.SetProperty(ref _qrCodeSize, value);
    }

    public CornerRadius CornerRadius
    {
        get => _CornerRadius;
        set => this.SetProperty(ref _CornerRadius, value);
    }

    public QrCode.EccLevel QrCodeEccLevel
    {
        get => _qrCodeEccLevel;
        set => this.SetProperty(ref _qrCodeEccLevel, value);
    }

    public QrCode.DotType QrDotStyle
    {
        get => _qrDotStyle;
        set => this.SetProperty(ref _qrDotStyle, value);
    }

    public QrCode.CornerDotsType QrCornerDotsStyle
    {
        get => _qrCornerDotsStyle;
        set => this.SetProperty(ref _qrCornerDotsStyle, value);
    }

    public Bitmap Image
    {
        get => _image;
        set => this.SetProperty(ref _image, value);
    }

    public Color QrCodeForegroundColor1
    {
        get => _qrCodeForegroundColor1;
        set => this.SetProperty(ref _qrCodeForegroundColor1, value);
    }

    public Color QrCodeForegroundColor2
    {
        get => _qrCodeForegroundColor2;
        set => this.SetProperty(ref _qrCodeForegroundColor2, value);
    }

    public Color QrCodeBackgroundColor1
    {
        get => _qrCodeBackgroundColor1;
        set => this.SetProperty(ref _qrCodeBackgroundColor1, value);
    }

    public Color QrCodeBackgroundColor2
    {
        get => _qrCodeForegroundColor2;
        set => this.SetProperty(ref _qrCodeBackgroundColor2, value);
    }

    public ObservableCollection<QrCode.EccLevel> Levels { get; }
    public ObservableCollection<QrCode.DotType> QrDotStyles { get; }
    public ObservableCollection<QrCode.CornerDotsType> QrCornerDotsStyles { get; }


    public MainWindowViewModel()
    {
        ResetQrCode();

        Levels = new ObservableCollection<QrCode.EccLevel>(Enum.GetValues<QrCode.EccLevel>());
        QrDotStyles = new ObservableCollection<QrCode.DotType>(Enum.GetValues<QrCode.DotType>());
        QrCornerDotsStyles = new ObservableCollection<QrCode.CornerDotsType>(Enum.GetValues<QrCode.CornerDotsType>());
    }

    public void UpdateQrCode(string text)
    {
        if (string.IsNullOrEmpty(text))
            text = "You didn't put anything here?";
        QrCodeString = text;
    }

    public void RandomizeData()
    {
        UpdateQrCode(string.Join("", Enumerable.Range(0, 150).Select(_ => Chars[Random.Shared.Next(0, Chars.Length)])));
    }

    public void RandomizeColors()
    {
        var newColors = new byte[12];
        Random.Shared.NextBytes(newColors);

        QrCodeForegroundColor1 = Color.FromRgb(newColors[0], newColors[1], newColors[2]);
        QrCodeForegroundColor2 = Color.FromRgb(newColors[3], newColors[4], newColors[5]);

        QrCodeBackgroundColor1 = Color.FromRgb(newColors[6], newColors[7], newColors[8]);
        QrCodeBackgroundColor2 = Color.FromRgb(newColors[9], newColors[10], newColors[11]);

        var cuurentCode = QrCodeString;
        QrCodeString = string.Empty;

        UpdateQrCode(cuurentCode);
    }

    public void ResetQrCode()
    {
        QrCodeEccLevel = QrCode.EccLevel.Medium;

        QrCodeString =
            "I'm a very long text that you might find somewhere as a link or something else.  It's rendered with smooth edges and gradients for the foreground and background";

        QrCodeForegroundColor1 = Colors.Navy;
        QrCodeForegroundColor2 = Colors.DarkRed;
        QrCodeBackgroundColor1 = Colors.White;
        QrCodeBackgroundColor2 = Colors.White;
    }

    public async void UpdateImage()
    {
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
            
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Image File",
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll },
                AllowMultiple = false
            });

            if (files.Count >= 1)
            {
                Image?.Dispose();
                Image = new Bitmap(await files[0].OpenReadAsync());
            }
        }
    }

```

***

## Legal information and credits

Gma.QrCodeNet.Encoding is a project by [Ron.Liang](https://www.nuget.org/packages/Gma.QrCodeNet.Encoding/).

QrCodeStyling.Avalonia is a project by [Ivan Alpatov](mailto:ivan@alpatov.family). It's licensed under the [MIT license](https://github.com/ia-alpatov/QrCodeStyling.Avalonia/blob/master/LICENSE).

