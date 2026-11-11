using System.Security.Cryptography;

namespace MyVault.Core.Services;

public static class PasswordGenerator
{
    private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
    private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string DigitChars = "0123456789";
    private const string SymbolChars = "!@#$%^&*()-_=+[]{};:,.?";
    private const string Ambiguous = "Il1O0o";

    public static string Generate(
        int length = 20,
        bool includeUpper = true,
        bool includeLower = true,
        bool includeDigits = true,
        bool includeSymbols = true,
        bool avoidAmbiguous = false)
    {
        if (length < 4 || length > 256)
            throw new ArgumentOutOfRangeException(nameof(length));

        var sets = new List<string>();
        if (includeLower) sets.Add(Filter(LowerChars, avoidAmbiguous));
        if (includeUpper) sets.Add(Filter(UpperChars, avoidAmbiguous));
        if (includeDigits) sets.Add(Filter(DigitChars, avoidAmbiguous));
        if (includeSymbols) sets.Add(SymbolChars);

        if (sets.Count == 0)
            throw new ArgumentException("At least one character class must be enabled.");

        string pool = string.Concat(sets);
        var chars = new char[length];

        // Guarantee one from each enabled set.
        for (int i = 0; i < sets.Count && i < length; i++)
            chars[i] = sets[i][RandomInt(sets[i].Length)];

        for (int i = sets.Count; i < length; i++)
            chars[i] = pool[RandomInt(pool.Length)];

        // Fisher–Yates shuffle so the guaranteed chars aren't always at the front.
        for (int i = length - 1; i > 0; i--)
        {
            int j = RandomInt(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static string Filter(string source, bool avoidAmbiguous) =>
        avoidAmbiguous
            ? new string(source.Where(c => !Ambiguous.Contains(c)).ToArray())
            : source;

    // Rejection sampling avoids modulo bias.
    private static int RandomInt(int exclusiveMax)
    {
        uint range = (uint)exclusiveMax;
        uint limit = uint.MaxValue - (uint.MaxValue % range);
        Span<byte> buf = stackalloc byte[4];
        uint value;
        do
        {
            RandomNumberGenerator.Fill(buf);
            value = BitConverter.ToUInt32(buf);
        } while (value >= limit);
        return (int)(value % range);
    }
}