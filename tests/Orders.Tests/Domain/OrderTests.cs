using Orders.Domain.Entities;
using Xunit;

namespace Orders.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void Order_Creation_Should_Set_Correct_Properties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var amount = 100.50m;
        var description = "Test order";

        // Act
        var order = new Order(userId, amount, description);

        // Assert
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(userId, order.UserId);
        Assert.Equal(amount, order.Amount);
        Assert.Equal(description, order.Description);
        Assert.Equal(OrderStatus.Created, order.Status);
        Assert.True(order.CreatedAt <= DateTime.UtcNow);
        Assert.Null(order.UpdatedAt);
    }

    [Fact]
    public void Order_Creation_With_Zero_Amount_Should_Throw_Exception()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var amount = 0m;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Order(userId, amount));
    }

    [Fact]
    public void Order_Creation_With_Negative_Amount_Should_Throw_Exception()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var amount = -10m;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Order(userId, amount));
    }

    [Fact]
    public void MarkAsPaid_Should_Update_Status_And_UpdatedAt()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), 100m);
        var beforeUpdate = DateTime.UtcNow;

        // Act
        order.MarkAsPaid();

        // Assert
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.NotNull(order.UpdatedAt);
        Assert.True(order.UpdatedAt >= beforeUpdate);
    }

    [Fact]
    public void MarkAsFailed_Should_Update_Status_And_UpdatedAt()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), 100m);
        var beforeUpdate = DateTime.UtcNow;

        // Act
        order.MarkAsFailed("Insufficient funds");

        // Assert
        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.NotNull(order.UpdatedAt);
        Assert.True(order.UpdatedAt >= beforeUpdate);
    }

    [Fact]
    public void MarkAsPaid_On_Already_Paid_Order_Should_Throw_Exception()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), 100m);
        order.MarkAsPaid();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.MarkAsPaid());
    }

    [Fact]
    public void MarkAsFailed_On_Already_Failed_Order_Should_Throw_Exception()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), 100m);
        order.MarkAsFailed("Test reason");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.MarkAsFailed("Another reason"));
    }
} 