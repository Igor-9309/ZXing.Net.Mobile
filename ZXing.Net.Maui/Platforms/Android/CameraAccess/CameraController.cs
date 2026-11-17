using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Hardware.Camera2;
using Android.Util;
using Android.Views;
using AndroidX.Camera.Camera2.InterOp;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Java.Util.Concurrent;
using Size = Android.Util.Size;

namespace ZXing.Net.Maui.Platforms.Android.CameraAccess
{
    public class CameraController
    {
        const string Tag = "ZXing.Net.Mobile";
        readonly Context context;
        readonly PreviewView previewView;
        readonly ILifecycleOwner lifecycleOwner;
        readonly IScannerSessionHost scannerHost;
        IExecutorService analysisExecutor;
        ProcessCameraProvider cameraProvider;
        Preview preview;
        ImageAnalysis imageAnalysis;
        ICamera camera;
        ImageAnalysis.IAnalyzer analyzer;
        bool setupPending;
        bool setupRequested;
        int targetRotation = -1;

        public CameraController(PreviewView previewView, ILifecycleOwner lifecycleOwner, IScannerSessionHost scannerHost)
        {
            this.previewView = previewView ?? throw new ArgumentNullException(nameof(previewView));
            this.lifecycleOwner = lifecycleOwner ?? throw new ArgumentNullException(nameof(lifecycleOwner));
            this.scannerHost = scannerHost ?? throw new ArgumentNullException(nameof(scannerHost));
            context = previewView.Context;
        }

        public bool IsCameraRunning => camera != null;

        public bool HasTorch => camera?.CameraInfo?.HasFlashUnit ?? false;

        public void SetupCamera(ImageAnalysis.IAnalyzer analyzer)
        {
            this.analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            setupRequested = true;
            analysisExecutor ??= Executors.NewSingleThreadExecutor();

            if (camera != null || setupPending)
                return;

            if (cameraProvider != null)
            {
                BindCamera();
                return;
            }

            setupPending = true;
            var cameraProviderFuture = ProcessCameraProvider.GetInstance(context);
            cameraProviderFuture.AddListener(new Java.Lang.Runnable(() =>
            {
                setupPending = false;
                if (!setupRequested)
                    return;

                try
                {
                    cameraProvider = cameraProviderFuture.Get() as ProcessCameraProvider
                        ?? throw new InvalidOperationException("CameraX did not return a camera provider.");
                    BindCamera();
                }
                catch (Exception ex)
                {
                    Log.Error(Tag, ex.ToString());
                }
            }), ContextCompat.GetMainExecutor(context));
        }

        public void RefreshCamera()
        {
            if (camera == null)
                return;

            var rotation = GetDisplayRotation();
            if (rotation == targetRotation)
                return;

            var currentAnalyzer = analyzer;
            ShutdownCamera();
            SetupCamera(currentAnalyzer);
        }

        public void ShutdownCamera()
        {
            setupRequested = false;

            if (cameraProvider != null && preview != null && imageAnalysis != null)
            {
                try
                {
                    cameraProvider.Unbind(preview, imageAnalysis);
                }
                catch (Exception ex)
                {
                    Log.Error(Tag, ex.ToString());
                }
            }

            imageAnalysis?.ClearAnalyzer();
            camera?.Dispose();
            imageAnalysis?.Dispose();
            preview?.Dispose();
            analysisExecutor?.Shutdown();
            analysisExecutor?.Dispose();
            analysisExecutor = null;
            camera = null;
            imageAnalysis = null;
            preview = null;
            targetRotation = -1;
        }

        public void AutoFocus()
            => AutoFocus(previewView.Width / 2f, previewView.Height / 2f);

        public void AutoFocus(float x, float y)
        {
            if (camera == null || scannerHost.ScanningOptions.DisableAutofocus)
                return;

            try
            {
                var point = previewView.MeteringPointFactory.CreatePoint(x, y);
                var action = new FocusMeteringAction.Builder(point, FocusMeteringAction.FlagAf).Build();
                camera.CameraControl.StartFocusAndMetering(action);
            }
            catch (Exception ex)
            {
                Log.Warn(Tag, ex.ToString());
            }
        }

