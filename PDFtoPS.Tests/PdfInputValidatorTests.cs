namespace PDFtoPS.Tests;

public class PdfInputValidatorTests
{
    [Fact]
    public void TryValidate_ReturnsFalse_WhenSignatureMissing()
    {
        string dir = Path.Combine(Path.GetTempPath(), "pdf2ps-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            string file = Path.Combine(dir, "bad.pdf");
            File.WriteAllText(file, "hello world");

            PdfInputValidator validator = new PdfInputValidator(new AppLogger(dir));
            bool ok = validator.TryValidate(file, out string error);

            Assert.False(ok);
            Assert.Contains("сигнатуры", error);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void TryValidate_ReturnsFalse_WhenEncryptedMarkerDetected()
    {
        string dir = Path.Combine(Path.GetTempPath(), "pdf2ps-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            string file = Path.Combine(dir, "enc.pdf");
            File.WriteAllText(file, "%PDF-1.7\n1 0 obj\n<< /Encrypt true >>\nendobj\n");

            PdfInputValidator validator = new PdfInputValidator(new AppLogger(dir));
            bool ok = validator.TryValidate(file, out string error);

            Assert.False(ok);
            Assert.Contains("защищённый", error);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
