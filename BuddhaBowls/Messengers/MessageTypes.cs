namespace BuddhaBowls.Messengers
{
    /// <summary>
    /// Use an enumeration for the messages to ensure consistency.
    /// </summary>
    public enum MessageTypes
    {
        INVENTORY_ITEM_ADDED,
        INVENTORY_ITEM_REMOVED,
        INVENTORY_ITEM_CHANGED,
        VENDOR_ADDED,
        VENDOR_REMOVED,
        VENDOR_CHANGED,
        PO_ADDED,
        PO_REMOVED,
        PO_CHANGED,
        RECIPE_ADDED,
        RECIPE_REMOVED,
        RECIPE_CHANGED,
        VENDOR_INV_ITEMS_CHANGED
    };
}