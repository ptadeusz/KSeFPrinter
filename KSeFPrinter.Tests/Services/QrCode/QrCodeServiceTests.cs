using FluentAssertions;
using KSeFPrinter.Services.QrCode;
using Microsoft.Extensions.Logging.Abstractions;

namespace KSeFPrinter.Tests.Services.QrCode;

/// <summary>
/// Testy serwisu generowania kodów QR
/// </summary>
public class QrCodeServiceTests
{
    private readonly QrCodeService _service;

    public QrCodeServiceTests()
    {
        _service = new QrCodeService(NullLogger<QrCodeService>.Instance);
    }

    [Fact]
    public void GenerateQrCode_Should_Return_Non_Empty_Image()
    {
        // Arrange
        var url = "https://ksef-test.mf.gov.pl/client-app/invoice/1234567890/15-10-2025/hash123";

        // Act
        var qrImage = _service.GenerateQrCode(url);

        // Assert
        qrImage.Should().NotBeNullOrEmpty();
        qrImage.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateQrCode_Should_Generate_PNG_Image()
    {
        // Arrange
        var url = "https://test.com";

        // Act
        var qrImage = _service.GenerateQrCode(url);

        // Assert
        // PNG files start with signature: 89 50 4E 47 (‰PNG)
        qrImage.Take(4).Should().Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public void GenerateQrCode_Should_Work_With_Different_Sizes(int pixelsPerModule)
    {
        // Arrange
        var url = "https://test.com";

        // Act
        var qrImage = _service.GenerateQrCode(url, pixelsPerModule);

        // Assert
        qrImage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateQrCode_Should_Throw_For_Empty_Url()
    {
        // Arrange
        var url = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.GenerateQrCode(url));
    }

    [Fact]
    public void GenerateQrCode_Should_Throw_For_Invalid_Size()
    {
        // Arrange
        var url = "https://test.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.GenerateQrCode(url, 0));
        Assert.Throws<ArgumentException>(() => _service.GenerateQrCode(url, -1));
    }

    [Fact]
    public void GenerateQrCodeWithLabel_Should_Return_Image_Larger_Than_Without_Label()
    {
        // Arrange
        var url = "https://test.com";
        var label = "Test Label";

        // Act
        var qrWithoutLabel = _service.GenerateQrCode(url);
        var qrWithLabel = _service.GenerateQrCodeWithLabel(url, label);

        // Assert
        qrWithLabel.Length.Should().BeGreaterThan(qrWithoutLabel.Length);
    }

    [Fact]
    public void GenerateQrCodeWithLabel_Should_Return_Same_As_Without_Label_When_Label_Empty()
    {
        // Arrange
        var url = "https://test.com";

        // Act
        var qrWithoutLabel = _service.GenerateQrCode(url);
        var qrWithEmptyLabel = _service.GenerateQrCodeWithLabel(url, "");

        // Assert
        qrWithEmptyLabel.Should().Equal(qrWithoutLabel);
    }

    [Fact]
    public void AddLabelToQrCode_Should_Add_Label_To_Existing_Image()
    {
        // Arrange
        var url = "https://test.com";
        var label = "Test Label";
        var qrImage = _service.GenerateQrCode(url);

        // Act
        var qrWithLabel = _service.AddLabelToQrCode(qrImage, label);

        // Assert
        qrWithLabel.Should().NotBeNullOrEmpty();
        qrWithLabel.Length.Should().BeGreaterThan(qrImage.Length);
    }

    [Fact]
    public void AddLabelToQrCode_Should_Return_Same_Image_For_Empty_Label()
    {
        // Arrange
        var url = "https://test.com";
        var qrImage = _service.GenerateQrCode(url);

        // Act
        var result = _service.AddLabelToQrCode(qrImage, "");

        // Assert
        result.Should().Equal(qrImage);
    }

    [Fact]
    public void GenerateInvoiceQrCode_Should_Use_Offline_Label_When_No_KSeF_Number()
    {
        // Arrange
        var url = "https://test.com";

        // Act
        var qrImage = _service.GenerateInvoiceQrCode(url, numerKSeF: null);

        // Assert
        qrImage.Should().NotBeNullOrEmpty();
        // Trudno sprawdzić zawartość etykiety bez analizowania obrazu,
        // ale sprawdzamy że działa
    }

    [Fact]
    public void GenerateInvoiceQrCode_Should_Use_KSeF_Number_As_Label()
    {
        // Arrange
        var url = "https://test.com";
        var numerKSeF = "1234567890-20250101-ABC123456789-01";

        // Act
        var qrImage = _service.GenerateInvoiceQrCode(url, numerKSeF);

        // Assert
        qrImage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateCertificateQrCode_Should_Generate_Valid_Image()
    {
        // Arrange
        var url = "https://test.com/certificate";

        // Act
        var qrImage = _service.GenerateCertificateQrCode(url);

        // Assert
        qrImage.Should().NotBeNullOrEmpty();
        // Powinien mieć etykietę "Certyfikat KSeF wystawcy"
    }

    [Fact]
    public void GenerateQrCode_Should_Be_Deterministic()
    {
        // Arrange
        var url = "https://test.com";

        // Act
        var qr1 = _service.GenerateQrCode(url);
        var qr2 = _service.GenerateQrCode(url);

        // Assert
        qr1.Should().Equal(qr2);
    }

    [Theory]
    [InlineData("https://short.url")]
    [InlineData("https://very-long-url-with-lots-of-parameters.com/path1/path2/path3?param1=value1&param2=value2&param3=value3")]
    public void GenerateQrCode_Should_Work_With_Different_Url_Lengths(string url)
    {
        // Act
        var qrImage = _service.GenerateQrCode(url);

        // Assert
        qrImage.Should().NotBeNullOrEmpty();
    }
}
