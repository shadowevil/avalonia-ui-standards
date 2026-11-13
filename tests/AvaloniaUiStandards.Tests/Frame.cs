using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AvaloniaUiStandards.Tests;

/// <summary>A captured window frame with pixel access (32-bit RGBA or BGRA).</summary>
internal sealed class Frame : IDisposable
{
    private readonly byte[] _bytes;
    private readonly int _stride;
    private readonly bool _bgra;

    private Frame(WriteableBitmap bitmap)
    {
        Bitmap = bitmap;
        using var fb = bitmap.Lock();
        Width = fb.Size.Width;
        Height = fb.Size.Height;
        _stride = fb.RowBytes;
        _bgra = fb.Format == PixelFormat.Bgra8888;
        _bytes = new byte[_stride * Height];
        Marshal.Copy(fb.Address, _bytes, 0, _bytes.Length);
    }

    public WriteableBitmap Bitmap { get; }
    public int Width { get; }
    public int Height { get; }

    public static Frame Capture(Window window)
    {
        // Shown only if it is not yet: showing a visible window activates it again.
        if (!window.IsVisible)
            window.Show();
        return new Frame(window.CaptureRenderedFrame()!);
    }

    /// <summary>Returns (r, g, b) at a pixel.</summary>
    public (byte R, byte G, byte B) this[int x, int y]
    {
        get
        {
            var i = y * _stride + x * 4;
            return _bgra ? (_bytes[i + 2], _bytes[i + 1], _bytes[i]) : (_bytes[i], _bytes[i + 1], _bytes[i + 2]);
        }
    }

    public double Luma(int x, int y)
    {
        var (r, g, b) = this[x, y];
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    public string Save(string name)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        using var stream = File.Create(path);
        Bitmap.Save(stream, new PngBitmapEncoderOptions());
        return path;
    }

    public void Dispose() => Bitmap.Dispose();
}
