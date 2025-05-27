namespace ZXing.Net.Maui.Interfaces
{
    public interface IMobileBarcodeScanner
    {
        Task<Result> Scan(MobileBarcodeScanningOptions options);
        Task<Result> Scan();

        void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler);
        void ScanContinuously(Action<Result> scanHandler);

        void Cancel();

        void Torch(bool on);
        void AutoFocus();
        void ToggleTorch();

        void PauseAnalysis();
        void ResumeAnalysis();

        bool UseCustomOverlay { get; }
        string TopText { get; set; }
        string BottomText { get; set; }

        string CancelButtonText { get; set; }
        string FlashButtonText { get; set; }
        string CameraUnsupportedMessage { get; set; }

        bool IsTorchOn { get; }

        bool Supported { get; }
    }
}
