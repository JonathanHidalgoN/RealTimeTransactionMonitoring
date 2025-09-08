namespace FinancialMonitoring.Models;

/// <summary>
/// Defines the different roles available for authenticated users in the financial monitoring system
/// </summary>
public enum AuthUserRole
{
    /// <summary>
    /// Read-only access to dashboards and reports
    /// </summary>
    Viewer = 1,

    /// <summary>
    /// Can create and modify analysis reports, access advanced features
    /// </summary>
    Analyst = 2,

    /// <summary>
    /// Full system access including user management and system configuration
    /// </summary>
    Admin = 3
}
