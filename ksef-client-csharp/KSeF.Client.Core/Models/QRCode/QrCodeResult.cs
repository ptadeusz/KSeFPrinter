namespace KSeF.Client.Core.Models.QRCode
{
    public class QrCodeResult
    {

        public QrCodeResult(string url, string qrCode)
        {
            Url = url;
            QrCode = qrCode;
        }

        public string Url { get; set; }
        public string QrCode { get; set; }
    }
}