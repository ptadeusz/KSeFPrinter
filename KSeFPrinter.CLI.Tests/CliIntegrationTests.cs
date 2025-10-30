using FluentAssertions;
using System.Diagnostics;
using System.Text;

namespace KSeFPrinter.CLI.Tests;

/// <summary>
/// Testy integracyjne dla CLI - uruchamia program jako proces zewnętrzny
/// </summary>
public class CliIntegrationTests : IDisposable
{
    private readonly string _testOutputDir;
    private readonly string _cliExecutable;

    public CliIntegrationTests()
    {
        // Katalog tymczasowy dla testów
        _testOutputDir = Path.Combine(Path.GetTempPath(), "KseFCLITests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDir);

        // Ścieżka do wykonywalnego pliku CLI
        // W środowisku testowym używamy DLL z dotnet
        _cliExecutable = FindCliExecutable();
    }

    public void Dispose()
    {
        // Cleanup
        if (Directory.Exists(_testOutputDir))
        {
            try
            {
                Directory.Delete(_testOutputDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private static string FindCliExecutable()
    {
        // Znajdź ścieżkę do projektu CLI
        var currentDir = Directory.GetCurrentDirectory();
        var solutionDir = Directory.GetParent(currentDir)?.Parent?.Parent?.Parent?.FullName;

        if (solutionDir == null)
            throw new InvalidOperationException("Nie można znaleźć katalogu rozwiązania");

        // Spróbuj znaleźć w Release lub Debug (output CLI to ksef-pdf.dll)
        var possiblePaths = new[]
        {
            Path.Combine(solutionDir, "KSeFPrinter.CLI", "bin", "Release", "net9.0", "ksef-pdf.dll"),
            Path.Combine(solutionDir, "KSeFPrinter.CLI", "bin", "Debug", "net9.0", "ksef-pdf.dll")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException(
            $"Nie można znaleźć pliku wykonywalnego CLI. Sprawdzono: {string.Join(", ", possiblePaths)}");
    }

    private async Task<(int ExitCode, string Output, string Error)> RunCliAsync(string arguments, int timeoutSeconds = 30)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{_cliExecutable}\" {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _testOutputDir
        };

        using var process = new Process { StartInfo = startInfo };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var completed = await Task.Run(() => process.WaitForExit(timeoutSeconds * 1000));

        if (!completed)
        {
            process.Kill();
            throw new TimeoutException($"CLI nie zakończył działania w ciągu {timeoutSeconds} sekund");
        }

        return (process.ExitCode, output.ToString(), error.ToString());
    }

    [Fact]
    public async Task CLI_Should_Show_Help_With_Help_Option()
    {
        // Act
        var result = await RunCliAsync("--help");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("KSeF PDF Generator");
        result.Output.Should().Contain("input");
        result.Output.Should().Contain("--output");
    }

    [Fact]
    public async Task CLI_Should_Generate_Pdf_For_Single_File()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var pdfPath = Path.Combine(_testOutputDir, "output.pdf");

        // Skopiuj plik XML do katalogu roboczego
        var testXmlPath = Path.Combine(_testOutputDir, "test.xml");
        File.Copy(xmlPath, testXmlPath, overwrite: true);

        // Act
        var result = await RunCliAsync($"\"{testXmlPath}\" --output \"{pdfPath}\" --no-validate");

        // Assert
        result.ExitCode.Should().Be(0);
        File.Exists(pdfPath).Should().BeTrue();

        var fileInfo = new FileInfo(pdfPath);
        fileInfo.Length.Should().BeGreaterThan(1000);

        // Verify PDF signature
        var pdfBytes = await File.ReadAllBytesAsync(pdfPath);
        var pdfSignature = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        pdfSignature.Should().Be("%PDF");
    }

    [Fact]
    public async Task CLI_Should_Return_Error_For_Nonexistent_File()
    {
        // Arrange
        var nonexistentPath = Path.Combine(_testOutputDir, "nonexistent.xml");

        // Act
        var result = await RunCliAsync($"\"{nonexistentPath}\"");

        // Assert
        result.ExitCode.Should().Be(1);
        result.Error.Should().Contain("Nie znaleziono");
    }

    [Fact]
    public async Task CLI_Should_Process_Multiple_Files_In_Batch()
    {
        // Arrange
        var xml1Path = Path.Combine(_testOutputDir, "test1.xml");
        var xml2Path = Path.Combine(_testOutputDir, "test2.xml");

        File.Copy(Path.Combine("TestData", "FakturaTEST017.xml"), xml1Path, overwrite: true);
        File.Copy(Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml"), xml2Path, overwrite: true);

        // Act
        var result = await RunCliAsync($"\"{xml1Path}\" \"{xml2Path}\" --no-validate");

        // Assert
        result.ExitCode.Should().Be(0);

        // Powinny powstać pliki PDF
        File.Exists(Path.Combine(_testOutputDir, "test1.pdf")).Should().BeTrue();
        File.Exists(Path.Combine(_testOutputDir, "test2.pdf")).Should().BeTrue();
    }

    [Fact]
    public async Task CLI_Should_Process_Wildcard_Pattern()
    {
        // Arrange
        var xml1Path = Path.Combine(_testOutputDir, "invoice1.xml");
        var xml2Path = Path.Combine(_testOutputDir, "invoice2.xml");

        File.Copy(Path.Combine("TestData", "FakturaTEST017.xml"), xml1Path, overwrite: true);
        File.Copy(Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml"), xml2Path, overwrite: true);

        // Act
        var result = await RunCliAsync($"\"invoice*.xml\" --no-validate");

        // Assert
        result.ExitCode.Should().Be(0);
        File.Exists(Path.Combine(_testOutputDir, "invoice1.pdf")).Should().BeTrue();
        File.Exists(Path.Combine(_testOutputDir, "invoice2.pdf")).Should().BeTrue();
    }

    [Fact]
    public async Task CLI_Should_Skip_Validation_With_NoValidate_Option()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var testXmlPath = Path.Combine(_testOutputDir, "test.xml");
        File.Copy(xmlPath, testXmlPath, overwrite: true);

        // Act
        var result = await RunCliAsync($"\"{testXmlPath}\" --no-validate");

        // Assert - powinno się powieść mimo błędów walidacji
        result.ExitCode.Should().Be(0);
        File.Exists(Path.Combine(_testOutputDir, "test.pdf")).Should().BeTrue();
    }

    [Fact]
    public async Task CLI_Should_Fail_Validation_Without_NoValidate_Option()
    {
        // Arrange - faktura z błędami walidacji
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var testXmlPath = Path.Combine(_testOutputDir, "test.xml");
        File.Copy(xmlPath, testXmlPath, overwrite: true);

        // Act
        var result = await RunCliAsync($"\"{testXmlPath}\"");

        // Assert - powinna zwrócić błąd walidacji
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task CLI_Should_Use_Verbose_Logging()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var testXmlPath = Path.Combine(_testOutputDir, "test.xml");
        File.Copy(xmlPath, testXmlPath, overwrite: true);

        // Act
        var result = await RunCliAsync($"\"{testXmlPath}\" --verbose --no-validate");

        // Assert
        result.ExitCode.Should().Be(0);
        // Verbose logging powinien pokazać więcej szczegółów
        result.Output.Should().Contain("Przetwarzanie");
    }

    [Fact]
    public async Task CLI_Should_Accept_Custom_KSeF_Number()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var testXmlPath = Path.Combine(_testOutputDir, "test.xml");
        File.Copy(xmlPath, testXmlPath, overwrite: true);
        var customKSeF = "1234567890-20251029-123456789ABC-01";

        // Act
        var result = await RunCliAsync($"\"{testXmlPath}\" --ksef-number {customKSeF} --no-validate");

        // Assert
        result.ExitCode.Should().Be(0);
        File.Exists(Path.Combine(_testOutputDir, "test.pdf")).Should().BeTrue();
    }

    [Fact]
    public async Task CLI_Should_Use_Production_Mode_When_Specified()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var testXmlPath = Path.Combine(_testOutputDir, "test.xml");
        File.Copy(xmlPath, testXmlPath, overwrite: true);

        // Act
        var result = await RunCliAsync($"\"{testXmlPath}\" --production --no-validate");

        // Assert
        result.ExitCode.Should().Be(0);
        File.Exists(Path.Combine(_testOutputDir, "test.pdf")).Should().BeTrue();
    }

    [Fact]
    public async Task CLI_Should_Parse_KSeF_From_Filename_When_Option_Enabled()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var testXmlPath = Path.Combine(_testOutputDir, "faktura_6511153259-20251015-010020140418-0D.xml");
        File.Copy(xmlPath, testXmlPath, overwrite: true);

        // Act
        var result = await RunCliAsync($"\"{testXmlPath}\" --ksef-from-filename --no-validate");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("6511153259-20251015-010020140418-0D");
    }

    [Fact]
    public async Task CLI_Should_Return_Error_When_No_Files_Found()
    {
        // Act
        var result = await RunCliAsync("\"nonexistent*.xml\"");

        // Assert
        result.ExitCode.Should().Be(1);
        result.Error.Should().Contain("Nie znaleziono");
    }

    [Fact]
    public async Task CLI_Should_Generate_PDF_With_Default_Name()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var testXmlPath = Path.Combine(_testOutputDir, "invoice.xml");
        File.Copy(xmlPath, testXmlPath, overwrite: true);

        // Act - brak opcji --output, więc PDF powinien być invoice.pdf
        var result = await RunCliAsync($"\"{testXmlPath}\" --no-validate");

        // Assert
        result.ExitCode.Should().Be(0);
        File.Exists(Path.Combine(_testOutputDir, "invoice.pdf")).Should().BeTrue();
    }

