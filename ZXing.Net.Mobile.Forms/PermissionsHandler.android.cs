using System;
using Android.Content.PM;

namespace ZXing.Net.Mobile.Forms.Android
{
	public static class PermissionsHandler
	{
		[Obsolete("Use ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult instead")]
		public static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
		{
			// Forward the call to the generic android implementation
			Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}
	}
}
