using NewTek;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Tractus.Ndi.StillSource2.Utils;

namespace Tractus.Ndi.StillSource2;


public unsafe class ImageSource : IDisposable
{
    private bool disposedValue;

    public string Name { get; set; }
    public string Code { get; set; }
    public string Path { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    [JsonIgnore]
    public bool Initialized => this.imageData != nint.Zero;

    [JsonIgnore]
    private nint imageData;
    [JsonIgnore]
    private byte* imageDataPtr;
    [JsonIgnore]
    private nint imageAlphaData;
    [JsonIgnore]
    private byte* imageAlphaDataPtr;

    public void InitializeImage()
    {
        if (this.Initialized)
        {
            return;
        }

        var path = System.IO.Path.IsPathFullyQualified(this.Path)
            ? this.Path
            : System.IO.Path.Combine(Program.ImageRootPath, this.Path);

        ImageReader.LoadImageFileWithAlpha(path,
            out this.imageData,
            out this.imageAlphaData,
            out var width,
            out var height);

        this.Width = width;
        this.Height = height;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (disposing)
            {
                if (this.imageAlphaData != nint.Zero)
                {
                    Marshal.FreeHGlobal(this.imageAlphaData);
                    this.imageAlphaData = nint.Zero;
                    this.imageAlphaDataPtr = null;
                }

                if (this.imageData != nint.Zero)
                {
                    Marshal.FreeHGlobal(this.imageData);
                    this.imageData = nint.Zero;
                    this.imageDataPtr = null;
                }
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            this.disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    internal NDIlib.video_frame_v2_t CreateNDIVideoFrame(NdiSender sender)
    {
        if (!this.Initialized)
        {
            this.InitializeImage();
        }

        var toReturn = new NDIlib.video_frame_v2_t()
        {
            frame_format_type = NDIlib.frame_format_type_e.frame_format_type_progressive,
            FourCC = NDIlib.FourCC_type_e.FourCC_type_UYVY,
            frame_rate_N = sender.FrameRateNumerator,
            frame_rate_D = sender.FrameRateDenominator,
            line_stride_in_bytes = this.Width * 2,
            timecode = NDIlib.send_timecode_synthesize,
            p_data = this.imageData,
            p_metadata = nint.Zero,
            xres = this.Width,
            yres = this.Height,
            picture_aspect_ratio = (float)this.Width / (float)this.Height,
        };

        return toReturn;
    }
}
