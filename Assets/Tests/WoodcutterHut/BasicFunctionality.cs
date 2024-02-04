// using NUnit.Framework;

// public partial class StorageRoomTests : TestBase
// {
//     [Test]
//     public void StorageRoom_BasicFunctionality()
//     {
//         // DropItem in storage room: unreserves storage spot
//         LoadTestTown("storageRoom_BasicFunctionality");
//         var storage = StorageRoom;
//         var worker = getAssignedWorker(storage);
//         var originalStorageSpot = WoodcuttersHut.GetStorageSpotWithUnreservedItem(GameDefns.Instance.ItemDefns["plank"]);
//         var itemToStore = originalStorageSpot.ItemContainer.Item;

//         waitUntilTask(worker, TaskType.PickupItemInStorageSpot);
        
//         var reservedStorageSpot = getStorageSpotInBuildingReservedByWorker(storage, worker);
        
//         waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_DropItemInItemSpot));
//         waitUntilTaskDone(worker);
        
//         verify_spotIsUnreserved(reservedStorageSpot);
//         verify_ItemInStorageSpot(reservedStorageSpot, itemToStore, "Item should be in new storage spot");
//         verify_ItemInStorageSpot(originalStorageSpot, null, "Item should not be in original storage spot");
//     }
// }