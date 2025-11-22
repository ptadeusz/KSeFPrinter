using Microsoft.Extensions.Logging;
using KSeFPrinter.CLI.Models;

namespace KSeFPrinter.CLI;

public static class FileProcessingHelpers
{
    /// <summary>
    /// Checks if file size is stable (not growing)
    /// </summary>
    public static async Task<bool> IsFileStableAsync(string filePath, int delayMs, ILogger logger)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                logger.LogWarning("File disappeared: {FileName}", Path.GetFileName(filePath));
                return false;
            }

            var size1 = new FileInfo(filePath).Length;
            await Task.Delay(delayMs);

            if (!File.Exists(filePath))
            {
                logger.LogWarning("File disappeared during check: {FileName}", Path.GetFileName(filePath));
                return false;
            }

            var size2 = new FileInfo(filePath).Length;

            if (size1 != size2)
            {
                logger.LogWarning("File still growing: {FileName} ({Size1} → {Size2} bytes)",
                    Path.GetFileName(filePath), size1, size2);
                return false;
            }

            logger.LogDebug("File size stable: {FileName} ({Size} bytes)",
                Path.GetFileName(filePath), size2);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cannot access file: {FileName}", Path.GetFileName(filePath));
            return false;
        }
    }

    /// <summary>
    /// Moves XML, PDF, and optionally UPO files to target folder
    /// </summary>
    public static void MoveProcessedFiles(
        string xmlPath,
        string? pdfPath,
        string targetFolder,
        ILogger logger,
        string? errorMessage = null)
    {
        try
        {
            // Ensure target folder exists
            Directory.CreateDirectory(targetFolder);

            var xmlFileName = Path.GetFileName(xmlPath);
            var xmlBaseName = Path.GetFileNameWithoutExtension(xmlPath);
            var xmlDir = Path.GetDirectoryName(xmlPath);
            var targetXmlPath = Path.Combine(targetFolder, xmlFileName);

            // Move XML (always)
            if (File.Exists(xmlPath))
            {
                File.Move(xmlPath, targetXmlPath, overwrite: true);
                logger.LogInformation("  → Moved XML to: {TargetPath}", targetXmlPath);
            }

            // Move PDF (if exists)
            if (!string.IsNullOrEmpty(pdfPath) && File.Exists(pdfPath))
            {
                var pdfFileName = Path.GetFileName(pdfPath);
                var targetPdfPath = Path.Combine(targetFolder, pdfFileName);
                File.Move(pdfPath, targetPdfPath, overwrite: true);
                logger.LogInformation("  → Moved PDF to: {TargetPath}", targetPdfPath);
            }
            else
            {
                logger.LogWarning("  ⚠ PDF not generated, moving XML without PDF");

                // Create error marker file if error message provided
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    var errorMarkerPath = Path.Combine(targetFolder, $"{xmlBaseName}_PDF_ERROR.txt");
                    File.WriteAllText(errorMarkerPath,
                        $"PDF generation failed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\nError:\n{errorMessage}");
                    logger.LogInformation("  → Created error marker: {ErrorMarker}", Path.GetFileName(errorMarkerPath));
                }
            }

            // Check for UPO file (same base name + _UPO.xml)
            if (xmlDir != null)
            {
                var upoPath = Path.Combine(xmlDir, $"{xmlBaseName}_UPO.xml");
                if (File.Exists(upoPath))
                {
                    var targetUpoPath = Path.Combine(targetFolder, Path.GetFileName(upoPath));
                    File.Move(upoPath, targetUpoPath, overwrite: true);
                    logger.LogInformation("  → Moved UPO to: {TargetPath}", targetUpoPath);
                }
            }

            logger.LogInformation("✓ Files moved to: {TargetFolder}", targetFolder);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to move files to {TargetFolder}", targetFolder);
            throw;
        }
    }

    /// <summary>
    /// Determines target folder based on source folder path
    /// </summary>
    public static string? GetTargetFolder(string sourcePath, PrinterConfiguration config)
    {
        var sourceFullPath = Path.GetFullPath(sourcePath);
        var sourceDir = Path.GetDirectoryName(sourceFullPath);

        if (sourceDir == null)
            return null;

        // Check if it's incoming folder
        if (config.Folders.IncomingSource != null &&
            sourceDir.StartsWith(Path.GetFullPath(config.Folders.IncomingSource), StringComparison.OrdinalIgnoreCase))
        {
            if (!config.AutoPrint.EnabledForIncoming)
                return null;

            // Preserve NIP subfolder structure
            var relativePath = Path.GetRelativePath(
                Path.GetFullPath(config.Folders.IncomingSource),
                sourceDir);

            return config.Folders.IncomingTarget != null
                ? Path.Combine(config.Folders.IncomingTarget, relativePath)
                : null;
        }

        // Check if it's processed folder
        if (config.Folders.ProcessedSource != null &&
            sourceDir.StartsWith(Path.GetFullPath(config.Folders.ProcessedSource), StringComparison.OrdinalIgnoreCase))
        {
            if (!config.AutoPrint.EnabledForProcessed)
                return null;

            // Preserve NIP subfolder structure
            var relativePath = Path.GetRelativePath(
                Path.GetFullPath(config.Folders.ProcessedSource),
                sourceDir);

            return config.Folders.ProcessedTarget != null
                ? Path.Combine(config.Folders.ProcessedTarget, relativePath)
                : null;
        }

        // Unknown folder - no target
        return null;
    }
}
