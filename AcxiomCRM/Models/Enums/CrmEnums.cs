namespace AcxiomCRM.Models.Enums
{
    public static class SystemRoles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string SalesExecutive = "Sales Executive";

        public static readonly string[] AllRoles = new[] { Admin, Manager, SalesExecutive };
    }

    public enum CustomerStatus
    {
        Active,
        Inactive,
        Lead
    }

    public enum LeadStatus
    {
        New,
        Contacted,
        Qualified,
        Unqualified,
        Converted,
        Lost
    }

    public enum LeadPriority
    {
        Low,
        Medium,
        High
    }

    public enum LeadSource
    {
        Website,
        Referral,
        ColdCall,
        Campaign,
        Partner,
        Other
    }

    public enum OpportunityStage
    {
        Qualification,
        Proposal,
        Negotiation,
        Won,
        Lost
    }

    public enum FollowUpType
    {
        Call,
        Meeting,
        Email,
        Task
    }

    public enum FollowUpStatus
    {
        Planned,
        Completed,
        Missed,
        Cancelled
    }

    public enum ActivityType
    {
        Call,
        Meeting,
        Email,
        Task
    }

    public enum ActivityStatus
    {
        Pending,
        Completed,
        Cancelled
    }
}
