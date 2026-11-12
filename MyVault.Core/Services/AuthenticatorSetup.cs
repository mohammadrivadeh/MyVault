namespace MyVault.Core.Services;

public sealed record AuthenticatorSetup(string Secret, string Account, string Issuer)
{
    public static AuthenticatorSetup Parse(string text)
    {
        if (text.Length > 4096 || !Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme != "otpauth" || uri.Host != "totp" || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0)
            throw new ArgumentException("Use a TOTP authenticator setup link.");
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = part.Split('=', 2);
            string key = Uri.UnescapeDataString(pair[0]);
            if (pair.Length != 2 || !values.TryAdd(key, Uri.UnescapeDataString(pair[1]))) throw new ArgumentException("Duplicate or malformed setup parameter.");
        }
        if (values.GetValueOrDefault("algorithm", "SHA1").ToUpperInvariant() != "SHA1" ||
            values.GetValueOrDefault("digits", "6") != "6" || values.GetValueOrDefault("period", "30") != "30")
            throw new ArgumentException("Only SHA-1, six digits and 30-second TOTP are supported.");
        string secret = values.GetValueOrDefault("secret", "").Replace(" ", "").ToUpperInvariant();
        if (!Totp.IsValid(secret)) throw new ArgumentException("The setup link contains an invalid Base32 key.");
        string label = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        return new(secret, label, values.GetValueOrDefault("issuer", ""));
    }
}
