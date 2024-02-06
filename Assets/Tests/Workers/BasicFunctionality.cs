// using NUnit.Framework;

// public partial class WorkerTests : TestBase
// {

//     // * RESET; while woodsman is walking to forest, SAVE; LOAD; unassign. still goes to forest before switching to camp
//     //   - abandon worker.task on assign.
//     //   - DOES work w/o the save/load.  Why?
//     //   - write test
//     [Test]
//     public void Worker_CancelTaskOnUnassign()
//     {
//         // DropItem in storage room: unreserves storage spot
//         LoadTestTown("woodcutter_MovePauseDestroy");
//         var worker = Town.CreateWorkerInBuilding(WoodcuttersHut);

//         waitUntilTask(worker, TaskType.PickupGatherableResource);
//         var item = (worker.AI.CurrentTask as BaseWorkerTask_TransportItemFromSpotToStorage).SpotWithItemToPickup.ItemContainer.Item;
//         waitUntilTask(worker, TaskType.DeliverItemInHandToStorageSpot);
//         var storageSpot = worker.StorageSpotReservedForItemInHand;
//         waitUntilTaskDone(worker);

//         verify_spotIsUnreserved(storageSpot);
//         verify_ItemInHand(worker, null);
//         verify_ItemInSpot(storageSpot, item, "Item should be in new storage spot");
//     }
// }