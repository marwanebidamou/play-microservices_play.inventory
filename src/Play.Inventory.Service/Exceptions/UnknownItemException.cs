namespace Play.Inventory.Service.Exceptions
{
    public class UnknownItemException : Exception
    {
        private Guid ItemId;


        public UnknownItemException(Guid itemId) 
            : base($"Uknown item {itemId}")
        {
            ItemId = itemId;
        }
    }
}
