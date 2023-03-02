using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers
{
    public class GrantItemsConsumer : IConsumer<GrantItems>
    {
        private readonly IRepository<InventoryItem> itemsRepository;
        private readonly IRepository<CatalogItem> catalogItemsRepository;

        public GrantItemsConsumer(IRepository<InventoryItem> itemsRepository, IRepository<CatalogItem> catalogItemsRepository)
        {
            this.itemsRepository = itemsRepository;
            this.catalogItemsRepository = catalogItemsRepository;
        }

        public async Task Consume(ConsumeContext<GrantItems> context)
        {
            var message = context.Message;

            var item = await catalogItemsRepository.GetAsync(message.CatalogItemId);

            if (item == null)
            {
                throw new UnknownItemException(message.CatalogItemId);
            }

            var inventoryItem = await itemsRepository.GetAsync(
            item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = message.CatalogItemId,
                    UserId = message.UserId,
                    Quantity = message.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow,
                };

                inventoryItem.MessageIds.Add(context.MessageId.Value);

                await itemsRepository.CreateAsync(inventoryItem);
            }
            else
            {
                if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
                {
                    await context.Publish(new InventoryItemsGranted(message.CorrelationId));
                    return;
                }

                inventoryItem.Quantity += message.Quantity;
                await itemsRepository.UpdateAsync(inventoryItem);
            }

            var itemGrantedTask = context.Publish(new InventoryItemsGranted(message.CorrelationId)); ;
            var inventoryItemUpdated = context.Publish(new InventoryItemUpdated(
                inventoryItem.UserId,
                inventoryItem.CatalogItemId,
                inventoryItem.Quantity
                ));


            await Task.WhenAll(itemGrantedTask, inventoryItemUpdated);
        }
    }
}
