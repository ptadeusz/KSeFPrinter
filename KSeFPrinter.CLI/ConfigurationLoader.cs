using Microsoft.Extensions.Configuration;
using KSeFPrinter.CLI.Models;

namespace KSeFPrinter.CLI;

public static class ConfigurationLoader
{
    public static PrinterConfiguration LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "KSEF_PRINTER_");

        var configuration = builder.Build();

        var printerConfig = new PrinterConfiguration();
        configuration.Bind(printerConfig);

        return printerConfig;
    }
}
