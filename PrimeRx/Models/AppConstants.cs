namespace PrimeRx.Models;

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

public static class BillStatuses
{
    public const string Active = "Active";
    public const string Cancelled = "Cancelled";
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
    public const string Return = "Return";
    public const string Exchange = "Exchange";
}

public static class PayableStatus
{
    public const string Pending = "Pending";
    public const string Partial = "Partial";
    public const string Paid = "Paid";

    public static readonly string[] All = [Pending, Partial, Paid];
}

public static class ExpenseCategories
{
    public const string Purchase = "Purchase";
    public const string StaffSalary = "StaffSalary";
    public const string Rent = "Rent";
    public const string Utilities = "Utilities";
    public const string Transport = "Transport";
    public const string Miscellaneous = "Miscellaneous";

    public static readonly string[] All = [Purchase, StaffSalary, Rent, Utilities, Transport, Miscellaneous];

    public static string Display(string category) => category switch
    {
        Purchase => "Purchase / Inventory",
        StaffSalary => "Staff Salary",
        Rent => "Rent",
        Utilities => "Utilities (Electric / Water / Internet)",
        Transport => "Transport / Delivery",
        Miscellaneous => "Miscellaneous",
        _ => category
    };
}
