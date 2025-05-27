namespace ZXing.Net.Maui
{
    public static class HostBuilderExtensions
    {
        public static MauiAppBuilder UseBarcodeReader(this MauiAppBuilder builder)
        {
#if ANDROID
            builder.Services.AddTransient<IMobileBarcodeScanner, Platforms.Android.MobileBarcodeScanner>();
#else
            builder.Services.AddTransient<IMobileBarcodeScanner, MobileBarcodeScannerNotSupported>();
#endif
            return builder;
        }
    }
}
