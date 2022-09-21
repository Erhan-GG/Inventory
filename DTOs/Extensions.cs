using Inventory.Entities;
using static Inventory.DTOs.DTOs;

namespace Inventory.DTOs
{
    public static class Extensions
    {
        public static InventoryItemDTO AsDTO(this InventoryItem item, string name, string description)
        {
            return new InventoryItemDTO(
                item.CatalogItemId,
                name,
                description,
                item.Quantity,
                item.AquiredDate
             );
        }
    }
}
