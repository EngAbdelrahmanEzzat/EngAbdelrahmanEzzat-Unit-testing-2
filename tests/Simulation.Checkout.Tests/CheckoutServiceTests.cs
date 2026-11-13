using FluentAssertions;
using Bogus;
using VerifyXunit;
using AutoFixture;
using Moq;
using Shouldly;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;
using Simulation.Checkout.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Simulation.Checkout.Tests
{
    public class CheckoutServiceTests
    {
        [Fact]
        public async Task CheckoutAsync_ShouldThrowArgumentNullException_WhenOrderIsNull()
        {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();
            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);
            // Act & Assert
            
            await Should.ThrowAsync<ArgumentNullException>(async () => await checkoutService.CheckoutAsync(null));
        }
        [Fact]
        public async Task CheckoutAsync_ShouldThrowEmptyOrderException_WhenOrderHasNoItems()
        {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();

            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                CustomerEmail = "JohnDoe@gmail.com",
                Items = new List<OrderItem>()
            };
            

            // Act & Assert
            await Should.ThrowAsync<EmptyOrderException>(
                async () => await checkoutService.CheckoutAsync(order));
        }
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task CheckoutAsync_ShouldThrowInvalidQuantityException_WhenOrderItemQuantityIsInvalid(int quantity)
        {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();
            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);
            var OrderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product 1",
                UnitPrice = 10.0m,
                Quantity = quantity // Invalid quantity
            };
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                CustomerEmail = "JohnDoe@gmail.com",
                Items = new List<OrderItem> { OrderItem }
            };
        
            // Act & Assert
            await Should.ThrowAsync<InvalidQuantityException>(async () => await checkoutService.CheckoutAsync(order));

        }

       
        public static TheoryData<decimal> InvalidUnitPrices => new TheoryData<decimal>
        {
            -0.01m,
        };
        [Theory]
        [MemberData(nameof(InvalidUnitPrices))]
        public async Task CheckoutAsync_ShouldThrowInvalidPriceException_WhenOrderItemUnitPriceIsNegativeAsync(decimal unitPrice)
        {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();
            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);
            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product 1",
                UnitPrice = unitPrice, // Invalid price
                Quantity = 5
            };
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                CustomerEmail = "JohnDoe@gmail.com",
                Items = new List<OrderItem> { orderItem }
            };
        

            // Act & Assert
            await Should.ThrowAsync<InvalidPriceException>(async () => await checkoutService.CheckoutAsync(order));
        }
        [Fact]
        public async Task CheckoutAsync_ShouldAcceptZeroPrice_WhenOrderItemUnitPriceIsZero()
        {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();

            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);

            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product 1",
                UnitPrice = 0m,
                Quantity = 5
            };

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                CustomerEmail = "JohnDoe@gmail.com",
                Items = new List<OrderItem> { orderItem }
            };

            iInventoryService
                .Setup(x => x.IsAvailableAsync(
                    orderItem.ProductId,
                    orderItem.Quantity,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var receipt = await checkoutService.CheckoutAsync(order);

            // Assert
            receipt.Subtotal.ShouldBe(0m);
            receipt.Discount.ShouldBe(0m);
            receipt.Total.ShouldBe(0m);
        }
        [Fact]
        public async Task CheckoutAsync_ShouldThrowProductNotAvailableException_WhenOrderItemIsNotAvailableInInventory()
        {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();
            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);
            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product 1",
                UnitPrice = 10.0m,
                Quantity = 5
            };
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                CustomerEmail = "JohnDoe@gmail.com",
                Items = new List<OrderItem> { orderItem }
            };
            iInventoryService
              .Setup(x => x.IsAvailableAsync(
                  orderItem.ProductId,
                  orderItem.Quantity,
                  It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
            //Act & Assert
           
            await Should.ThrowAsync<InsufficientInventoryException>(async () => await checkoutService.CheckoutAsync(order));
        }
        public static TheoryData<decimal, decimal> DiscountTestData1 => new TheoryData<decimal, decimal>
        {
            { 999.99m, 0m },
            { 1000m, 100m },
            { 1500m, 100m },
        };
        [Theory]
        [MemberData(nameof(DiscountTestData1))]
      
        public async Task CheckoutAsync_CheckDifferentSenarios_WhenDicountIsHasDifferentValues(decimal subtotal,decimal expecteddiscount)
        {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();

            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);

            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product 1",
                UnitPrice = subtotal,
                Quantity = 1
            };

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                CustomerEmail = "JohnDoe@gmail.com",
                Items = new List<OrderItem> { orderItem }
            };

            iInventoryService
                .Setup(x => x.IsAvailableAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var receipt = await checkoutService.CheckoutAsync(order);

            // Assert
          
            receipt.Discount.ShouldBe(expecteddiscount);
           
        }
        public static TheoryData<List<OrderItem>, decimal, decimal, decimal> DiscountTestData2 => new()
        { 
    // Example A
           {
        new List<OrderItem>
        {
            new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product A",
                UnitPrice = 150m,
                Quantity = 2
            },
            new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product B",
                UnitPrice = 400m,
                Quantity = 1
            }
        },
        700m,  // Expected Subtotal
        0m,    // Expected Discount
        700m   // Expected Total
    },

    // Example C
    {
        new List<OrderItem>
        {
            new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product C",
                UnitPrice = 500m,
                Quantity = 3
            }
        },
        1500m, // Expected Subtotal
        100m,  // Expected Discount
        1400m  // Expected Total
    }
        };
        [Theory]
        [MemberData(nameof(DiscountTestData2))]
        public async Task CheckoutAsync_CheckExamplesAAndC_WhenCalculatingDiscount(
           List<OrderItem> items,
           decimal expectedSubtotal,
           decimal expectedDiscount,
           decimal expectedTotal)
         {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();

            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                CustomerEmail = "JohnDoe@gmail.com",
                Items = items
            };

            iInventoryService
                .Setup(x => x.IsAvailableAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var receipt = await checkoutService.CheckoutAsync(order);

            // Assert
            receipt.Subtotal.Should().Be(expectedSubtotal);
            receipt.Discount.Should().Be(expectedDiscount);
            receipt.Total.Should().Be(expectedTotal);
        }
        [Fact]
        public async Task CheckoutAsync_ValidateSaveAsync_SaveAsyncCorrectly()
        {

            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();
            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);
            iInventoryService
         .Setup(x => x.IsAvailableAsync(
        It.IsAny<Guid>(),
        It.IsAny<int>(),
        It.IsAny<CancellationToken>()))
       .ReturnsAsync(true);
            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product 1",
                UnitPrice = 10.0m,
                Quantity = 5
            };
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                CustomerEmail = "JohnDoe@gmail.com",
                Items = new List<OrderItem> { orderItem }
            };
            //act&assert

            var recipt = await checkoutService.CheckoutAsync(order);
            iOrderRepository.Verify(x => x.SaveAsync(order, It.IsAny<CancellationToken>()), Times.Once);//دي مكان جزءيه ال received اللي في nsubstitute.
            iNotificationService.Verify(x => x.SendOrderConfirmationAsync(recipt, It.IsAny<CancellationToken>()), Times.Once);
        }
        [Fact]
        public async Task CheckoutAsync_ShouldNotSaveOrder_WhenInventoryIsUnavailable()
        {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();

            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);

            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product 1",
                UnitPrice = 100m,
                Quantity = 5
            };

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                CustomerEmail = "JohnDoe@gmail.com",
                Items = new List<OrderItem> { orderItem }
            };

            iInventoryService
                .Setup(x => x.IsAvailableAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            
            await Should.ThrowAsync<InsufficientInventoryException>(
                async () => await checkoutService.CheckoutAsync(order));

            iOrderRepository.Verify(
                x => x.SaveAsync(order, It.IsAny<CancellationToken>()),
                Times.Never);

            iNotificationService.Verify(x => x.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CheckoutAsync_ShouldNotSaveOrder_WhenQuantityIsInvalid()
        {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();

            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);

            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product 1",
                UnitPrice = 100m,
                Quantity = 0 // Invalid quantity
            };

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                CustomerEmail = "JohnDoe@gmail.com",
                Items = new List<OrderItem> { orderItem }
            };

           
            // Act & Assert

            await Should.ThrowAsync<InvalidQuantityException>(
                async () => await checkoutService.CheckoutAsync(order));
            iInventoryService.Verify(
              x => x.IsAvailableAsync(
             It.IsAny<Guid>(),
             It.IsAny<int>(),
             It.IsAny<CancellationToken>()),
             Times.Never);
            iOrderRepository.Verify(
                x => x.SaveAsync(order, It.IsAny<CancellationToken>()),
                Times.Never);

            iNotificationService.Verify(x => x.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CheckoutAsync_WithMultipleOrders_ReturnsSumOfAllOrders()
        {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();

            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);

           var fixture = new Fixture();
            fixture.Customize<OrderItem>(c=>c
            .With(x => x.Quantity,()=>Random.Shared.Next(1, 10))
            .With(x => x.UnitPrice,()=>Random.Shared.Next(0, 10000)));
            var orders = fixture.CreateMany<Order>(3).ToList();
            foreach (var order in orders)
            {
                foreach (var item in order.Items)
                {
                    iInventoryService
                        .Setup(x => x.IsAvailableAsync(
                            item.ProductId,
                            item.Quantity,
                            It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);
                }
            }
            decimal sum = 0;
            decimal discount = 0;
            foreach (var order in orders)
            {
                foreach (var item in order.Items)
                {
                    sum += item.UnitPrice * item.Quantity;
                }
                if(sum<1000)
                {
                    discount += 0;
                }
                else
                {
                    if(sum >= 1000)
                    {
                        discount += 100;
                    }
                }
            }
            decimal expectedTotal = orders.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity)) - discount;
            // Act
            decimal actualTotal = 0;
            foreach (var order in orders)
            {
                var receipt = await checkoutService.CheckoutAsync(order);
                actualTotal += receipt.Total;
            }
            // Assert
            actualTotal.Should().Be(expectedTotal);


        }
        private static readonly Faker<OrderItem> items = new Faker<OrderItem>()
    .UseSeed(2026)
    .RuleFor(i => i.ProductId, f => f.Random.Guid())
    .RuleFor(i => i.ProductName, f => f.Commerce.ProductName())
    .RuleFor(i => i.UnitPrice, f => Math.Round(f.Random.Decimal(1, 1000), 2))
    .RuleFor(i => i.Quantity, f => f.Random.Int(1, 5));

        private static readonly Faker<Order> customers = new Faker<Order>()
            .UseSeed(2026)
            .RuleFor(c => c.CustomerName, f => f.Name.FullName())
            .RuleFor(c => c.CustomerEmail, f => f.Internet.Email())
            .RuleFor(c => c.Items, f => items.Generate(1));
        [Fact]
        public async Task CreateReceipt_WithValidOrder_CopiesCustomerNameAndEmail()
        {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();

            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);
            iInventoryService
               .Setup(x => x.IsAvailableAsync(
                   It.IsAny<Guid>(),
                   It.IsAny<int>(),
                   It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);
            //Act
            var order = customers.Generate();
            var recipt = await checkoutService.CheckoutAsync(order);
           
            //Assert
            recipt.CustomerName.Should().Be(order.CustomerName);
            recipt.CustomerEmail.Should().Be(order.CustomerEmail);
        }

        [Fact]
        public async Task CheckoutAsync_CheckExampleC_UsingVerifyXunit()
        {
            // Arrange
            var iOrderRepository = new Mock<IOrderRepository>();
            var iInventoryService = new Mock<IInventoryService>();
            var iNotificationService = new Mock<INotificationService>();

            var checkoutService = new CheckoutService(
                iOrderRepository.Object,
                iInventoryService.Object,
                iNotificationService.Object);
            iInventoryService
               .Setup(x => x.IsAvailableAsync(
                   It.IsAny<Guid>(),
                   It.IsAny<int>(),
                   It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product C",
                UnitPrice = 500m,
                Quantity = 3
            };
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                CustomerEmail = "john.doe@example.com",
                Items = new List<OrderItem> { orderItem }
            };

            // Act
            var receipt = await checkoutService.CheckoutAsync(order);

            // Assert
             await Verify(receipt);
        }


    }
}
