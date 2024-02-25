using SkiaSharp;
using ZXing.SkiaSharp;

namespace TelegramBot
{
    internal static class Image
    {
        static string Decode(SKBitmap captured)
        {
            var barcodeReader = new BarcodeReader();
            var result = barcodeReader.Decode(captured);
            if (result is null)
                return null;
            else
                return result.Text;
        }
        public static string FromFile(string imagePath) => Decode(SKBitmap.Decode(imagePath));
    }
}
