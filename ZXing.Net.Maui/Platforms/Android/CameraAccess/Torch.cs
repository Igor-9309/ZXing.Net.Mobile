using System;
using Android.Util;

namespace ZXing.Net.Maui.Platforms.Android.CameraAccess
{
    public class Torch
    {
        readonly CameraController cameraController;

        public Torch(CameraController cameraController)
            => this.cameraController = cameraController;

        public bool IsSupported => cameraController.HasTorch;

        public bool IsEnabled { get; private set; }

        public event Action<bool> StateChanged;

        public void TurnOn() => Enable(true);

        public void TurnOff() => Enable(false);

        public void Toggle() => Enable(!IsEnabled);

        void Enable(bool state)
        {
            if (!IsSupported || IsEnabled == state)
                return;

            if (!cameraController.IsCameraRunning)
            {
                Log.Info(MobileBarcodeScanner.TAG, "Camera is not running; cannot toggle torch.");
                return;
            }

            cameraController.SetTorch(state, succeeded =>
            {
                if (succeeded)
                {
                    IsEnabled = state;
                    StateChanged?.Invoke(state);
                }
            });
        }
    }
}
