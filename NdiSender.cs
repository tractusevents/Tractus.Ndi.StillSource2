using NewTek;
using NewTek.NDI;
using System.Runtime.InteropServices;

namespace Tractus.Ndi.StillSource2;

public class NdiSender : IDisposable
{
    public ImageSource Source { get; private set; }
    public string Name { get; set; }
    public string Code { get; }
    public string ImageSourceCode { get; set; }

    private volatile bool sendActualFrameRate;
    public bool SendActualFrameRate
    {
        get => this.sendActualFrameRate;
        set => this.sendActualFrameRate = value;
    }

    public int FrameRateNumerator { get; set; }
    public int FrameRateDenominator { get; set; }

    private Thread? renderThread;
    private bool disposedValue;

    public NdiSender(
        ImageSource source,
        string name,
        string code,
        bool sendActualFrameRate,
        int frameRateNumerator,
        int frameRateDenominator)
    {
        this.Source = source;
        this.Name = name;
        this.Code = code;
        this.ImageSourceCode = source.Code;

        this.SendActualFrameRate = sendActualFrameRate;
        this.FrameRateNumerator = frameRateNumerator;
        this.FrameRateDenominator = frameRateDenominator;

        this.UpdateSource(source);
    }

    public void UpdateSource(ImageSource source)
    {
        if (this.disposedValue)
        {
            return;
        }

        lock (this)
        {
            this.Source = source;
            this.ImageSourceCode = source.Code;

            if (this.renderThread is not null)
            {
                return;
            }

            this.renderThread = new Thread(this.OnRender);
            this.renderThread.Start();
        }
    }

    private void OnRender()
    {
        var createSettings = new NDIlib.send_create_t
        {
            p_ndi_name = UTF.StringToUtf8(this.Name),
            clock_video = true
        };

        var senderPtr = NDIlib.send_create(ref createSettings);

        var lastRenderCode = this.Source.Code;

        var videoFrame = this.Source.CreateNDIVideoFrame(this);

        while (!this.disposedValue)
        {
            if (lastRenderCode != this.Source.Code
                || !this.Source.Initialized)
            {
                videoFrame = this.Source.CreateNDIVideoFrame(this);
                lastRenderCode = this.Source.Code;
            }

            NDIlib.send_send_video_v2(senderPtr, ref videoFrame);

            if (!this.sendActualFrameRate)
            {
                Thread.Sleep(500);
            }
            else
            {
                Thread.Sleep(1);
            }
        }

        NDIlib.send_destroy(senderPtr);
        Marshal.FreeHGlobal(createSettings.p_ndi_name);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            this.disposedValue = true;
            this.renderThread?.Join();
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~NdiSender()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
