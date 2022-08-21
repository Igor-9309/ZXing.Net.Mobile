namespace ZXing.Net.Maui.Interfaces
{
    public interface IZXingScanner<TOverlayViewType> : IScannerView
    {
        TOverlayViewType CustomOverlayView { get; set; }
        bool UseCustomOverlayView { get; set; }
        string TopText { get; set; }
        string BottomText { get; set; }
    }
}
