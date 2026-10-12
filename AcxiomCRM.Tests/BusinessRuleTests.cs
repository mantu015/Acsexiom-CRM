using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace AcxiomCRM.Tests
{
    public class BusinessRuleTests
    {
        [Fact]
        public void Opportunity_WeightedValue_ShouldCalculateCorrectly()
        {
            // Weighted Value = Amount * Probability / 100
            var opp = new Opportunity
            {
                Amount = 50000m,
                Probability = 60
            };

            var expected = 30000m; // 50000 * 60 / 100
            Assert.Equal(expected, opp.WeightedValue);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-500)]
        public void OpportunityForm_ShouldReject_AmountLessThanOrEqualToZero(decimal invalidAmount)
        {
            var model = new OpportunityFormViewModel
            {
                OpportunityName = "Test Deal",
                CustomerId = 1,
                Amount = invalidAmount,
                Probability = 50,
                ExpectedCloseDate = DateTime.Today.AddDays(10),
                Status = "Open"
            };

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model);
            var isValid = Validator.TryValidateObject(model, context, validationResults, true);

            Assert.False(isValid);
            Assert.Contains(validationResults, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Opportunity Amount must be greater than 0"));
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(105)]
        public void OpportunityForm_ShouldReject_ProbabilityOutsideZeroToHundred(int invalidProb)
        {
            var model = new OpportunityFormViewModel
            {
                OpportunityName = "Test Deal",
                CustomerId = 1,
                Amount = 1000m,
                Probability = invalidProb,
                ExpectedCloseDate = DateTime.Today.AddDays(10),
                Status = "Open"
            };

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model);
            var isValid = Validator.TryValidateObject(model, context, validationResults, true);

            Assert.False(isValid);
            Assert.Contains(validationResults, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Probability must be between 0 and 100"));
        }

        [Fact]
        public void OpportunityForm_ShouldReject_ExpectedCloseDateInThePast_ForActiveOpportunity()
        {
            var model = new OpportunityFormViewModel
            {
                OpportunityName = "Test Deal",
                CustomerId = 1,
                Amount = 1000m,
                Probability = 50,
                ExpectedCloseDate = DateTime.Today.AddDays(-2), // Yesterday or before
                Status = "Open"
            };

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model);
            var isValid = Validator.TryValidateObject(model, context, validationResults, true);

            Assert.False(isValid);
            Assert.Contains(validationResults, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Expected Close Date cannot be in the past"));
        }

        [Fact]
        public void FollowUpForm_ShouldReject_PastFollowUpDate_ForNewPlannedActivity()
        {
            var model = new FollowUpFormViewModel
            {
                FollowUpId = 0,
                RelatedType = "Customer",
                RelatedId = 1,
                FollowUpDate = DateTime.Today.AddDays(-1), // in the past
                FollowUpType = "Call",
                Subject = "Follow up with client",
                Status = "Planned"
            };

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model);
            var isValid = Validator.TryValidateObject(model, context, validationResults, true);

            Assert.False(isValid);
            Assert.Contains(validationResults, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Follow-up date cannot be earlier than today"));
        }

        [Fact]
        public async Task LeadService_ConvertLead_ShouldCreateCustomerAndOpportunity_AndPreserveHistory()
        {
            // Set up in-memory DbContext
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var mockAudit = new Mock<IAuditService>();
            var customerService = new CustomerService(context, mockAudit.Object);
            var leadService = new LeadService(context, mockAudit.Object, customerService);

            // Add sample lead
            var lead = new Lead
            {
                LeadCode = "LEAD-9001",
                LeadName = "Alice Morgan",
                Email = "alice@example.com",
                Phone = "9988776655",
                CompanyName = "Morgan Tech",
                Status = "Qualified",
                Priority = "High",
                ExpectedValue = 25000m,
                AssignedTo = "user-123",
                CreatedDate = DateTime.UtcNow
            };
            context.Leads.Add(lead);
            await context.SaveChangesAsync();

            var convertModel = new LeadConvertViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                CreateOpportunity = true,
                OpportunityName = "Alice Morgan - Cloud Project",
                OpportunityAmount = 25000m,
                ExpectedCloseDate = DateTime.Today.AddDays(30)
            };

            var result = await leadService.ConvertLeadAsync(convertModel, "user-123", new List<string> { "Sales Executive" }, "John Sales");

            Assert.True(result.Success);
            Assert.NotNull(result.CustomerId);
            Assert.NotNull(result.OpportunityId);

            // Verify Lead status updated
            var updatedLead = await context.Leads.FindAsync(lead.LeadId);
            Assert.NotNull(updatedLead);
            Assert.Equal("Converted", updatedLead.Status);
            Assert.True(updatedLead.IsConverted);
            Assert.Equal(result.CustomerId, updatedLead.ConvertedCustomerId);
            Assert.Equal(result.OpportunityId, updatedLead.ConvertedOpportunityId);

            // Verify Customer created
            var createdCustomer = await context.Customers.FindAsync(result.CustomerId);
            Assert.NotNull(createdCustomer);
            Assert.Equal("Alice Morgan", createdCustomer.CustomerName);
            Assert.Equal("alice@example.com", createdCustomer.Email);

            // Verify Opportunity created
            var createdOpp = await context.Opportunities.FindAsync(result.OpportunityId);
            Assert.NotNull(createdOpp);
            Assert.Equal("Alice Morgan - Cloud Project", createdOpp.OpportunityName);
            Assert.Equal(25000m, createdOpp.Amount);
        }

        [Fact]
        public async Task DashboardService_WithSqlite_ShouldLoadDashboardWithoutDecimalOrderByException()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();

                var user = new ApplicationUser
                {
                    Id = "user-1",
                    UserName = "user1@example.com",
                    Email = "user1@example.com",
                    FullName = "Example User",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                var customer = new Customer
                {
                    CustomerCode = "CUST-9001",
                    CustomerName = "Example Customer",
                    Email = "customer@example.com",
                    Phone = "9991112222",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    OwnerId = user.Id
                };
                context.Customers.Add(customer);
                await context.SaveChangesAsync();

                context.Opportunities.Add(new Opportunity
                {
                    OpportunityCode = "OPP-9001",
                    OpportunityName = "Example Opportunity",
                    CustomerId = customer.CustomerId,
                    Amount = 75000m,
                    Stage = "Proposal",
                    Probability = 50,
                    Status = "Open",
                    CreatedDate = DateTime.UtcNow.AddDays(-5),
                    ExpectedCloseDate = DateTime.Today.AddDays(10),
                    AssignedTo = user.Id
                });

                await context.SaveChangesAsync();

                var followUpService = new Mock<IFollowUpService>();
                followUpService.Setup(s => s.GetUpcomingFollowUpsAsync("user-1", It.IsAny<IList<string>>(), 5))
                    .ReturnsAsync(new List<FollowUp>());
                followUpService.Setup(s => s.GetOverdueFollowUpsAsync("user-1", It.IsAny<IList<string>>(), 5))
                    .ReturnsAsync(new List<FollowUp>());

                var activityService = new Mock<IActivityService>();
                activityService.Setup(s => s.GetRecentActivitiesAsync("user-1", It.IsAny<IList<string>>(), 5))
                    .ReturnsAsync(new List<Activity>());

                var service = new DashboardService(context, followUpService.Object, activityService.Object);

                var result = await service.GetDashboardDataAsync("user-1", new List<string> { SystemRoles.SalesExecutive }, "Example User", "ThisMonth");

                Assert.NotNull(result);
                Assert.Equal(1, result.TotalOpportunities);
                Assert.Single(result.HighValueOpportunities);
                Assert.Equal("Example Opportunity", result.HighValueOpportunities[0].OpportunityName);
            }
        }
    }
}
