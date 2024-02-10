using NUnit.Framework;

public partial class CraftingStationTests : MovePauseDestroyTestBase
{
    [Test]
    public void CraftingStation_BasicFunctionality()
    {
        // Test that worker in a crafting station that has enough resources will craft an item
        LoadTestTown("craftingstation_MovePauseDestroy");
        var worker = Town.CreateWorkerInBuilding(CraftingStation);
        waitUntilTask(worker, TaskType.Task_CraftItem);
        waitUntilTaskDone(worker);

        verify_ItemInHand(worker, null);
        var itemDefn = CraftingStation.Defn.CraftableItems[0];
        var storageSpot = CraftingStation.GetClosestUnreservedStorageSpotWithItem(worker.Location, itemDefn);
        verify_ItemTypeInSpot(storageSpot, itemDefn, "Item should be in new storage spot");
    }
}