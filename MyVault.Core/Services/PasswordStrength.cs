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
    /// <summary>Display heuristic, not measured entropy or a breach check. Repetition is penalized.</summary>
    public static double EstimateBits(string? password)
    {
        if (string.IsNullOrEmpty(password)) return 0;

        int pool = 0;
        if (password.Any(char.IsLower)) pool += 26;
        if (password.Any(char.IsUpper)) pool += 26;
        if (password.Any(char.IsDigit)) pool += 10;
        if (password.Any(c=>char.IsLetter(c) && !char.IsLower(c) && !char.IsUpper(c))) pool += 64;
        if (password.Any(c => !char.IsLetterOrDigit(c))) pool += 32;
        if (pool == 0) pool = 1;

        double bits=password.Length*Math.Log2(pool);
        // Avoid calling long repeated runs "strong", including uncased Persian letters.
        for(int period=1;period<=Math.Min(64,password.Length/2);period++)
        {
            if(password.Length%period!=0) continue;
            bool repeated=true;
            for(int i=period;i<password.Length;i++) if(password[i]!=password[i%period]) {repeated=false;break;}
            if(repeated) {bits=Math.Min(bits,period*Math.Log2(pool)+Math.Log2(password.Length));break;}
        }
        return bits;
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