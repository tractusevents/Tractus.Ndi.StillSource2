using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace Tractus.Ndi.StillSource2.Utils;


// Taken from Multiview for NDI SWEngine.
public class ColorSpaceConverter
{
    public static unsafe void ConvertRgba32ToYuv422(
        Image<Rgba32> pngFile,
        byte* dataPtr,
        byte* alphaDataPtr)
    {
        var width = pngFile.Width;
        var height = pngFile.Height;

        for (var y = 0; y < height; y++)
        {
            var rowMemory = pngFile.DangerousGetPixelRowMemory(y);
            var rowSpan = rowMemory.Span;

            for (var x = 0; x < width; x += 2)
            {
                var pixel1 = rowSpan[x];
                var pixel2 = x + 1 < width ? rowSpan[x + 1] : new Rgba32(0, 0, 0, 255); // Handle odd width

                // Convert RGB to YUV for pixel1
                var y1 = (byte)Math.Clamp(0.299f * pixel1.R
                                         + 0.587f * pixel1.G
                                         + 0.114f * pixel1.B,
                                         0, 255);

                var u1 = (byte)Math.Clamp(-0.14713f * pixel1.R
                                         - 0.28886f * pixel1.G
                                         + 0.436f * pixel1.B
                                         + 128, 0, 255);

                var v1 = (byte)Math.Clamp(0.615f * pixel1.R
                                         - 0.51499f * pixel1.G
                                         - 0.10001f * pixel1.B
                                         + 128, 0, 255);

                // Convert RGB to YUV for pixel2
                var y2 = (byte)Math.Clamp(0.299f * pixel2.R
                                         + 0.587f * pixel2.G
                                         + 0.114f * pixel2.B,
                                         0, 255);

                var u2 = (byte)Math.Clamp(-0.14713f * pixel2.R
                                         - 0.28886f * pixel2.G
                                         + 0.436f * pixel2.B
                                         + 128, 0, 255);

                var v2 = (byte)Math.Clamp(0.615f * pixel2.R
                                         - 0.51499f * pixel2.G
                                         - 0.10001f * pixel2.B
                                         + 128, 0, 255);


                // Average U and V values for the macropixel
                var u = (byte)((u1 + u2) / 2);
                var v = (byte)((v1 + v2) / 2);

                var index = (y * width + x) * 2;
                var alphaIndex = y * width + x;

                // Pack UYVY data
                dataPtr[index] = u;
                dataPtr[index + 1] = y1;
                dataPtr[index + 2] = v;
                dataPtr[index + 3] = y2;

                if (alphaDataPtr is not null)
                {
                    alphaDataPtr[alphaIndex] = pixel1.A;
                    alphaDataPtr[alphaIndex + 1] = pixel2.A;
                }
            }
        }
    }
}
