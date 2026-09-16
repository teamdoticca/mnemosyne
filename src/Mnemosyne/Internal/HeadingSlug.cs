using System.Text;

namespace Mnemosyne.Internal;

internal static class HeadingSlug
{
    public static string FromHeadingText(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text.Trim().ToLowerInvariant())
        {
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(ch);
            }
            else if (ch is ' ' or '-' or '_')
            {
                if (sb.Length > 0 && sb[^1] != '-')
                {
                    sb.Append('-');
                }
            }
        }

        var slug = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? "section" : slug;
    }
}
