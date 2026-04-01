namespace SnippingTool.Services;

public interface IGifExportService
{
    Task ExportAsync(string inputPath, string outputPath, int fps, CancellationToken ct = default);
}
