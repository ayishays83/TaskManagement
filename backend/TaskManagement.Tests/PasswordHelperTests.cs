using TaskManagement.Api.Services;
using Xunit;

namespace TaskManagement.Tests;

public class PasswordHelperTests
{
    [Fact]
    public void Hash_DoesNotContainThePlainPassword()
    {
        var hash = PasswordHelper.Hash("Secret@123");

        Assert.DoesNotContain("Secret@123", hash);
        Assert.Contains(".", hash);
    }

    [Fact]
    public void Hash_UsesARandomSalt_SoTwoHashesDiffer()
    {
        var first = PasswordHelper.Hash("Secret@123");
        var second = PasswordHelper.Hash("Secret@123");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForTheCorrectPassword()
    {
        var hash = PasswordHelper.Hash("Secret@123");

        Assert.True(PasswordHelper.Verify("Secret@123", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForAWrongPassword()
    {
        var hash = PasswordHelper.Hash("Secret@123");

        Assert.False(PasswordHelper.Verify("secret@123", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("a.b.c")]
    [InlineData("!!!.???")]
    public void Verify_ReturnsFalse_ForMalformedStoredHashes(string stored)
    {
        Assert.False(PasswordHelper.Verify("Secret@123", stored));
    }
}