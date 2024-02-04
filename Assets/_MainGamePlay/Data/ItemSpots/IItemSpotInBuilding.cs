public interface IItemSpotInBuilding : IReservationProvider
{
    ItemContainerComponent ItemContainer { get; }
    LocationComponent Location { get; set; }
    BuildingData Building { get; set; }
}
