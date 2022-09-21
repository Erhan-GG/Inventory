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
        private readonly IEntityRepository<InventoryItem> inventoryItemsRepository;
        //private readonly CatalogClient catalogClient;
        private readonly IEntityRepository<CatalogItem> catalogItemsRepository;
        public ItemsController(IEntityRepository<InventoryItem> itemsRepository, IEntityRepository<CatalogItem> catalogItemsRepository)
        {
            this.inventoryItemsRepository = itemsRepository;
            this.catalogItemsRepository = catalogItemsRepository;
            //this.catalogClient = catalogClient;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDTO>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var inventoryItemEntities = await inventoryItemsRepository.GetAllAsync(item => item.UserId == userId);

            var catalogItemIds = inventoryItemEntities.Select(item => item.CatalogItemId);

            var catalogItemEntities = await catalogItemsRepository.GetAllAsync(item => catalogItemIds.Contains(item.Id));

            var missingCatalogIds = new List<Guid>();
            foreach(var catalogItemId in catalogItemIds)
            {
                if(!catalogItemEntities.Any(catalogItem => catalogItem.Id == catalogItemId))
                {
                    missingCatalogIds.Add(catalogItemId);
                }
            }
            if(missingCatalogIds.Any())
            {
                return BadRequest($"catalog id's: * {string.Join(",",missingCatalogIds.ToArray())} * not found in catalog service!");
            }

            //var catalogItems = await catalogClient.GetCatalogItemsAsync();

            //if (inventoryItemEntities.Any(item => !catalogItems.Select(catalogItem => catalogItem.Id).Contains(item.CatalogItemId)))
            //{
            //    return BadRequest("catalog id not found in catalog service!");
            //}

            var inventoryItemsDTO = inventoryItemEntities.Select(items =>
                {
                    var catalogItem = catalogItemEntities.Single(catalogItem => 
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
            if(grantItemsDTO.Quantity <= 0)
                return BadRequest("Quantity can not be 0 or lower!");


            //var catalogItems =  await catalogClient.GetCatalogItemsAsync();
            var catalogItem = await catalogItemsRepository.GetAsync(catalogItem => catalogItem.Id == grantItemsDTO.CatalogItemId);

            if(catalogItem == null)
                return BadRequest("CatalogItemId not found in catalog item database!");


            //if(catalogItems.Any(catalogItem => catalogItem.Id == grantItemsDTO.CatalogItemId) == false)
            //{
            //    return BadRequest("CatalogItemId not found in catalog item database!");
            //}

            var inventoryItem = await inventoryItemsRepository.GetAsync(item => item.UserId == grantItemsDTO.UserId && item.CatalogItemId == grantItemsDTO.CatalogItemId);

            if(inventoryItem == null)
            {
                var newInventoryItem = new InventoryItem
                {
                    UserId = grantItemsDTO.UserId,
                    CatalogItemId = grantItemsDTO.CatalogItemId,
                    Quantity = grantItemsDTO.Quantity,
                    AquiredDate = DateTimeOffset.UtcNow,
                };

                await inventoryItemsRepository.CreateAsync(newInventoryItem);
                return Ok();
            }

            inventoryItem.Quantity += grantItemsDTO.Quantity;
            await inventoryItemsRepository.UpdateAsync(inventoryItem);

            return Ok();
        }
    }
}
