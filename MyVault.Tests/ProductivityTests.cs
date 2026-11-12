using MyVault.Core.Crypto;
using MyVault.Core.Models;
using MyVault.Core.Services;
using MyVault.Core.Storage;
using Xunit;

namespace MyVault.Tests;

public sealed class ProductivityTests
{
    [Fact]
    public void PasswordHistoryIsBoundedAndCanBeDisabled()
    {
        var entry = new VaultEntry { Password = "original" };
        for (int i = 0; i < 15; i++) EntryEditing.SetPassword(entry, "synthetic-" + i, 3);
        Assert.Equal(3, entry.PasswordHistory.Count);
        Assert.Equal("synthetic-13", entry.PasswordHistory[0].Password);
        EntryEditing.SetPassword(entry, entry.Password, 0);
        Assert.Empty(entry.PasswordHistory);
    }
    [Fact]
    public void CustomFieldsAndHistoryStayInsideAuthenticatedPayload()
    {
        var entry = new VaultEntry { Title = "API", Kind = EntryKind.ApiKey, Password = "current-key", CustomFields = [new() { Name = "Account", Value = "private-field", IsSecret = true }] };
        EntryEditing.SetPassword(entry, "new-key");
        var doc = new VaultDocument { Entries = [entry] };
        byte[] key = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        byte[] blob = VaultFile.Encrypt(doc, key, VaultHeader.CreateDefault());
        Assert.DoesNotContain("private-field", System.Text.Encoding.UTF8.GetString(blob));
        var restored = Assert.Single(VaultFile.Decrypt(blob, key).Entries);
        Assert.Equal("private-field", restored.CustomFields[0].Value);
        Assert.Equal("current-key", Assert.Single(restored.PasswordHistory).Password);
        Assert.Equal(EntryKind.ApiKey, restored.Kind);
    }
    [Fact]
    public void InvalidHistoryAndCustomFieldsAreRejected()
    {
        var doc = new VaultDocument { Entries = [new() { PasswordHistoryLimit = 11 }] };
        Assert.Throws<MyVault.Core.Exceptions.VaultException>(doc.Validate);
        doc.Entries[0].PasswordHistoryLimit = 5;
        doc.Entries[0].CustomFields.Add(new() { Name = "" });
        Assert.Throws<MyVault.Core.Exceptions.VaultException>(doc.Validate);
    }
    [Theory]
    [InlineData("otpauth://totp/Example:user?secret=JBSWY3DPEHPK3PXP&issuer=Example")]
    [InlineData("otpauth://totp/Example%3Auser?secret=jbswy3dpehpk3pxp&algorithm=SHA1&digits=6&period=30")]
    public void AuthenticatorLinksAreParsedLocally(string uri)
    {
        var setup = AuthenticatorSetup.Parse(uri);
        Assert.Equal("JBSWY3DPEHPK3PXP", setup.Secret);
        Assert.Equal("Example:user", setup.Account);
    }
    [Theory]
    [InlineData("otpauth://hotp/name?secret=JBSWY3DPEHPK3PXP")]
    [InlineData("otpauth://totp/name?secret=JBSWY3DPEHPK3PXP&digits=8")]
    [InlineData("otpauth://totp/name?secret=JBSWY3DPEHPK3PXP&period=60")]
    [InlineData("otpauth://totp/name?secret=JBSWY3DPEHPK3PXP&algorithm=SHA256")]
    [InlineData("otpauth://totp/name?secret=BAD&secret=JBSWY3DPEHPK3PXP")]
    [InlineData("https://example.com/?secret=JBSWY3DPEHPK3PXP")]
    public void UnsupportedOrAmbiguousAuthenticatorLinksAreRejected(string uri) => Assert.Throws<ArgumentException>(() => AuthenticatorSetup.Parse(uri));
    [Fact]
    public void BackupRetentionDoesNotDeleteUnrelatedFilesOrAnotherVault()
    {
        string dir = Path.Combine(Path.GetTempPath(), "myvault-retention-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            byte[] key = new byte[32];
            byte[] blob = VaultFile.Encrypt(new(), key, VaultHeader.CreateDefault());
            File.WriteAllText(Path.Combine(dir, "unrelated.vault"), "keep");
            for (int i = 0; i < 5; i++) EncryptedBackupStore.Write(blob, dir, "/synthetic/first.vault", 2);
            string other = EncryptedBackupStore.Write(blob, dir, "/synthetic/second.vault", 2);
            Assert.Equal(4, Directory.GetFiles(dir).Length);
            Assert.True(File.Exists(other));
            Assert.Equal("keep", File.ReadAllText(Path.Combine(dir, "unrelated.vault")));
            Assert.Equal(blob, File.ReadAllBytes(other));
        }
        finally { Directory.Delete(dir, true); }
    }
    [Fact]
    public void ScheduleHonorsOffDueAndFutureTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.False(EncryptedBackupStore.IsDue(null, now, 0));
        Assert.True(EncryptedBackupStore.IsDue(null, now, 15));
        Assert.True(EncryptedBackupStore.IsDue(now.AddMinutes(-15), now, 15));
        Assert.False(EncryptedBackupStore.IsDue(now.AddMinutes(-14), now, 15));
        Assert.False(EncryptedBackupStore.IsDue(now.AddMinutes(30), now, 15));
    }
    [Fact]
    public void SourceSpecificCsvAdaptersPreserveCredentials()
    {
        var chrome = CsvImporter.Analyze("name,url,username,password\nMail,https://example.com,user,secret", CsvSource.Chrome);
        Assert.Equal("secret", Assert.Single(chrome.Entries).Password);
        var bitwarden = CsvImporter.Analyze("type,name,login_uri,login_username,login_password,login_totp\nlogin,Mail,https://example.com,user,secret,JBSWY3DPEHPK3PXP", CsvSource.Bitwarden);
        Assert.Equal("JBSWY3DPEHPK3PXP", Assert.Single(bitwarden.Entries).TotpSecret);
        var one = CsvImporter.Analyze("Title,Url,Username,Password,Notes\nAccount,https://example.com,user,secret,note", CsvSource.OnePassword);
        Assert.Equal("note", Assert.Single(one.Entries).Notes);
    }
    [Fact]
    public void V1VaultUpgradesOnSaveAndPreservesAReadableBackup()
    {
        string dir = Path.Combine(Path.GetTempPath(), "myvault-migration-" + Guid.NewGuid()); Directory.CreateDirectory(dir);
        try
        {
            string path = Path.Combine(dir, "old.dat");
            var header = VaultHeader.CreateDefault(); header.FormatVersion = 1; header.MemoryKiB = 64; header.Iterations = 1; header.Parallelism = 1;
            byte[] key = KeyDerivation.DeriveKey("synthetic", header.Salt, 64, 1, 1);
            VaultFile.WriteAtomic(path, VaultFile.Encrypt(new VaultDocument { Version = 1 }, key, header));
            using (var repo = VaultRepository.Open(path, "synthetic")) repo.Update(d => d.Entries.Add(new() { Title = "New" }));
            Assert.Equal(2, VaultFile.ReadHeader(VaultFile.Read(path)).FormatVersion);
            Assert.Equal(1, VaultFile.ReadHeader(VaultFile.Read(path + ".bak.1")).FormatVersion);
            Assert.Equal(1, VaultFile.Decrypt(VaultFile.Read(path + ".bak.1"), key).Version);
        }
        finally { Directory.Delete(dir, true); }
    }
}