        public void SetTorch(bool enabled, Action<bool> completed)
        {
            if (camera == null)
            {
                completed?.Invoke(false);
                return;
            }

            try
            {
                var future = camera.CameraControl.EnableTorch(enabled);
                future.AddListener(new Java.Lang.Runnable(() =>
                {
                    try
                    {
                        future.Get();
                        completed?.Invoke(true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(Tag, ex.ToString());
                        completed?.Invoke(false);
                    }
                    finally
                    {
                        future.Dispose();
                    }
                }), ContextCompat.GetMainExecutor(context));
            }
            catch (Exception ex)
            {
                Log.Error(Tag, ex.ToString());
                completed?.Invoke(false);
            }
        }

        void BindCamera()
        {
            if (!setupRequested || cameraProvider == null || camera != null)
                return;

            var cameraSelector = SelectCamera();
            var resolutionSelector = CreateResolutionSelector();
            var rotation = GetDisplayRotation();
            var autofocusMode = (int)(scannerHost.ScanningOptions.DisableAutofocus
                ? ControlAFMode.Off
                : ControlAFMode.ContinuousPicture);

            var previewBuilder = new Preview.Builder();
            previewBuilder.SetResolutionSelector(resolutionSelector);
            previewBuilder.SetTargetRotation(rotation);
            new Camera2Interop.Extender(previewBuilder)
                .SetCaptureRequestOption(CaptureRequest.ControlAfMode, autofocusMode);
            preview = previewBuilder.Build();
            preview.SurfaceProvider = previewView.SurfaceProvider;

            var analysisBuilder = new ImageAnalysis.Builder();
            analysisBuilder.SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest);
            analysisBuilder.SetResolutionSelector(resolutionSelector);
            analysisBuilder.SetTargetRotation(rotation);
            new Camera2Interop.Extender(analysisBuilder)
                .SetCaptureRequestOption(CaptureRequest.ControlAfMode, autofocusMode);
            imageAnalysis = analysisBuilder.Build();
            imageAnalysis.SetAnalyzer(analysisExecutor, analyzer);

            try
            {
                camera = cameraProvider.BindToLifecycle(lifecycleOwner, cameraSelector, preview, imageAnalysis);
                targetRotation = rotation;
            }
            catch
            {
                imageAnalysis.ClearAnalyzer();
                imageAnalysis.Dispose();
                preview.Dispose();
                imageAnalysis = null;
                preview = null;
                throw;
            }
        }

        CameraSelector SelectCamera()
        {
            var backCamera = CameraSelector.DefaultBackCamera;
            var frontCamera = CameraSelector.DefaultFrontCamera;

            if (scannerHost.ScanningOptions.UseFrontCameraIfAvailable == true && cameraProvider.HasCamera(frontCamera))
                return frontCamera;

            if (cameraProvider.HasCamera(backCamera))
                return backCamera;

            if (cameraProvider.HasCamera(frontCamera))
                return frontCamera;

            throw new NotSupportedException("This device does not have a supported camera.");
        }

        ResolutionSelector CreateResolutionSelector()
        {
            var builder = new ResolutionSelector.Builder();
            builder.SetResolutionStrategy(new ResolutionStrategy(
                new Size(640, 480),
                ResolutionStrategy.FallbackRuleClosestHigherThenLower));
            builder.SetResolutionFilter(new CameraResolutionFilter(scannerHost));
            return builder.Build();
        }

        int GetDisplayRotation()
            => (int)(previewView.Display?.Rotation ?? SurfaceOrientation.Rotation0);

        sealed class CameraResolutionFilter : Java.Lang.Object, IResolutionFilter
        {
            readonly IScannerSessionHost scannerHost;

            public CameraResolutionFilter(IScannerSessionHost scannerHost)
                => this.scannerHost = scannerHost;

            public IList<Size> Filter(IList<Size> supportedSizes, int rotationDegrees)
            {
                if (supportedSizes == null || supportedSizes.Count == 0)
                    return supportedSizes;

                var resolutions = supportedSizes.Select(size => new CameraResolution
                {
                    Width = size.Width,
                    Height = size.Height
                }).ToList();

                var selectedResolution = scannerHost.ScanningOptions.GetResolution(resolutions)
                    ?? resolutions.FirstOrDefault(resolution =>
                        resolution.Width >= 640 && resolution.Width <= 1000 &&
                        resolution.Height >= 360 && resolution.Height <= 1000)
                    ?? resolutions[0];

                var selectedIndex = supportedSizes.ToList().FindIndex(size =>
                    size.Width == selectedResolution.Width && size.Height == selectedResolution.Height);

                if (selectedIndex < 0)
                {
                    Log.Warn(Tag, "The camera resolution selector returned a size that is not supported.");
                    return supportedSizes;
                }

                var preferredSizes = new List<Size> { supportedSizes[selectedIndex] };
                for (var index = 0; index < supportedSizes.Count; index++)
                {
                    if (index != selectedIndex)
                        preferredSizes.Add(supportedSizes[index]);
                }

                return preferredSizes;
            }
        }
    }
}
