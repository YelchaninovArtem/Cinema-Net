namespace Cinema.Application.QrCode;

public interface IQrCodeGenerator
{
    /// <summary>Генерує QR-код і повертає масив байт у форматі PNG.</summary>
    byte[] Generate(string data);
}
