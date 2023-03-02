using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<InventoryItem> itemsRepository;
        private readonly IRepository<CatalogItem> catalogItemsRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private const string AdminRole = "Admin";

        public ItemsController(IRepository<InventoryItem> itemsRepository, IRepository<CatalogItem> catalogItemsRepository, IPublishEndpoint publishEndpoint)
        {
            this.itemsRepository = itemsRepository;
            this.catalogItemsRepository = catalogItemsRepository;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {

            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (Guid.Parse(currentUserId) != userId)
            {
                if (!User.IsInRole(AdminRole))
                {
                    return Forbid();
                }
            }

            var inventoryItemEntities = await itemsRepository.GetAllAsync(item => item.UserId == userId);
            var catalogItemIds = inventoryItemEntities.Select(x => x.CatalogItemId).Distinct();
            var catalogItemsEntities = await catalogItemsRepository.GetAllAsync(x => catalogItemIds.Contains(x.Id));

            var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItemsEntities.Single(x => x.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });

            return Ok(inventoryItemDtos);
        }

        [HttpPost]
        [Authorize(Roles = AdminRole)]
        public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
        {
            var inventoryItem = await itemsRepository.GetAsync(
                item => item.UserId == grantItemsDto.UserId && item.CatalogItemId == grantItemsDto.CatalogItemId);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = grantItemsDto.CatalogItemId,
                    UserId = grantItemsDto.UserId,
                    Quantity = grantItemsDto.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                await itemsRepository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity += grantItemsDto.Quantity;
                await itemsRepository.UpdateAsync(inventoryItem);
            }

            await _publishEndpoint.Publish(new InventoryItemUpdated(
                  inventoryItem.UserId,
                  inventoryItem.CatalogItemId,
                  inventoryItem.Quantity
                  ));

            return Ok();
        }
    }
}