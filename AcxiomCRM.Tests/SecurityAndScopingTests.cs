using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services;
using AcxiomCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AcxiomCRM.Tests
{
    public class SecurityAndScopingTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task AuditService_LogAsync_ShouldAppendAuditRecordSuccessfully()
        {
            using var context = CreateInMemoryContext();
            var mockLogger = new Mock<ILogger<AuditService>>();
            var auditService = new AuditService(context, mockLogger.Object);

            await auditService.LogAsync(
                userId: "user-test-1",
                userName: "Test User",
                action: "Create",
                entityName: "Customer",
                recordId: "101",
                oldValue: null,
                newValue: "Name: Test Customer",
                details: "Customer created via automated test",
                result: "Success",
                ipAddress: "127.0.0.1"
            );

            var logs = await context.AuditLogs.ToListAsync();
            Assert.Single(logs);
            var log = logs.First();
            Assert.Equal("Create", log.Action);
            Assert.Equal("Customer", log.EntityName);
            Assert.Equal("Test User", log.UserName);
            Assert.Equal("Success", log.Result);
            Assert.Equal("127.0.0.1", log.IpAddress);
        }

        [Fact]
        public async Task OpportunityService_RoleScoping_SalesExecutiveCannotAccessOtherUsersOpportunity()
        {
            using var context = CreateInMemoryContext();
            var mockAudit = new Mock<IAuditService>();
            var oppService = new OpportunityService(context, mockAudit.Object);

            var salesUser1 = "sales-exec-1";
            var salesUser2 = "sales-exec-2";

            var customer = new Customer
            {
                CustomerCode = "CUST-1001",
                CustomerName = "Enterprise Client",
                Email = "client@domain.com",
                Phone = "1234567890",
                Status = "Active"
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var opp = new Opportunity
            {
                OpportunityCode = "OPP-5001",
                OpportunityName = "Confidential Enterprise Deal",
                CustomerId = customer.CustomerId,
                Amount = 100000m,
                Stage = "Negotiation",
                Probability = 75,
                ExpectedCloseDate = DateTime.Today.AddDays(20),
                Status = "Open",
                AssignedTo = salesUser2 // Owned by salesUser2
            };
            context.Opportunities.Add(opp);
            await context.SaveChangesAsync();

            // salesUser1 tries to access salesUser2's opportunity
            var forbiddenOpp = await oppService.GetOpportunityByIdAsync(opp.OpportunityId, salesUser1, new List<string> { SystemRoles.SalesExecutive });
            Assert.Null(forbiddenOpp); // Must be blocked / null on server!

            // salesUser2 accesses their own opportunity
            var allowedOpp = await oppService.GetOpportunityByIdAsync(opp.OpportunityId, salesUser2, new List<string> { SystemRoles.SalesExecutive });
            Assert.NotNull(allowedOpp);
            Assert.Equal("Confidential Enterprise Deal", allowedOpp.OpportunityName);

            // Admin accesses any opportunity
            var adminOpp = await oppService.GetOpportunityByIdAsync(opp.OpportunityId, "admin-user", new List<string> { SystemRoles.Admin });
            Assert.NotNull(adminOpp);
        }

        [Fact]
        public async Task FollowUpService_CompleteFollowUp_ShouldMarkStatusAndAddActivity()
        {
            using var context = CreateInMemoryContext();
            var mockAudit = new Mock<IAuditService>();
            var followUpService = new FollowUpService(context, mockAudit.Object);

            var followUp = new FollowUp
            {
                Subject = "Quarterly contract discussion",
                FollowUpType = "Meeting",
                FollowUpDate = DateTime.UtcNow.AddHours(2),
                Status = "Planned",
                AssignedTo = "sales-1"
            };
            context.FollowUps.Add(followUp);
            await context.SaveChangesAsync();

            var completeModel = new ViewModels.FollowUpCompleteViewModel
            {
                FollowUpId = followUp.FollowUpId,
                Subject = followUp.Subject,
                CompletionRemarks = "Met with client, approved quote proposal",
                ScheduleNextFollowUp = false
            };

            var success = await followUpService.CompleteFollowUpAsync(completeModel, "sales-1", new List<string> { SystemRoles.SalesExecutive }, "Sales Rep");

            Assert.True(success);

            var updatedFollowUp = await context.FollowUps.FindAsync(followUp.FollowUpId);
            Assert.NotNull(updatedFollowUp);
            Assert.Equal("Completed", updatedFollowUp.Status);
            Assert.Equal("Met with client, approved quote proposal", updatedFollowUp.CompletionRemarks);

            // Verify Activity record was automatically created
            var activities = await context.Activities.ToListAsync();
            Assert.Single(activities);
            Assert.Equal("Completed: Quarterly contract discussion", activities.First().Subject);
        }
    }
}
