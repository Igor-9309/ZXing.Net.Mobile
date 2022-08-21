using Android.Content;
using Android.Util;
using View = Android.Views.View;

namespace ZXing.Net.Maui.Platforms.Android
{
    public class MobileBarcodeScanner : MobileBarcodeScannerBase
    {
        public const string TAG = "ZXing.Net.Mobile";

        public View CustomOverlay { get; set; }

        bool torch = false;

        Context GetContext(Context context)
            => Platform.CurrentActivity ?? Platform.AppContext;

        internal void PlatformScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
            => ScanContinuously(null, options, scanHandler);

        public void ScanContinuously(Context context, MobileBarcodeScanningOptions options, Action<Result> scanHandler)
        {
            var ctx = GetContext(context);
            var scanIntent = new Intent(ctx, typeof(ZxingActivity));

            scanIntent.AddFlags(ActivityFlags.NewTask);

            ZxingActivity.UseCustomOverlayView = UseCustomOverlay;
            ZxingActivity.CustomOverlayView = CustomOverlay;
            ZxingActivity.ScanningOptions = options;
            ZxingActivity.ScanContinuously = true;
            ZxingActivity.TopText = TopText;
            ZxingActivity.BottomText = BottomText;

            ZxingActivity.ScanCompletedHandler = (Result result)
                => scanHandler?.Invoke(result);

            ctx.StartActivity(scanIntent);
        }

        internal Task<Result> PlatformScan(MobileBarcodeScanningOptions options)
            => Scan(null, options);

        public Task<Result> Scan(Context context, MobileBarcodeScanningOptions options)
        {
            var ctx = GetContext(context);

            var task = Task.Factory.StartNew(() =>
            {
                var waitScanResetEvent = new ManualResetEvent(false);

                var scanIntent = new Intent(ctx, typeof(ZxingActivity));

                scanIntent.AddFlags(ActivityFlags.NewTask);

                ZxingActivity.UseCustomOverlayView = UseCustomOverlay;
                ZxingActivity.CustomOverlayView = CustomOverlay;
                ZxingActivity.ScanningOptions = options;
                ZxingActivity.ScanContinuously = false;
                ZxingActivity.TopText = TopText;
                ZxingActivity.BottomText = BottomText;

                Result scanResult = null;

                ZxingActivity.CanceledHandler = () => waitScanResetEvent.Set();

                ZxingActivity.ScanCompletedHandler = (Result result) =>
                {
                    scanResult = result;
                    waitScanResetEvent.Set();
                };

                ctx.StartActivity(scanIntent);

                waitScanResetEvent.WaitOne();

                return scanResult;
            });

            return task;
        }

        internal void PlatformCancel()
            => ZxingActivity.RequestCancel();

        internal void PlatformAutoFocus()
            => ZxingActivity.RequestAutoFocus();

        internal void PlatformTorch(bool on)
        {
            torch = on;
            ZxingActivity.RequestTorch(on);
        }

        internal void PlatformToggleTorch()
            => Torch(!torch);

        internal void PlatformPauseAnalysis()
            => ZxingActivity.RequestPauseAnalysis();

        internal void PlatformResumeAnalysis()
            => ZxingActivity.RequestResumeAnalysis();

        internal bool PlatformIsTorchOn
            => torch;

        public override bool IsTorchOn => throw new NotImplementedException();

        internal static void LogDebug(string format, params object[] args)
            => Log.Debug("ZXING", format, args);

        internal static void LogError(string format, params object[] args)
            => Log.Error("ZXING", format, args);

        internal static void LogInfo(string format, params object[] args)
            => Log.Info("ZXING", format, args);

        internal static void LogWarn(string format, params object[] args)
            => Log.Warn("ZXING", format, args);

        public override Task<Result> Scan(MobileBarcodeScanningOptions options)
            => PlatformScan(options);

        public override void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
            => PlatformScanContinuously(options, scanHandler);

        public override void Cancel()
            => PlatformCancel();

        public override void AutoFocus()
            => PlatformAutoFocus();

        public override void Torch(bool on)
            => PlatformTorch(on);

        public override void ToggleTorch()
            => PlatformToggleTorch();

        public override void PauseAnalysis()
            => PlatformPauseAnalysis();

        public override void ResumeAnalysis()
            => PlatformResumeAnalysis();
    }
}
