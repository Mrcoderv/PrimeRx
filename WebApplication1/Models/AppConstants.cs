namespace WebApplication1.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
}

public static class PaymentMethods
{
    public const string Cash = "Cash";
    public const string Online = "Online";
    public const string Due = "Due";

    public static readonly string[] All = [Cash, Online, Due];
}

public static class PaymentStatuses
{
    public const string Paid = "Paid";
    public const string PartiallyPaid = "Partially Paid";
    public const string Due = "Due";
}

public static class TransactionTypes
{
    public const string Sale = "Sale";
    public const string Purchase = "Purchase";
    public const string Adjustment = "Adjustment";
}
