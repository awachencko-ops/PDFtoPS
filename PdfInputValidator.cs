using System.Text;

namespace PDFtoPS;

internal sealed class PdfInputValidator
{
    private readonly AppLogger logger;

    public PdfInputValidator(AppLogger logger)
    {
        this.logger = logger;
    }

    public bool TryValidate(string inputPath, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            FileInfo fileInfo = new FileInfo(inputPath);
            if (!fileInfo.Exists)
            {
                errorMessage = "Входной PDF-файл не найден.";
                return false;
            }

            if (fileInfo.Length < 8)
            {
                errorMessage = "Файл слишком мал и не похож на валидный PDF.";
                return false;
            }

            using FileStream stream = File.OpenRead(inputPath);

            Span<byte> header = stackalloc byte[5];
            int readHeader = stream.Read(header);
            if (readHeader < 5 || header[0] != (byte)'%' || header[1] != (byte)'P' || header[2] != (byte)'D' || header[3] != (byte)'F' || header[4] != (byte)'-')
            {
                errorMessage = "Файл не распознан как PDF (нет сигнатуры %PDF-).";
                return false;
            }

            long sampleSize = Math.Min(fileInfo.Length, 262144);
            stream.Position = 0;
            byte[] buffer = new byte[sampleSize];
            int sampleRead = stream.Read(buffer, 0, buffer.Length);
            string sample = Encoding.ASCII.GetString(buffer, 0, sampleRead);

            if (sample.Contains("/Encrypt", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "PDF выглядит как защищённый (/Encrypt). Конвертация может быть невозможна.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.Error("PDF pre-validation failed", ("inputPath", inputPath), ("error", ex.Message));
            errorMessage = "Ошибка предварительной проверки PDF. Подробности в логах.";
            return false;
        }
    }
}
