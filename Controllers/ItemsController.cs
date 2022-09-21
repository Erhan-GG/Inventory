using Common;
using Inventory.Clients;
using Inventory.DTOs;
using Inventory.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Inventory.DTOs.DTOs;

namespace Inventory.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IEntityRepository<InventoryItem> itemsRepository;
        private readonly CatalogClient catalogClient;
        public ItemsController(IEntityRepository<InventoryItem> itemsRepository, CatalogClient catalogClient)
        {
            this.itemsRepository = itemsRepository;
            this.catalogClient = catalogClient;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDTO>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var items = await itemsRepository.GetAllAsync(item => item.UserId == userId);
            var catalogItems = await catalogClient.GetCatalogItemsAsync();
            if(items.Any(item => !catalogItems.Select(catalogItem => catalogItem.Id).Contains(item.CatalogItemId)))
            {
                return BadRequest("catalog id not found in catalog service!");
            }

            var inventoryItemsDTO = items.Select(items =>
                {
                    var catalogItem = catalogItems.Single(catalogItem => 
                        catalogItem.Id == items.CatalogItemId
                    );
                    return items.AsDTO(catalogItem.Name, catalogItem.Description);
                }
            );
            return Ok(inventoryItemsDTO);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync(GrantItemsDTO grantItemsDTO)
        {
            var catalogItems =  await catalogClient.GetCatalogItemsAsync();

            if(catalogItems.Any(catalogItem => catalogItem.Id == grantItemsDTO.CatalogItemId) == false)
            {
                return BadRequest("CatalogItemId not found in catalog item database!");
            }

            var inventoryItem = await itemsRepository.GetAsync(item => item.UserId == grantItemsDTO.UserId && item.CatalogItemId == grantItemsDTO.CatalogItemId);

            if(inventoryItem == null)
            {
                var newInventoryItem = new InventoryItem
                {
                    UserId = grantItemsDTO.UserId,
                    CatalogItemId = grantItemsDTO.CatalogItemId,
                    Quantity = grantItemsDTO.Quantity,
                    AquiredDate = DateTimeOffset.UtcNow,
                };

                await itemsRepository.CreateAsync(newInventoryItem);
                return Ok();
            }

            inventoryItem.Quantity += grantItemsDTO.Quantity;
            await itemsRepository.UpdateAsync(inventoryItem);

            return Ok();
        }
    }
}
