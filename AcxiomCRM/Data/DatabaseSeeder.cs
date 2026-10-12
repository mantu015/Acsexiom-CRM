using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

            try
            {
                // Ensure SQLite or SQL Server tables are created
                await context.Database.EnsureCreatedAsync();

                await RemoveLegacyDemoDataAsync(context, userManager);

                // 1. Seed Roles
                foreach (var roleName in SystemRoles.AllRoles)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                logger.LogInformation("Database initialized with roles; legacy demo data removed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        private static async Task RemoveLegacyDemoDataAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            var customerCodes = new[] { "CUST-1001", "CUST-1002", "CUST-1003" };
            var leadCodes = new[] { "LEAD-1001", "LEAD-1002", "LEAD-1003" };
            var opportunityCodes = new[] { "OPP-1001", "OPP-1002", "OPP-1003" };

            var customers = await context.Customers
                .Where(customer => customerCodes.Contains(customer.CustomerCode))
                .ToListAsync();
            var customerIds = customers.Select(customer => customer.CustomerId).ToArray();

            var leads = await context.Leads
                .Where(lead => leadCodes.Contains(lead.LeadCode))
                .ToListAsync();
            var leadIds = leads.Select(lead => lead.LeadId).ToArray();

            var opportunities = await context.Opportunities
                .Where(opportunity => opportunityCodes.Contains(opportunity.OpportunityCode)
                    || customerIds.Contains(opportunity.CustomerId))
                .ToListAsync();
            var opportunityIds = opportunities.Select(opportunity => opportunity.OpportunityId).ToArray();

            var followUps = await context.FollowUps
                .Where(followUp => (followUp.CustomerId.HasValue && customerIds.Contains(followUp.CustomerId.Value))
                    || (followUp.LeadId.HasValue && leadIds.Contains(followUp.LeadId.Value))
                    || (followUp.OpportunityId.HasValue && opportunityIds.Contains(followUp.OpportunityId.Value)))
                .ToListAsync();
            var activities = await context.Activities
                .Where(activity => (activity.CustomerId.HasValue && customerIds.Contains(activity.CustomerId.Value))
                    || (activity.LeadId.HasValue && leadIds.Contains(activity.LeadId.Value))
                    || (activity.OpportunityId.HasValue && opportunityIds.Contains(activity.OpportunityId.Value)))
                .ToListAsync();

            context.FollowUps.RemoveRange(followUps);
            context.Activities.RemoveRange(activities);
            context.Opportunities.RemoveRange(opportunities);
            context.Leads.RemoveRange(leads);
            context.Customers.RemoveRange(customers);
            context.AuditLogs.RemoveRange(await context.AuditLogs
                .Where(log => log.Details == "User admin@acxiomcrm.com logged in successfully"
                    || log.Details == "Created customer Apex Global Technologies")
                .ToListAsync());
            await context.SaveChangesAsync();

            var demoUsers = new[]
            {
                (Email: "admin@acxiomcrm.com", FullName: "Alex Morgan (Administrator)"),
                (Email: "manager@acxiomcrm.com", FullName: "Sarah Connor (Sales Manager)"),
                (Email: "sales@acxiomcrm.com", FullName: "John Doe (Sales Executive)")
            };

            foreach (var (email, fullName) in demoUsers)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null || user.FullName != fullName)
                {
                    continue;
                }

                var userId = user.Id;
                var hasAssignedRecords = await context.Customers.AnyAsync(customer => customer.OwnerId == userId)
                    || await context.Leads.AnyAsync(lead => lead.AssignedTo == userId)
                    || await context.Opportunities.AnyAsync(opportunity => opportunity.AssignedTo == userId)
                    || await context.FollowUps.AnyAsync(followUp => followUp.AssignedTo == userId)
                    || await context.Activities.AnyAsync(activity => activity.AssignedTo == userId);

                if (!hasAssignedRecords)
                {
                    await userManager.DeleteAsync(user);
                }
            }
        }
    }
}
