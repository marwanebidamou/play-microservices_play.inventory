using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers
{
    public class SubstractItemsConsumer : IConsumer<SubstractItems>
    {
        private readonly IRepository<InventoryItem> itemsRepository;
        private readonly IRepository<CatalogItem> catalogItemsRepository;

        public SubstractItemsConsumer(IRepository<InventoryItem> itemsRepository, IRepository<CatalogItem> catalogItemsRepository)
        {
            this.itemsRepository = itemsRepository;
            this.catalogItemsRepository = catalogItemsRepository;
        }

        public async Task Consume(ConsumeContext<SubstractItems> context)
        {
            var message = context.Message;

            var item = await catalogItemsRepository.GetAsync(message.CatalogItemId);

            if (item == null)
            {
                throw new UnknownItemException(message.CatalogItemId);
            }

            var inventoryItem = await itemsRepository.GetAsync(
            item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);

         
            if (inventoryItem != null)
            {
                if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
                {
                    await context.Publish(new InventoryItemsSubstracted(message.CorrelationId));
                    return;
                }

                inventoryItem.Quantity -= message.Quantity;
                inventoryItem.MessageIds.Add(context.MessageId.Value);

                await itemsRepository.UpdateAsync(inventoryItem);

                await context.Publish(new InventoryItemUpdated(
                    inventoryItem.UserId,
                    inventoryItem.CatalogItemId,
                    inventoryItem.Quantity
                    ));
            }

            await context.Publish(new InventoryItemsSubstracted(message.CorrelationId));
        }

  
    }
}
