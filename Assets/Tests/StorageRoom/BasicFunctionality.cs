using NUnit.Framework;

public partial class StorageRoomTests : TestBase
{
    [Test]
    public void StorageRoom_BasicFunctionality()
    {
        // DropItem in storage room: unreserves storage spot
        LoadTestTown("storageRoom_assignedworker");
        var testBuilding = StorageRoom;
        var worker = getAssignedWorker(testBuilding);
        waitUntilTask(worker, TaskType.PickupItemInStorageSpot);
        var reservedStorageSpot = getStorageSpotInBuildingReservedByWorker(testBuilding, worker);
        waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_DropItemInItemSpot));
        waitUntilTaskDone(worker);
        verify_spotIsUnreserved(reservedStorageSpot);
    }
}