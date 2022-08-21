using Android.Hardware;
using Android.Util;
using ApxLabs.FastAndroidCamera;

namespace ZXing.Net.Maui.Platforms.Android.CameraAccess
{
    public class CameraEventsListener : Java.Lang.Object, INonMarshalingPreviewCallback, Camera.IAutoFocusCallback
    {
        public event EventHandler<FastJavaByteArray> OnPreviewFrameReady;

        public void OnPreviewFrame(IntPtr data, Camera camera)
        {
            if (data != IntPtr.Zero)
            {
                using var fastArray = new FastJavaByteArray(data);
                OnPreviewFrameReady?.Invoke(this, fastArray);
                camera.AddCallbackBuffer(fastArray);
            }
        }

        public void OnAutoFocus(bool success, Camera camera)
        {
            Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus {0}", success ? "Succeeded" : "Failed");
        }
    }
}
