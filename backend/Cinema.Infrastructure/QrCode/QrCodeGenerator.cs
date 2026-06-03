using Cinema.Application.QrCode;
using QRCoder;

namespace Cinema.Infrastructure.QrCode;

public sealed class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] Generate(string data)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData  = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode      = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(10);
    }
}
