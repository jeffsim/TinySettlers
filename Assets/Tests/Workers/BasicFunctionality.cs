using NUnit.Framework;

public partial class WorkerTests : TestBase
{
    [Test]
    public void Worker_CancelTaskOnUnassign()
    {
        // DropItem in storage room: unreserves storage spot
        LoadTestTown("worker_Unassign");
        var worker = Town.CreateWorkerInBuilding(Camp);

        waitUntilTask(worker, TaskType.TransportItemFromSpotToSpot);
        var task = worker.AI.CurrentTask as Task_TransportItemFromSpotToSpot;
        var item = task.SpotWithItemToPickup.ItemContainer.Item;
        var origStorageSpot = task.SpotToStoreItemIn;

        waitUntilSubtask(worker, typeof(Subtask_PickupItemFromItemSpot));
        Town.AssignWorkerToBuilding(StorageRoom);

        waitUntilSubtask(worker, typeof(Subtask_DropItemInItemSpot));
        var newStorageSpot = (task.CurSubTask as Subtask_DropItemInItemSpot).ItemSpot;
        waitUntilTaskDone(worker);

        verify_ItemInSpot(newStorageSpot, item);
        verify_spotIsUnreserved(task.SpotWithItemToPickup);
        verify_spotIsUnreserved(newStorageSpot);
        
        verify_ItemInSpot(origStorageSpot, null);
        verify_spotIsUnreserved(origStorageSpot);

        verify_ItemInHand(worker, null);
    }
}