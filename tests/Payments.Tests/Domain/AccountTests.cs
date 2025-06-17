using Payments.Domain.Entities;
using Xunit;

namespace Payments.Tests.Domain;

public class AccountTests
{
    [Fact]
    public void Account_Creation_Should_Set_Correct_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var account = new Account(userId);

        // Assert
        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal(userId, account.UserId);
        Assert.Equal(0, account.Balance);
        Assert.True(account.CreatedAt <= DateTime.UtcNow);
        Assert.True(account.UpdatedAt <= DateTime.UtcNow);
        Assert.Equal(1, account.Version);
    }

    [Fact]
    public void TopUp_Should_Increase_Balance_And_Version()
    {
        // Arrange
        var account = new Account(Guid.NewGuid());
        var initialVersion = account.Version;
        var amount = 100.50m;

        // Act
        account.TopUp(amount);

        // Assert
        Assert.Equal(amount, account.Balance);
        Assert.Equal(initialVersion + 1, account.Version);
        Assert.True(account.UpdatedAt >= account.CreatedAt);
    }

    [Fact]
    public void TopUp_With_Zero_Amount_Should_Throw_Exception()
    {
        // Arrange
        var account = new Account(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => account.TopUp(0));
    }

    [Fact]
    public void TopUp_With_Negative_Amount_Should_Throw_Exception()
    {
        // Arrange
        var account = new Account(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => account.TopUp(-10));
    }

    [Fact]
    public void TryWithdraw_With_Sufficient_Balance_Should_Succeed()
    {
        // Arrange
        var account = new Account(Guid.NewGuid());
        account.TopUp(100);
        var initialVersion = account.Version;

        // Act
        var result = account.TryWithdraw(50);

        // Assert
        Assert.True(result);
        Assert.Equal(50, account.Balance);
        Assert.Equal(initialVersion + 1, account.Version);
    }

    [Fact]
    public void TryWithdraw_With_Insufficient_Balance_Should_Fail()
    {
        // Arrange
        var account = new Account(Guid.NewGuid());
        account.TopUp(50);
        var initialBalance = account.Balance;
        var initialVersion = account.Version;

        // Act
        var result = account.TryWithdraw(100);

        // Assert
        Assert.False(result);
        Assert.Equal(initialBalance, account.Balance);
        Assert.Equal(initialVersion, account.Version);
    }

    [Fact]
    public void TryWithdraw_With_Zero_Amount_Should_Throw_Exception()
    {
        // Arrange
        var account = new Account(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => account.TryWithdraw(0));
    }

    [Fact]
    public void TryWithdraw_With_Negative_Amount_Should_Throw_Exception()
    {
        // Arrange
        var account = new Account(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => account.TryWithdraw(-10));
    }
} 