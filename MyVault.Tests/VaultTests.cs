using MyVault.Core.Crypto;
using MyVault.Core.Exceptions;
using MyVault.Core.Models;
using MyVault.Core.Services;
using MyVault.Core.Storage;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace MyVault.Tests;

public sealed class VaultTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "myvault-tests-" + Guid.NewGuid());
    public VaultTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, true);
    private string VaultPath => Path.Combine(_dir, "vault.dat");

    [Fact]
    public void RepositoryRoundTripRekeyAndLock()
    {
        using (var repo = VaultRepository.CreateNew(VaultPath, "synthetic-old-password"))
        {
            repo.Update(d => d.Entries.Add(new VaultEntry { Title = "Test", Password = "synthetic-value" }));
            Assert.False(repo.VerifyPassword("wrong"));
            repo.ChangeMasterPassword("synthetic-new-password");
        }
        Assert.Throws<VaultException>(() => VaultRepository.Open(VaultPath, "synthetic-old-password"));
        using var opened = VaultRepository.Open(VaultPath, "synthetic-new-password");
        var entry = Assert.Single(opened.Document.Entries);
        Assert.Equal("synthetic-value", entry.Password);
        opened.Lock();
        Assert.False(opened.IsUnlocked);
        Assert.Empty(opened.Document.Entries);
        Assert.Equal("", entry.Password);
    }

    [Fact]
    public void FailedEditRestoresMemoryAndDisk()
    {
        using var repo = VaultRepository.CreateNew(VaultPath, "synthetic-password");
        repo.Update(d => d.Entries.Add(new VaultEntry { Title = "Original" }));
        byte[] original = File.ReadAllBytes(VaultPath);
        Assert.Throws<InvalidOperationException>(() => repo.Update(d => { d.Entries.Clear(); throw new InvalidOperationException("Injected failure"); }));
        Assert.Equal("Original", Assert.Single(repo.Document.Entries).Title);
        Directory.CreateDirectory(VaultPath + ".obstruction");
        File.Move(VaultPath, VaultPath + ".saved");
        Directory.CreateDirectory(VaultPath);
        Assert.Throws<VaultException>(() => repo.Update(d => d.Entries.Clear()));
        Assert.Single(repo.Document.Entries);
        Directory.Delete(VaultPath);
        File.Move(VaultPath + ".saved", VaultPath);
        Assert.Equal(original, File.ReadAllBytes(VaultPath));
    }

    [Fact]
    public void ExclusiveLeasePreventsSecondWriterAndReleasesAfterDispose()
    {
        using (var repo = VaultRepository.CreateNew(VaultPath, "synthetic-password"))
            Assert.Throws<VaultException>(() => VaultRepository.Open(VaultPath, "synthetic-password"));
        using var reopened = VaultRepository.Open(VaultPath, "synthetic-password");
        Assert.True(reopened.IsUnlocked);
    }

    [Fact]
    public void RestoreBackupPreservesPreviousStateAndRejectsCorruption()
    {
        using var repo = VaultRepository.CreateNew(VaultPath, "synthetic-password");
        repo.Update(d => d.Entries.Add(new VaultEntry { Title = "Original" }));
        byte[] backup = File.ReadAllBytes(VaultPath);
        repo.Update(d => d.Entries.Clear());
        repo.RestoreBackup(backup);
        Assert.Equal("Original", Assert.Single(repo.Document.Entries).Title);
        backup[^1] ^= 1;
        Assert.Throws<VaultException>(() => repo.RestoreBackup(backup));
        Assert.Single(repo.Document.Entries);
    }

    [Fact]
    public void AdditiveTrashAndTotpFieldsRoundTrip()
    {
        byte[] key = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var doc = new VaultDocument { Entries = [new VaultEntry { Title = "Deleted", DeletedAt = DateTimeOffset.UtcNow, TotpSecret = "JBSWY3DPEHPK3PXP" }] };
        var restored = VaultFile.Decrypt(VaultFile.Encrypt(doc, key, VaultHeader.CreateDefault()), key);
        Assert.NotNull(restored.Entries[0].DeletedAt);
        Assert.Equal("JBSWY3DPEHPK3PXP", restored.Entries[0].TotpSecret);
    }

    [Fact]
    public void OriginalV1PayloadWithoutNewFieldsStillOpens()
    {
        var header = VaultHeader.CreateDefault();
        header.FormatVersion = 1;
        byte[] key = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        byte[] headerJson = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(header, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        byte[] aad = new byte[9 + headerJson.Length];
        System.Text.Encoding.ASCII.GetBytes("MVLT1").CopyTo(aad, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(aad.AsSpan(5, 4), headerJson.Length);
        headerJson.CopyTo(aad, 9);
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("{\"version\":1,\"entries\":[{\"title\":\"Legacy\",\"username\":\"user\",\"password\":\"test\",\"url\":\"\",\"notes\":\"\",\"tags\":[]}],\"categories\":[]}");
        var (nonce, ciphertext, tag) = Aead.Encrypt(key, payload, aad);
        byte[] blob = [.. aad, .. nonce, .. ciphertext, .. tag];
        var entry = Assert.Single(VaultFile.Decrypt(blob, key).Entries);
        Assert.Equal("Legacy", entry.Title); Assert.Null(entry.DeletedAt); Assert.Null(entry.TotpSecret);
    }

    [Fact]
    public void OversizedVaultIsRejectedBeforeAllocation()
    {
        using (var stream = File.Create(VaultPath)) stream.SetLength(VaultFile.MaxFileBytes + 1);
        Assert.Throws<VaultException>(() => VaultFile.Read(VaultPath));
    }

    [Fact]
    public void AtomicWritesRotateBackupsAndLeaveNoTemporaryFiles()
    {
        for (int i = 0; i < 8; i++) VaultFile.WriteAtomic(VaultPath, [(byte)i], maxBackups: 3);
        Assert.Equal(new byte[] { 7 }, File.ReadAllBytes(VaultPath));
        Assert.Equal(new byte[] { 6 }, File.ReadAllBytes(VaultPath + ".bak.1"));
        Assert.Equal(new byte[] { 4 }, File.ReadAllBytes(VaultPath + ".bak.3"));
        Assert.False(File.Exists(VaultPath + ".bak.4"));
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    [Fact]
    public void AuthenticatedHeaderCiphertextAndTagRejectTampering()
    {
        var header = VaultHeader.CreateDefault();
        byte[] key = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        byte[] blob = VaultFile.Encrypt(new VaultDocument(), key, header);
        foreach (int position in new[] { 10, blob.Length / 2, blob.Length - 1 })
        {
            byte[] damaged = (byte[])blob.Clone(); damaged[position] ^= 1;
            Assert.Throws<VaultException>(() => VaultFile.Decrypt(damaged, key));
        }
        Assert.Throws<VaultException>(() => VaultFile.ReadHeader(blob.AsSpan(0, 7)));
    }

    [Theory]
    [InlineData(1048577, 3, 4)]
    [InlineData(262144, 11, 4)]
    [InlineData(262144, 3, 17)]
    [InlineData(8, 1, 4)]
    public void RejectsUntrustedKdfCostsBeforeDerivation(int memory, int iterations, int lanes)
    {
        var h = VaultHeader.CreateDefault(); h.MemoryKiB = memory; h.Iterations = iterations; h.Parallelism = lanes;
        Assert.Throws<VaultException>(h.Validate);
    }

    [Fact]
    public void DocumentRejectsNullCollectionsAndDuplicateIds()
    {
        var d = new VaultDocument { Entries = null! }; Assert.Throws<VaultException>(d.Validate);
        var e = new VaultEntry(); d = new VaultDocument { Entries = [e, e] }; Assert.Throws<VaultException>(d.Validate);
    }

    [Fact]
    public void CsvRoundTripPreservesQuotesCommasAndNewlines()
    {
        string[] fields = ["Mail, work", "a\"b", "line1\r\nline2", ""];
        var parsed = CsvParser.Parse(CsvWriter.Write([fields]));
        Assert.Equal(fields, Assert.Single(parsed));
        Assert.Throws<InvalidDataException>(() => CsvParser.Parse("title\n\"unterminated"));
    }

    [Fact]
    public void CsvImportUnderstandsAliasesAndSkipsMissingTitles()
    {
        var result = CsvImporter.Analyze("name,email,pass,tags\nMail,user,test,work\n,other,test,personal");
        Assert.Single(result.Entries); Assert.Equal(1, result.Skipped); Assert.Equal("user", result.Entries[0].Username);
    }

    [Fact]
    public void GeneratorHonorsLengthClassesAndAmbiguousOption()
    {
        for (int i = 0; i < 30; i++)
        {
            string p = PasswordGenerator.Generate(24, avoidAmbiguous: true);
            Assert.Equal(24, p.Length); Assert.Contains(p, char.IsLower); Assert.Contains(p, char.IsUpper);
            Assert.Contains(p, char.IsDigit); Assert.Contains(p, c => !char.IsLetterOrDigit(c));
            Assert.DoesNotContain(p, c => "Il1O0o".Contains(c));
        }
        Assert.Throws<ArgumentException>(() => PasswordGenerator.Generate(20, false, false, false, false));
    }

    [Theory]
    [InlineData(59, "287082")]
    [InlineData(1111111109, "081804")]
    [InlineData(1234567890, "005924")]
    public void TotpMatchesRfc6238Vectors(long seconds, string expected) =>
        Assert.Equal(expected, Totp.Generate("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", DateTimeOffset.FromUnixTimeSeconds(seconds)));
}
