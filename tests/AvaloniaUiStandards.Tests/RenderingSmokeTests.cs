using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Xunit;

namespace AvaloniaUiStandards.Tests;

public class RenderingSmokeTests
{
    [AvaloniaFact]
    public void FluentButton_RendersToPng()
    {
        var window = new Window
        {
            Width = 200,
            Height = 100,
            Content = new Button
            {
                Content = "Button",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        window.Show();

        using var frame = window.CaptureRenderedFrame()!;

        var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "button.png");
        using (var stream = File.Create(path))
            frame.Save(stream, new PngBitmapEncoderOptions());

        Assert.True(HasMoreThanOneColour(frame), "Rendered frame is blank.");
    }

    private static bool HasMoreThanOneColour(WriteableBitmap frame)
    {
        using var fb = frame.Lock();
        var bytes = new byte[fb.RowBytes * fb.Size.Height];
        Marshal.Copy(fb.Address, bytes, 0, bytes.Length);
        var first = BitConverter.ToInt32(bytes, 0);
        for (var y = 0; y < fb.Size.Height; y++)
            for (var x = 0; x < fb.Size.Width; x++)
                if (BitConverter.ToInt32(bytes, y * fb.RowBytes + x * 4) != first)
                    return true;
        return false;
    }
}
