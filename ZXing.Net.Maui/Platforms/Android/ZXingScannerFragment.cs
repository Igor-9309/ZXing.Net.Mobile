using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using View = Android.Views.View;
using Button = Android.Widget.Button;

namespace ZXing.Net.Maui.Platforms.Android
{
    public class ZXingScannerFragment : Fragment, IZXingScanner<View>, IScannerView
    {
        public ZXingScannerFragment() => UseCustomOverlayView = false;

        FrameLayout frame;
        Button flashButton;

        public override View OnCreateView(LayoutInflater layoutInflater, ViewGroup viewGroup, Bundle bundle)
        {
            frame = (FrameLayout)layoutInflater.Inflate(Resource.Layout.zxingscanneractivitylayout, viewGroup, false);

            var layoutParams = GetChildLayoutParams();

            try
            {
                scanner = new ZXingSurfaceView(Activity, ScanningOptions, this);
                scanner.TorchStateChanged += UpdateFlashButtonText;

                frame.AddView(scanner, layoutParams);

                if (!UseCustomOverlayView)
                {
                    zxingOverlay = new ZxingOverlayView(Activity)
                    {
                        TopText = TopText ?? "",
                        BottomText = BottomText ?? ""
                    };

                    frame.AddView(zxingOverlay, layoutParams);

                    flashButton = new Button(Activity);
                    UpdateFlashButtonText(IsTorchOn);
                    flashButton.Click += (sender, args) => ToggleTorch();
                    frame.AddView(flashButton, GetFlashButtonLayoutParams());
                }
                else if (CustomOverlayView != null)
                {
                    frame.AddView(CustomOverlayView, layoutParams);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Create Surface View Failed: " + ex);
            }

            // Someone tried to call StartScanning before we were ready. Call it again.
            if (scanCallback != null)
            {
                StartScanning(scanCallback, ScanningOptions);
            }

            Log.Debug(MobileBarcodeScanner.TAG, "ZXingScannerFragment->OnResume exit");

            return frame;
        }

        public override void OnStart()
        {
            base.OnStart();
            // won't be 0 if OnCreateView has been called before.
            if (scanner != null && scanner.Parent == null)
            {
                var layoutParams = GetChildLayoutParams();
                // reattach scanner and overlay views.
                frame.AddView(scanner, layoutParams);

                if (!UseCustomOverlayView)
                {
                    frame.AddView(zxingOverlay, layoutParams);
                    if (flashButton != null)
                        frame.AddView(flashButton, GetFlashButtonLayoutParams());
                }
                else if (CustomOverlayView != null)
                    frame.AddView(CustomOverlayView, layoutParams);
            }
        }

        public override void OnStop()
        {
            if (scanner != null)
            {
                scanner.StopScanning();

                frame.RemoveView(scanner);
            }

            if (!UseCustomOverlayView)
                frame.RemoveView(zxingOverlay);
            else if (CustomOverlayView != null)
                frame.RemoveView(CustomOverlayView);

            if (flashButton != null)
                frame.RemoveView(flashButton);

            base.OnStop();
        }

        static LinearLayout.LayoutParams GetChildLayoutParams() =>
            new(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
            {
                Weight = 1
            };

        FrameLayout.LayoutParams GetFlashButtonLayoutParams()
            => new(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent, GravityFlags.Bottom | GravityFlags.CenterHorizontal)
            {
                BottomMargin = (int)(24 * Activity.Resources.DisplayMetrics.Density)
            };

        void UpdateFlashButtonText(bool isTorchOn)
        {
            if (flashButton == null)
                return;

            flashButton.Text = isTorchOn
                ? FlashOnButtonText ?? "Flash"
                : FlashOffButtonText ?? "Flash";
        }

        public View CustomOverlayView { get; set; }
        public bool UseCustomOverlayView { get; set; }
        public MobileBarcodeScanningOptions ScanningOptions { get; set; }
        public string TopText { get; set; }
        public string BottomText { get; set; }
        public string FlashOnButtonText { get; set; }
        public string FlashOffButtonText { get; set; }

        ZXingSurfaceView scanner;
        ZxingOverlayView zxingOverlay;

        public void Torch(bool on)
            => scanner?.Torch(on);

        public void AutoFocus()
            => scanner?.AutoFocus();

        public void AutoFocus(int x, int y)
            => scanner?.AutoFocus(x, y);

        Action<Result> scanCallback;

        public void StartScanning(Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
        {
            ScanningOptions = options;
            scanCallback = scanResultHandler;

            if (scanner == null)
                return;

            Scan();
        }

        void Scan()
            => scanner?.StartScanning(scanCallback, ScanningOptions);

        public void StopScanning()
            => scanner?.StopScanning();

        public void PauseAnalysis()
            => scanner?.PauseAnalysis();

        public void ResumeAnalysis()
            => scanner?.ResumeAnalysis();

        public void ToggleTorch()
            => scanner?.ToggleTorch();

        public bool IsTorchOn
            => scanner?.IsTorchOn ?? false;

        public bool IsAnalyzing
            => scanner?.IsAnalyzing ?? false;

        public bool HasTorch
            => scanner?.HasTorch ?? false;

        public override void OnDestroy()
        {
            base.OnDestroy();
            scanner?.TorchStateChanged -= UpdateFlashButtonText;
        }
    }
}
