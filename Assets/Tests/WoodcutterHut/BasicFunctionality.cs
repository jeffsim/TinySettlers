using NUnit.Framework;

public partial class WoodcutterHutTests : MovePauseDestroyTestBase
{
    [Test]
    public void Woodcutter_BasicFunctionality()
    {
        // DropItem in storage room: unreserves storage spot
        LoadTestTown("woodcutter_MovePauseDestroy");
        var worker = Town.CreateWorkerInBuilding(WoodcuttersHut);

        waitUntilTask(worker, TaskType.GatherResource);
        var task = worker.AI.CurrentTask as Task_GatherResource;
        var item = task.SpotToGatherFrom.ItemContainer.Item;
        var storageSpot = task.SpotToStoreItemIn;
        waitUntilTaskDone(worker);
        
        verify_spotIsUnreserved(storageSpot);
        verify_ItemInHand(worker, null);
        verify_ItemInSpot(storageSpot, item, "Item should be in new storage spot");
    }
}