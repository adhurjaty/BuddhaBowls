﻿namespace BuddhaBowls.Messengers
{
    /// <summary>
    /// Use an enumeration for the messages to ensure consistency.
    /// </summary>
    public enum MessageTypes
    {
        INVENTORY_ITEM_ADDED,
        INVENTORY_ITEM_REMOVED,
        INVENTORY_ITEM_CHANGED,
        INVENTORY_CHANGED,
        VENDORS_CHANGED,
        PO_CHANGED,
        RECIPE_ADDED,
        RECIPE_REMOVED,
        RECIPE_CHANGED,
        VENDOR_INV_ITEMS_CHANGED,
        BREAD_CHANGED,
        PREP_ITEM_CHANGED
    };
}