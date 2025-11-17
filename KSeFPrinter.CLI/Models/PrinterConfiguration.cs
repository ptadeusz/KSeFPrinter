namespace KSeFPrinter.CLI.Models;

public class PrinterConfiguration
{
    public FoldersConfiguration Folders { get; set; } = new();
    public AutoPrintConfiguration AutoPrint { get; set; } = new();
    public FileProcessingConfiguration FileProcessing { get; set; } = new();
}

public class FoldersConfiguration
{
    /// <summary>
    /// Source folder for incoming invoices (purchases from KSeF)
    /// </summary>
    public string? IncomingSource { get; set; }

    /// <summary>
    /// Target folder for processed incoming invoices (with PDF)
    /// </summary>
    public string? IncomingTarget { get; set; }

    /// <summary>
    /// Source folder for processed invoices (sales sent to KSeF)
    /// </summary>
    public string? ProcessedSource { get; set; }

    /// <summary>
    /// Target folder for printed processed invoices (with PDF)
    /// </summary>
    public string? ProcessedTarget { get; set; }
}

public class AutoPrintConfiguration
{
    /// <summary>
    /// Enable auto-printing for incoming invoices
    /// </summary>
    public bool EnabledForIncoming { get; set; } = true;

    /// <summary>
    /// Enable auto-printing for processed (sent) invoices
    /// </summary>
    public bool EnabledForProcessed { get; set; } = true;

    /// <summary>
    /// Do NOT enable auto-printing for error invoices (only on-demand via API)
    /// </summary>
    public bool EnabledForErrors { get; set; } = false;
}

public class FileProcessingConfiguration
{
    /// <summary>
    /// Delay in milliseconds to check if file size is stable (default: 2000ms)
    /// </summary>
    public int StabilityCheckDelayMs { get; set; } = 2000;

    /// <summary>
    /// Initial delay after FileSystemWatcher event (default: 500ms)
    /// </summary>
    public int WatcherDelayMs { get; set; } = 500;

    /// <summary>
    /// Skip processing if PDF already exists alongside XML
    /// </summary>
    public bool SkipIfPdfExists { get; set; } = true;

    /// <summary>
    /// Move files to target folder after PDF generation (default: true)
    /// </summary>
    public bool MoveAfterProcessing { get; set; } = true;
}
