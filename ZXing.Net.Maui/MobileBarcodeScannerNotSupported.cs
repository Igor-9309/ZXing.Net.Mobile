namespace ZXing.Net.Maui
{
    public class MobileBarcodeScannerNotSupported : MobileBarcodeScannerBase
    {
        static readonly NotSupportedException ex = new("MobileBarcodeScanner is unsupported on this platform.");

        public override bool IsTorchOn => throw ex;
        public override void AutoFocus() => throw ex;
        public override void Cancel() => throw ex;
        public override void PauseAnalysis() => throw ex;
        public override void ResumeAnalysis() => throw ex;
        public override Task<Result> Scan(MobileBarcodeScanningOptions options) => throw ex;
        public override void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler) => throw ex;
        public override void ToggleTorch() => throw ex;
        public override void Torch(bool on) => throw ex;
    }
}
