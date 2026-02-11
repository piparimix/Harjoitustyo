using System.IO;
using System.Windows.Media.Imaging;

namespace Harjoitustyö
{
    public class Barcode
    {
        // Apumetodi, joka hoitaa datan muotoilun yhdessä paikassa 
        private static string MuotoileData(string data)
        {
            return "LASKU-" + data;
        }

        // Metodi viivakoodin luomiseen ja palauttamiseen byte-taulukkona PNG-muodossa
        public static byte[] GetBarcodeBytes(string data)
        {
            string muokattuData = MuotoileData(data);
            BarcodeStandard.Barcode b = new BarcodeStandard.Barcode();
            var img = b.Encode(BarcodeStandard.Type.Code128, muokattuData, SkiaSharp.SKColors.Black, SkiaSharp.SKColors.White, 200, 80);
            using (var encoded = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
            {
                return encoded.ToArray();
            }
        }

        // Metodi viivakoodin luomiseen ja palauttamiseen WPF BitmapImage -muodossa
        public static BitmapImage GenerateBarcode(string data)
        {
            string muokattuData = MuotoileData(data);

            BarcodeStandard.Barcode b = new BarcodeStandard.Barcode();
            var img = b.Encode(BarcodeStandard.Type.Code128, muokattuData, SkiaSharp.SKColors.Black, SkiaSharp.SKColors.White, 200, 80);
            return ConvertSkiaImageToWPF(img);
        }

        // Apumetodi SkiaSharp SKImage -kuvan muuntamiseen WPF BitmapImage -muotoon
        public static BitmapImage ConvertSkiaImageToWPF(SkiaSharp.SKImage skImg)
        {
            using (var data = skImg.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
            using (var ms = new MemoryStream())
            {
                data.SaveTo(ms);
                ms.Seek(0, SeekOrigin.Begin);

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;

                image.StreamSource = ms;
                image.EndInit();
                image.Freeze(); // Parantaa suorituskykyä ja tekee kuvasta säikeistöturvallisen

                return image;
            }
        }
    }
}