using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MyVault.Core.Services;
using ZXing;
using ZXing.Common;

namespace MyVault.App.Services;

public static class QrSetupReader
{
    public static AuthenticatorSetup Read(string path)
    {
        if (new FileInfo(path).Length > 20 * 1024 * 1024) throw new ArgumentException("Choose a QR image smaller than 20 MiB.");
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
        var frame = decoder.Frames[0];
        if ((long)frame.PixelWidth * frame.PixelHeight > 4_000_000) throw new ArgumentException("Choose a QR image under four megapixels.");
        var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        int stride = checked(converted.PixelWidth * 4);
        byte[] pixels = new byte[checked(stride * converted.PixelHeight)];
        try
        {
            converted.CopyPixels(pixels, stride, 0);
            var reader = new BarcodeReaderGeneric { Options = new DecodingOptions { PossibleFormats = new[] { BarcodeFormat.QR_CODE }, TryHarder = true } };
            var result = reader.Decode(pixels, converted.PixelWidth, converted.PixelHeight, RGBLuminanceSource.BitmapFormat.BGRA32);
            if (result is null) throw new ArgumentException("No readable QR code was found. Use the manual setup key instead.");
            return AuthenticatorSetup.Parse(result.Text);
        }
        finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(pixels); }
    }
}
