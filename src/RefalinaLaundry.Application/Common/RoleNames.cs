namespace RefalinaLaundry.Application.Common;

public static class RoleNames
{
    public const string Owner = "Owner";
    public const string Cashier = "Cashier";
    public const string Operational = Owner + "," + Cashier;
}

public static class AuditActions
{
    public const string OrderCancelled = "OrderCancelled";
    public const string ReceiptReprinted = "ReceiptReprinted";
    public const string ServicePriceChanged = "ServicePriceChanged";
    public const string BusinessSettingsChanged = "BusinessSettingsChanged";
}
