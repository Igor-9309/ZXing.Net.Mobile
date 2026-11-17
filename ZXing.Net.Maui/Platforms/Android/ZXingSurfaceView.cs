using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Camera.View;
using AndroidX.Lifecycle;
using ZXing.Net.Maui.Platforms.Android.CameraAccess;
using ZXing.Net.Mobile.Android;

namespace ZXing.Net.Maui.Platforms.Android
{
    public class ZXingSurfaceView : FrameLayout, IScannerView, IScannerSessionHost
    {
        readonly ILifecycleOwner lifecycleOwner;
        PreviewView previewView;
        CameraAnalyzer cameraAnalyzer;
        bool cameraSetupComplete;
        bool cameraSetupPending;

        internal event Action<bool> TorchStateChanged;

        public ZXingSurfaceView(Context context, MobileBarcodeScanningOptions options)
            : this(context, options, ResolveLifecycleOwner(context))
        {
        }

        internal ZXingSurfaceView(Context context, MobileBarcodeScanningOptions options, ILifecycleOwner lifecycleOwner)
            : base(context)
        {
            this.lifecycleOwner = lifecycleOwner;
            ScanningOptions = options ?? new MobileBarcodeScanningOptions();
            Init();
        }

        protected ZXingSurfaceView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            ScanningOptions = new MobileBarcodeScanningOptions();
        }

        static ILifecycleOwner ResolveLifecycleOwner(Context context)
        {
            if (context is ILifecycleOwner lifecycleOwner)
                return lifecycleOwner;

            if (context is ContextWrapper wrapper && wrapper.BaseContext is ILifecycleOwner baseLifecycleOwner)
                return baseLifecycleOwner;

            if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is ILifecycleOwner currentActivity)
                return currentActivity;

            throw new InvalidOperationException("The scanner requires an AndroidX lifecycle owner.");
        }

        void Init()
        {
            if (previewView == null)
            {
                previewView = new PreviewView(Context)
                {
                    LayoutParameters = new FrameLayout.LayoutParams(
                        ViewGroup.LayoutParams.MatchParent,
                        ViewGroup.LayoutParams.MatchParent)
                };
                AddView(previewView);
            }

            if (cameraAnalyzer == null)
            {
                cameraAnalyzer = new CameraAnalyzer(
                    previewView,
                    lifecycleOwner ?? ResolveLifecycleOwner(Context),
                    this);
                cameraAnalyzer.Torch.StateChanged += OnTorchStateChanged;
            }
            cameraAnalyzer.ResumeAnalysis();
        }

        void OnTorchStateChanged(bool isTorchOn)
            => TorchStateChanged?.Invoke(isTorchOn);

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            Init();
            _ = EnsureCameraSetupAsync();
        }

        protected override void OnDetachedFromWindow()
        {
            cameraSetupComplete = false;
            cameraAnalyzer?.ShutdownCamera();
            base.OnDetachedFromWindow();
        }

        async Task EnsureCameraSetupAsync()
        {
            if (cameraSetupComplete || cameraSetupPending || cameraAnalyzer == null)
                return;

            cameraSetupPending = true;
            try
            {
                if (await PermissionsHandler.RequestPermissionsAsync())
                {
                    cameraAnalyzer.SetupCamera();
                    cameraSetupComplete = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(MobileBarcodeScanner.TAG, ex.ToString());
            }
            finally
            {
                cameraSetupPending = false;
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            var handled = base.OnTouchEvent(e);

            switch (e.Action)
            {
                case MotionEventActions.Down:
                    return true;
                case MotionEventActions.Up:
                    AutoFocus((int)e.GetX(), (int)e.GetY());
                    break;
            }

            return handled;
        }

        public void AutoFocus()
            => cameraAnalyzer?.AutoFocus();

        public void AutoFocus(int x, int y)
            => cameraAnalyzer?.AutoFocus(x, y);

        public void StartScanning(Action<Result> scanResultCallback, MobileBarcodeScanningOptions options = null)
        {
            ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;
            cameraAnalyzer.BarcodeFound = result => scanResultCallback?.Invoke(result);
            cameraAnalyzer.ResumeAnalysis();
            _ = EnsureCameraSetupAsync();
        }

        public void StopScanning()
        {
            cameraAnalyzer?.ShutdownCamera();
            cameraSetupComplete = false;
        }

        public void PauseAnalysis()
            => cameraAnalyzer?.PauseAnalysis();

        public void ResumeAnalysis()
            => cameraAnalyzer?.ResumeAnalysis();

        public void Torch(bool on)
        {
            if (on)
                cameraAnalyzer?.Torch.TurnOn();
            else
                cameraAnalyzer?.Torch.TurnOff();
        }

        public void ToggleTorch()
            => cameraAnalyzer?.Torch.Toggle();

        public MobileBarcodeScanningOptions ScanningOptions { get; set; }

        public bool IsTorchOn => cameraAnalyzer?.Torch.IsEnabled ?? false;

        public bool IsAnalyzing => cameraAnalyzer?.IsAnalyzing ?? false;

        public bool HasTorch => cameraAnalyzer?.Torch.IsSupported ?? false;

        protected override void OnWindowVisibilityChanged(ViewStates visibility)
        {
            base.OnWindowVisibilityChanged(visibility);
            if (visibility == ViewStates.Visible)
            {
                Init();
                _ = EnsureCameraSetupAsync();
            }
        }

        public override void OnWindowFocusChanged(bool hasWindowFocus)
        {
            base.OnWindowFocusChanged(hasWindowFocus);
            if (hasWindowFocus && cameraSetupComplete)
                cameraAnalyzer.RefreshCamera();
        }
    }
}
