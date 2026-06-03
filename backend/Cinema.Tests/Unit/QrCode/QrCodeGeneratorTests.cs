using Cinema.Infrastructure.QrCode;
using FluentAssertions;

namespace Cinema.Tests.Unit.QrCode;

public sealed class QrCodeGeneratorTests
{
    private readonly QrCodeGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsNonEmptyBytes()
    {
        var result = _generator.Generate("test-token-abc123");
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void Generate_ReturnsPngMagicBytes()
    {
        // PNG починається з: 0x89 0x50 0x4E 0x47
        var result = _generator.Generate("some-qr-token");
        result[0].Should().Be(0x89);
        result[1].Should().Be(0x50); // P
        result[2].Should().Be(0x4E); // N
        result[3].Should().Be(0x47); // G
    }

    [Fact]
    public void Generate_DifferentTokens_ProduceDifferentImages()
    {
        var png1 = _generator.Generate("token-aaa");
        var png2 = _generator.Generate("token-bbb");
        png1.Should().NotEqual(png2);
    }
}
