using System;
using System.ComponentModel.DataAnnotations;

namespace Inventory.DTOs
{
    public class DTOs
    {
        public record GrantItemsDTO(
            Guid UserId,
            Guid CatalogItemId,
            [Range(1,int.MaxValue)]
            int Quantity);

        public record InventoryItemDTO(
            Guid CatalogItemId,
            string Name,
            string Description,
            int Quantity,
            DateTimeOffset AquiredDate);

        public record CatalogItemDTO(
           Guid Id,
           string Name,
           string Description);
    }
}
