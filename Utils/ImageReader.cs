using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

namespace Tractus.Ndi.StillSource2.Utils;

// Taken from Multiview for NDI SWEngine.
public class ImageReader
{
    public static unsafe void LoadImageFileWithoutAlpha(
        string fileName,
        out nint uyvyData,
        out int width, out int height)
    {
        using var stream = File.OpenRead(fileName);
        using var rgbaImage = Image.Load<Rgba32>(stream);

        var fontUYVYData = Marshal.AllocHGlobal(rgbaImage.Width * rgbaImage.Height * 2);
        var fontUYVYDataPtr = (byte*)fontUYVYData.ToPointer();

        ColorSpaceConverter.ConvertRgba32ToYuv422(rgbaImage, fontUYVYDataPtr, null);

        uyvyData = fontUYVYData;
        width = rgbaImage.Width;
        height = rgbaImage.Height;
    }

    public static unsafe void LoadImageFileWithAlpha(
        string fileName,
        out nint uyvyData, out nint alphaData,
        out int width, out int height)
    {
        using var stream = File.OpenRead(fileName);
        using var pngFile = Image.Load<Rgba32>(stream);

        var fontUYVYData = Marshal.AllocHGlobal(pngFile.Width * pngFile.Height * 2);
        var fontUYVYDataPtr = (byte*)fontUYVYData.ToPointer();

        var fontAlphaData = Marshal.AllocHGlobal(pngFile.Width * pngFile.Height);
        var fontAlphaDataPtr = (byte*)fontAlphaData.ToPointer();

        ColorSpaceConverter.ConvertRgba32ToYuv422(pngFile, fontUYVYDataPtr, fontAlphaDataPtr);

        uyvyData = fontUYVYData;
        alphaData = fontAlphaData;
        width = pngFile.Width;
        height = pngFile.Height;

    }
}
