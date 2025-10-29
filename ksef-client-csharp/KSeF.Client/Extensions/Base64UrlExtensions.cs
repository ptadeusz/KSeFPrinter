#if NET9_0_OR_GREATER
using System.Buffers.Text;
#endif

namespace KSeF.Client.Extensions;

    public static class Base64UrlExtensions
{
    public static string EncodeBase64UrlToString(this byte[] blob)
    {
#if NET9_0_OR_GREATER
                return Base64Url.EncodeToString(blob);
#else
        // RFC 4648 §5: Base64url = Base64 z zamianą znaków i bez paddingu.
        return Convert.ToBase64String(blob).TrimEnd('=').Replace('+', '-').Replace('/', '_');
#endif
    }
}

