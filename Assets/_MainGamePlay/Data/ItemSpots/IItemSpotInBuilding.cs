public interface IItemSpotInBuilding : IReservationProvider
{
    ItemContainerComponent ItemContainer { get; }
    LocationComponent Location { get; set; }
    BuildingData Building { get; set; }
}

public interface IMultipleItemSpotInBuilding : IReservationProvider
{
    MultipleItemContainerComponent ItemsContainer { get; }
    LocationComponent Location { get; set; }
    BuildingData Building { get; set; }
}
