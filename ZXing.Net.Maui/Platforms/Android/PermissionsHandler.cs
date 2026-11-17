using Android.Content.PM;

namespace ZXing.Net.Mobile.Android
{
    public static class PermissionsHandler
    {
        public static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
            => Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        public static async Task<bool> RequestPermissionsAsync()
        {
            var camera = await Permissions.RequestAsync<Permissions.Camera>();

            if (camera != PermissionStatus.Granted)
                return false;

            return true;
        }
    }
}
