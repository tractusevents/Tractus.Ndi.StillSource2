namespace Tractus.Ndi.StillSource2.WebModels;

public class SetupNdiSenderModel
{
    public string Name { get; set; }
    public string SenderCode { get; set; }
    public string ImageSourceCode { get; set; }
    public bool SendActualFrameRate { get; set; }
    public int FrameRateNumerator { get; set; }
    public int FrameRateDenominator { get; set; }
}
