using FluentAssertions;
using KSeFPrinter.Exceptions;

namespace KSeFPrinter.Tests.Exceptions;

/// <summary>
/// Testy dla InvoiceParseException
/// </summary>
public class InvoiceParseExceptionTests
{
    [Fact]
    public void Constructor_With_Message_Should_Set_Message()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new InvoiceParseException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.FilePath.Should().BeNull();
        exception.XmlFragment.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_With_Message_And_InnerException_Should_Set_Both()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new InvoiceParseException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
        exception.FilePath.Should().BeNull();
        exception.XmlFragment.Should().BeNull();
    }

    [Fact]
    public void Constructor_With_Message_And_FilePath_Should_Set_Both()
    {
        // Arrange
        var message = "Test error message";
        var filePath = "C:\\test\\invoice.xml";

        // Act
        var exception = new InvoiceParseException(message, filePath);

        // Assert
        exception.Message.Should().Be(message);
        exception.FilePath.Should().Be(filePath);
        exception.XmlFragment.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_With_Message_FilePath_And_InnerException_Should_Set_All()
    {
        // Arrange
        var message = "Test error message";
        var filePath = "C:\\test\\invoice.xml";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new InvoiceParseException(message, filePath, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.FilePath.Should().Be(filePath);
        exception.InnerException.Should().Be(innerException);
        exception.XmlFragment.Should().BeNull();
    }

    [Fact]
    public void Constructor_With_All_Parameters_Should_Set_Everything()
    {
        // Arrange
        var message = "Test error message";
        var filePath = "C:\\test\\invoice.xml";
        var xmlFragment = "<Faktura><InvalidElement></Faktura>";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new InvoiceParseException(message, filePath, xmlFragment, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.FilePath.Should().Be(filePath);
        exception.XmlFragment.Should().Be(xmlFragment);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_With_Null_FilePath_Should_Work()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new InvoiceParseException(message, filePath: null);

        // Assert
        exception.Message.Should().Be(message);
        exception.FilePath.Should().BeNull();
    }

    [Fact]
    public void Constructor_With_Null_XmlFragment_Should_Work()
    {
        // Arrange
        var message = "Test error message";
        var filePath = "C:\\test\\invoice.xml";

        // Act
        var exception = new InvoiceParseException(message, filePath, xmlFragment: null);

        // Assert
        exception.Message.Should().Be(message);
        exception.FilePath.Should().Be(filePath);
        exception.XmlFragment.Should().BeNull();
    }

    [Fact]
    public void Exception_Should_Be_Throwable_And_Catchable()
    {
        // Arrange
        var message = "Test error message";
        var filePath = "C:\\test\\invoice.xml";

        // Act
        Action act = () => throw new InvoiceParseException(message, filePath);

        // Assert
        act.Should().Throw<InvoiceParseException>()
            .WithMessage(message)
            .And.FilePath.Should().Be(filePath);
    }

    [Fact]
    public void Exception_Should_Be_Catchable_As_Base_Exception()
    {
        // Arrange
        var message = "Test error message";

        // Act
        Action act = () => throw new InvoiceParseException(message);

        // Assert
        act.Should().Throw<Exception>()
            .WithMessage(message);
    }

    [Fact]
    public void Exception_With_Long_Message_Should_Work()
    {
        // Arrange
        var message = new string('x', 10000);

        // Act
        var exception = new InvoiceParseException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_With_Special_Characters_In_FilePath_Should_Work()
    {
        // Arrange
        var message = "Test error";
        var filePath = "C:\\test\\ścieżka z polskimi znakami\\faktura.xml";

        // Act
        var exception = new InvoiceParseException(message, filePath);

        // Assert
        exception.FilePath.Should().Be(filePath);
    }

    [Fact]
    public void Exception_With_Large_XmlFragment_Should_Work()
    {
        // Arrange
        var message = "Parse error";
        var filePath = "test.xml";
        var xmlFragment = "<Faktura>" + new string('x', 50000) + "</Faktura>";

        // Act
        var exception = new InvoiceParseException(message, filePath, xmlFragment);

        // Assert
        exception.XmlFragment.Should().Be(xmlFragment);
    }

    [Fact]
    public void Exception_Should_Support_Serialization_Properties()
    {
        // Arrange
        var message = "Test error message";
        var filePath = "C:\\test\\invoice.xml";
        var xmlFragment = "<test/>";

        // Act
        var exception = new InvoiceParseException(message, filePath, xmlFragment);

        // Assert - sprawdź, że wszystkie właściwości są dostępne
        var properties = exception.GetType().GetProperties();
        properties.Should().Contain(p => p.Name == "Message");
        properties.Should().Contain(p => p.Name == "FilePath");
        properties.Should().Contain(p => p.Name == "XmlFragment");
        properties.Should().Contain(p => p.Name == "InnerException");
    }
}
