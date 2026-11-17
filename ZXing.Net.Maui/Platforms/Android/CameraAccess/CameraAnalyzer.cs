using Android.Util;
using AndroidX.Camera.Core;
using AndroidX.Camera.View;
using AndroidX.Lifecycle;

namespace ZXing.Net.Maui.Platforms.Android.CameraAccess
{
    public class CameraAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        readonly CameraController cameraController;
        readonly IScannerSessionHost scannerHost;
        DateTime lastPreviewAnalysis = DateTime.UtcNow;
        bool wasScanned;
        BarcodeReaderGeneric barcodeReader;

        public CameraAnalyzer(PreviewView previewView, ILifecycleOwner lifecycleOwner, IScannerSessionHost scannerHost)
        {
            this.scannerHost = scannerHost;
            cameraController = new CameraController(previewView, lifecycleOwner, scannerHost);
            Torch = new Torch(cameraController);
        }

        public Action<Result> BarcodeFound;

        public Torch Torch { get; }

        public bool IsAnalyzing { get; private set; }

        public void PauseAnalysis()
            => IsAnalyzing = false;

        public void ResumeAnalysis()
            => IsAnalyzing = true;

        public void ShutdownCamera()
        {
            IsAnalyzing = false;
            Torch.TurnOff();
            cameraController.ShutdownCamera();
        }

        public void SetupCamera()
        {
            barcodeReader = scannerHost.ScanningOptions.BuildBarcodeReader();
            cameraController.SetupCamera(this);
        }

        public void AutoFocus()
            => cameraController.AutoFocus();

        public void AutoFocus(int x, int y)
            => cameraController.AutoFocus(x, y);

        public void RefreshCamera()
            => cameraController.RefreshCamera();

        bool CanAnalyzeFrame
        {
            get
            {
                if (!IsAnalyzing)
                    return false;

                var elapsedTimeMs = (DateTime.UtcNow - lastPreviewAnalysis).TotalMilliseconds;
                if (elapsedTimeMs < scannerHost.ScanningOptions.DelayBetweenAnalyzingFrames)
                    return false;

                return !wasScanned || elapsedTimeMs >= scannerHost.ScanningOptions.DelayBetweenContinuousScans;
            }
        }

        public void Analyze(IImageProxy image)
        {
            try
            {
                if (image == null || !CanAnalyzeFrame || barcodeReader == null)
                    return;

                wasScanned = false;
                lastPreviewAnalysis = DateTime.UtcNow;

                var width = image.Width;
                var height = image.Height;
                var rotationDegrees = image.ImageInfo.RotationDegrees;
                var rotate = rotationDegrees == 90 || rotationDegrees == 270;
                var frame = CopyLuminance(image);

                var start = PerformanceCounter.Start();
                LuminanceSource source = new PlanarYUVLuminanceSource(frame, width, height, 0, 0, width, height, false);
                if (rotate)
                    source = source.rotateCounterClockwise();

                var result = barcodeReader.Decode(source);
                PerformanceCounter.Stop(start,
                    "Decode Time: {0} ms (width: " + width + ", height: " + height + ", degrees: " + rotationDegrees + ", rotate: " + rotate + ")");

                if (result == null)
                    return;

                Log.Debug(MobileBarcodeScanner.TAG, "Barcode Found");
                wasScanned = true;
                BarcodeFound?.Invoke(result);
            }
            catch (Exception ex)
            {
                Log.Error(MobileBarcodeScanner.TAG, ex.ToString());
            }
            finally
            {
                image?.Close();
            }
        }

        static byte[] CopyLuminance(IImageProxy image)
        {
            var width = image.Width;
            var height = image.Height;
            var plane = image.GetPlanes()[0];
            var buffer = plane.Buffer;
            var rowStride = plane.RowStride;
            var pixelStride = plane.PixelStride;
            var luminance = new byte[width * height];

            for (var y = 0; y < height; y++)
            {
                var rowOffset = y * rowStride;
                var outputOffset = y * width;
                for (var x = 0; x < width; x++)
                    luminance[outputOffset + x] = unchecked((byte)buffer.Get(rowOffset + x * pixelStride));
            }

            return luminance;
        }
    }
}
