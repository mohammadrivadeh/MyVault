namespace MyVault.Core.Services;

public enum PasswordStrengthLevel
{
    Empty,
    Weak,
    Fair,
    Strong,
    VeryStrong
}

public static class PasswordStrength
{
    /// <summary>Rough Shannon-style entropy estimate: length × log2(charset size).</summary>
    public static double EstimateBits(string? password)
    {
        if (string.IsNullOrEmpty(password)) return 0;

        int pool = 0;
        if (password.Any(char.IsLower)) pool += 26;
        if (password.Any(char.IsUpper)) pool += 26;
        if (password.Any(char.IsDigit)) pool += 10;
        if (password.Any(c => !char.IsLetterOrDigit(c))) pool += 32;
        if (pool == 0) pool = 1;

        return password.Length * Math.Log2(pool);
    }

    public static PasswordStrengthLevel Categorize(string? password)
    {
        double bits = EstimateBits(password);
        return bits switch
        {
            <= 0 => PasswordStrengthLevel.Empty,
            < 40 => PasswordStrengthLevel.Weak,
            < 60 => PasswordStrengthLevel.Fair,
            < 80 => PasswordStrengthLevel.Strong,
            _ => PasswordStrengthLevel.VeryStrong
        };
    }

    public static string Label(PasswordStrengthLevel level) => level switch
    {
        PasswordStrengthLevel.Empty => string.Empty,
        PasswordStrengthLevel.Weak => "Weak",
        PasswordStrengthLevel.Fair => "Fair",
        PasswordStrengthLevel.Strong => "Strong",
        PasswordStrengthLevel.VeryStrong => "Very strong",
        _ => string.Empty
    };
}