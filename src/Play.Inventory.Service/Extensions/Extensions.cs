using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Extensions
{
    public static class Extensions
    {
        public static InventoryItemDto AsDto(this InventoryItem item, string name, string desciption)
        {
            return new InventoryItemDto(item.CatalogItemId,name,desciption, item.Quantity, item.AcquiredDate);
        }
    }
}