namespace Play.Inventory.Contracts
{
   public record GrantItems(
       Guid UserId,
       Guid CatalogItemId,
       int Quantity,
       //Correlation Id is what the state machine is going to use to
       //correlate the different messages to belong to one specific
       //instance of the state machine
       //without it the state machine won't know how to map the messages to the different
       //instances of the state machine that could be happening at any given time
       //=> so all the messages that belong to one specific transaction should have the same correlation ID
       Guid CorrelationId
       );

    public record InventoryItemsGranted
        (
        Guid CorrelationId
        );

    public record SubstractItems(
       Guid UserId,
       Guid CatalogItemId,
       int Quantity,
       Guid CorrelationId
     );

    public record InventoryItemsSubstracted
    (
    Guid CorrelationId
    );

    public record InventoryItemUpdated
    (
        Guid UserId,
        Guid CatalogItemId,
        int NewTotalQuantity
    );
}