    [Fact]
    public async Task CLI_Should_Show_Summary_For_Batch_Processing()
    {
        // Arrange
        var xml1Path = Path.Combine(_testOutputDir, "test1.xml");
        var xml2Path = Path.Combine(_testOutputDir, "test2.xml");

        File.Copy(Path.Combine("TestData", "FakturaTEST017.xml"), xml1Path, overwrite: true);
        File.Copy(Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml"), xml2Path, overwrite: true);

        // Act
        var result = await RunCliAsync($"\"{xml1Path}\" \"{xml2Path}\" --no-validate");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Podsumowanie");
        result.Output.Should().Contain("pomyślnie");
    }

    // Note: Test watch mode jest trudny do testowania automatycznie
    // ponieważ działa w nieskończonej pętli
    [Fact(Skip = "Watch mode wymaga ręcznego testowania")]
    public async Task CLI_Should_Support_Watch_Mode()
    {
        // Ten test jest pominięty bo watch mode działa w nieskończoność
        // i wymaga Ctrl+C do zakończenia
        await Task.CompletedTask;
    }

    // Note: Testy z certyfikatami wymagają rzeczywistego certyfikatu w Windows Store
    [Fact(Skip = "Requires certificate in Windows Store")]
    public async Task CLI_Should_Use_Certificate_From_Windows_Store()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var testXmlPath = Path.Combine(_testOutputDir, "test.xml");
        File.Copy(xmlPath, testXmlPath, overwrite: true);

        // Act
        var result = await RunCliAsync(
            $"\"{testXmlPath}\" --cert-thumbprint YOUR_CERT_THUMBPRINT --no-validate");

        // Assert
        result.ExitCode.Should().Be(0);
    }

    [Fact(Skip = "Requires certificate file")]
    public async Task CLI_Should_Use_Certificate_From_File()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var testXmlPath = Path.Combine(_testOutputDir, "test.xml");
        File.Copy(xmlPath, testXmlPath, overwrite: true);
        var certPath = "path/to/certificate.pfx";

        // Act
        var result = await RunCliAsync(
            $"\"{testXmlPath}\" --cert \"{certPath}\" --cert-password \"password\" --no-validate");

        // Assert
        result.ExitCode.Should().Be(0);
    }
}
