using System.Net;
using System.Net.Sockets;

namespace Rezepte.Web.Security;

/// <summary>
/// Validates user supplied URLs before the server fetches them so that requests cannot be
/// redirected to loopback, link local or private network addresses.
/// </summary>
public static class RemoteUrlGuard
{
    private static readonly int[] AllowedPorts = [80, 443];

    /// <summary>
    /// Validates the given URL.
    /// </summary>
    /// <param name="url">URL provided by a user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple with success flag, an error message for the caller and the parsed URI.</returns>
    public static async Task<(bool ok, string? error, Uri? uri)> TryValidateAsync(string? url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return (false, "Invalid URL. Only http(s) URLs are supported.", null);
        }

        if (!AllowedPorts.Contains(uri.Port))
        {
            return (false, "Invalid URL. Only the standard http(s) ports are supported.", null);
        }

        IPAddress[] addresses;
        if (IPAddress.TryParse(uri.DnsSafeHost, out var literalAddress))
        {
            addresses = [literalAddress];
        }
        else
        {
            try
            {
                addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, ct).ConfigureAwait(false);
            }
            catch (SocketException)
            {
                return (false, "The host name could not be resolved.", null);
            }
            catch (ArgumentException)
            {
                return (false, "The host name is invalid.", null);
            }
        }

        if (addresses.Length == 0)
        {
            return (false, "The host name could not be resolved.", null);
        }

        if (addresses.Any(IsInternal))
        {
            return (false, "URLs pointing to internal network addresses are not allowed.", null);
        }

        return (true, null, uri);
    }

    private static bool IsInternal(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.IsIPv6Multicast
                || address.Equals(IPAddress.IPv6Any)
                || (address.GetAddressBytes()[0] & 0xFE) == 0xFC;
        }

        var octets = address.GetAddressBytes();
        return octets[0] switch
        {
            0 => true,
            10 => true,
            127 => true,
            100 => octets[1] >= 64 && octets[1] <= 127,
            169 => octets[1] == 254,
            172 => octets[1] >= 16 && octets[1] <= 31,
            192 => octets[1] == 168,
            >= 224 => true,
            _ => false
        };
    }
}
