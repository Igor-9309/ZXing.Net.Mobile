namespace ZXing.Net.Maui
{
    public abstract class MobileBarcodeScannerBase : IMobileBarcodeScanner
    {
        public MobileBarcodeScannerBase()
        {
            CancelButtonText = "Cancel";
            FlashButtonText = "Flash";
            CameraUnsupportedMessage = "Unable to start Camera for Scanning";
        }

        public bool UseCustomOverlay { get; set; }

        public string TopText { get; set; }

        public string BottomText { get; set; }

        public string CancelButtonText { get; set; }

        public string FlashButtonText { get; set; }

        public string CameraUnsupportedMessage { get; set; }

        public abstract Task<Result> Scan(MobileBarcodeScanningOptions options);

        public Task<Result> Scan()
            => Scan(MobileBarcodeScanningOptions.Default);

        public void ScanContinuously(Action<Result> scanHandler)
            => ScanContinuously(MobileBarcodeScanningOptions.Default, scanHandler);

        public abstract void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler);

        public abstract void Cancel();

        public abstract void Torch(bool on);

        public abstract void ToggleTorch();

        public abstract bool IsTorchOn { get; }

        public abstract void AutoFocus();

        public abstract void PauseAnalysis();

        public abstract void ResumeAnalysis();
    }
}
