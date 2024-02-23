public interface IItemSpotInBuilding : IReservable
{
    SingleContainable ItemContainer { get; }
    Location Location { get; set; }
    BuildingData Building { get; set; }
}

public interface IMultipleItemSpotInBuilding : IReservable
{
    MultipleContainable ItemsContainer { get; }
    Location Location { get; set; }
    BuildingData Building { get; set; }
}
