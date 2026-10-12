using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AcxiomCRM.Tests
{
    public class CustomerServiceTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CustomerService_DuplicateCustomer_ShouldBeDetected()
        {
            using var context = CreateInMemoryContext();
            var mockAudit = new Mock<IAuditService>();
            var service = new CustomerService(context, mockAudit.Object);

            // Add existing customer
            context.Customers.Add(new Customer
            {
                CustomerCode = "CUST-1001",
                CustomerName = "Acme Global",
                Email = "contact@acme.com",
                Phone = "9876543210",
                Status = "Active"
            });
            await context.SaveChangesAsync();

            // Check duplicate by email
            var isDupByEmail = await service.IsDuplicateCustomerAsync("Other Name", "contact@acme.com", "1234567890");
            Assert.True(isDupByEmail);

            // Check duplicate by phone
            var isDupByPhone = await service.IsDuplicateCustomerAsync("Other Name", "other@acme.com", "9876543210");
            Assert.True(isDupByPhone);

            // Check duplicate by exact name
            var isDupByName = await service.IsDuplicateCustomerAsync("Acme Global", "new@domain.com", "1234567890");
            Assert.True(isDupByName);

            // Check unique customer
            var isUnique = await service.IsDuplicateCustomerAsync("Completely Unique Corp", "unique@domain.com", "5555555555");
            Assert.False(isUnique);
        }

        [Fact]
        public async Task CustomerService_RoleScoping_SalesExecutive_ShouldOnlySeeOwnCustomers()
        {
            using var context = CreateInMemoryContext();
            var mockAudit = new Mock<IAuditService>();
            var service = new CustomerService(context, mockAudit.Object);

            var salesUser1 = "sales-1";
            var salesUser2 = "sales-2";

            // Add customers assigned to different owners
            context.Customers.AddRange(
                new Customer
                {
                    CustomerCode = "CUST-1001",
                    CustomerName = "Customer of Rep 1",
                    Email = "rep1@example.com",
                    Phone = "1111111111",
                    OwnerId = salesUser1
                },
                new Customer
                {
                    CustomerCode = "CUST-1002",
                    CustomerName = "Customer of Rep 2",
                    Email = "rep2@example.com",
                    Phone = "2222222222",
                    OwnerId = salesUser2
                }
            );
            await context.SaveChangesAsync();

            // Query as Sales Executive 1
            var rep1Results = await service.GetCustomersAsync(salesUser1, new List<string> { SystemRoles.SalesExecutive }, null, null);
            Assert.Single(rep1Results.Items);
            Assert.Equal("Customer of Rep 1", rep1Results.Items.First().CustomerName);

            // Query as Manager (sees all team records)
            var managerResults = await service.GetCustomersAsync("manager-1", new List<string> { SystemRoles.Manager }, null, null);
            Assert.Equal(2, managerResults.Items.Count);

            // Query as Admin (sees all records)
            var adminResults = await service.GetCustomersAsync("admin-1", new List<string> { SystemRoles.Admin }, null, null);
            Assert.Equal(2, adminResults.Items.Count);
        }
    }
}
