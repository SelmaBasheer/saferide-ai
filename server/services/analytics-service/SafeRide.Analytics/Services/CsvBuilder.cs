using System.Globalization;
using System.Text;

namespace SafeRide.Analytics.Services;

/// <summary>
/// A small RFC 4180 writer. Sections separated by blank lines are not strictly
/// valid CSV — but the super admin asked for one file covering everything, and
/// four separate downloads would be worse. Excel reads it correctly.
/// </summary>
public sealed class CsvBuilder
{
    private readonly StringBuilder _sb = new();

    public CsvBuilder Section(string title)
    {
        if (_sb.Length > 0)
        {
            _sb.AppendLine();
        }

        _sb.AppendLine(Escape(title));
        return this;
    }

    public CsvBuilder Row(params object?[] values)
    {
        _sb.AppendLine(string.Join(",", values.Select(Format).Select(Escape)));
        return this;
    }

    public byte[] ToBytes()
    {
        // The BOM is three bytes and the difference between a reviewer seeing
        // "Peevees" and seeing mojibake: Excel on Windows assumes the system
        // code page for a CSV without one, which mangles anything non-ASCII.
        var text = _sb.ToString();
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text)];
    }

    private static string Format(object? value) =>
        value switch
        {
            null => string.Empty,
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            decimal m => m.ToString("0.00", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    /// A school called <c>St. Mary's, Old Gate</c> breaks a naive join. Quote
    /// anything containing a comma, a quote or a newline, and double the quotes.
    private static string Escape(string value)
    {
        if (!value.Any(c => c is ',' or '"' or '\r' or '\n'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
