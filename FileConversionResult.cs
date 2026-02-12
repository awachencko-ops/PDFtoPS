namespace PDFtoPS;

internal sealed class FileConversionResult
{
    public string FileName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}
