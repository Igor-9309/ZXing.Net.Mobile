namespace ZXing.Net.Maui
{
    public class CancelScanRequestEventArgs : EventArgs
    {
        public CancelScanRequestEventArgs() => Cancel = false;

        public bool Cancel { get; set; }
    }
}
