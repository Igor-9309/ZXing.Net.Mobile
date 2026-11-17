using System.Threading.Tasks;
using Xamarin.Essentials;

namespace ZXing.Net.Mobile.Android
{
	public static class PermissionsHandler
	{
		public static async Task<bool> RequestPermissionsAsync()
		{
			var camera = await Permissions.RequestAsync<Permissions.Camera>();

			if (camera != PermissionStatus.Granted)
				return false;

			// Only check if it's in the manifest already
			if (Permissions.IsDeclaredInManifest(global::Android.Manifest.Permission.Flashlight))
			{
				var flashlight = await Permissions.RequestAsync<Permissions.Flashlight>();
				if (flashlight != PermissionStatus.Granted)
					return false;
			}

			return true;
		}

		internal static bool IsTorchPermissionDeclared()
			=> Permissions.IsDeclaredInManifest(global::Android.Manifest.Permission.Flashlight);
	}
}
