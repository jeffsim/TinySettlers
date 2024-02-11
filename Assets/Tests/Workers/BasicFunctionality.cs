using NUnit.Framework;

public partial class WorkerTests : TestBase
{
    [Test]
    public void Worker_UnassignDuringTransportTask()
    {
        // subtask:
        // 0: Unassign while worker is walking to item to pick up
        // 1: Unassign while worker is picking up item
        // 2: [ignore: instant-complete unreserve of spot item was originally in]
        // 3: Unassign while worker is carrying item to (new) storage spot
        // 4: Unassign while worker is dropping item in (new) storage spot
        for (int subtask = 0; subtask < 5; subtask++)
        {
            if (subtask == 2) continue;

            LoadTestTown("worker_Unassign", subtask);
            var worker = Town.CreateWorkerInBuilding(Camp);

            // Wait until the worker starts the process
            waitUntilTask(worker, TaskType.TransportItemFromSpotToSpot);

            // At this point, the worker is assigned to the Camp and is walking to the woodcutter to pick up the wood in its storage
            // and deliver it to store in the storeroom
            var originalTask = getWorkerCurrentTaskAsType<Task_TransportItemFromSpotToSpot>(worker);
            var item = originalTask.SpotWithItemToPickup.ItemContainer.Item;
            var origSpotToStoreItemIn = originalTask.SpotToStoreItemIn;
            var origSpotWithItemToPickup = originalTask.SpotWithItemToPickup;
            verify_BuildingsAreEqual(origSpotToStoreItemIn.Building, StorageRoom);
            verify_BuildingsAreEqual(origSpotWithItemToPickup.Building, WoodcuttersHut);
            verify_AssignedBuilding(worker, Camp);

            // Wait until worker gets into the state that we want to test
            waitUntilTaskAndSubtaskIndex(worker, TaskType.TransportItemFromSpotToSpot, subtask);

            switch (subtask)
            {
                case 0: // Unassigned during Subtask_WalkToItemSpot
                    break;
                case 1: // Unassigned during Subtask_PickupItemFromItemSpot
                    break;
                case 3: // Unassigned during Subtask_WalkToItemSpot
                    break;
                case 4: // Unassigned during Subtask_DropItemInItemSpot
                    break;
            }
            var hasPickedupItem = subtask > 2;

            // Reassign worker to storageroom.  This will cause them to abandon their current task and eventually restart it but now as a Store worker
            Town.AssignWorkerToBuilding(StorageRoom);

            getWorkerCurrentTaskAsType<Task_Idle>(worker, $"{preface("", 1)} Worker should have abandoned their task and be idle");
            verify_spotIsUnreserved(origSpotWithItemToPickup);
            verify_AssignedBuilding(worker, StorageRoom);
            verify_spotIsUnreserved(origSpotToStoreItemIn);
            verify_ItemInSpot(origSpotWithItemToPickup, hasPickedupItem ? null : item);
            verify_ItemInHand(worker, hasPickedupItem ? item : null);

            // Wait until worker decides to start transporting the item again (but now as a worker in the store)
            waitUntilTask(worker, hasPickedupItem ? TaskType.DeliverItemInHandToStorageSpot : TaskType.TransportItemFromSpotToSpot);

            verify_ItemInHand(worker, hasPickedupItem ? item : null);

            IItemSpotInBuilding newSpotToStoreItemIn;
            if (hasPickedupItem)
            {
                var newTask = getWorkerCurrentTaskAsType<Task_DeliverItemInHandToStorageSpot>(worker, $"{preface("", 1)} Worker should be carrying item to storage spot");
                newSpotToStoreItemIn = newTask.SpotToStoreItemIn;
                verify_BuildingsAreEqual(newSpotToStoreItemIn.Building, StorageRoom);
                verify_spotIsUnreserved(origSpotWithItemToPickup, $"{preface("", 1)} Original spot with item should be unreserved");
            }
            else
            {
                var newTask = getWorkerCurrentTaskAsType<Task_TransportItemFromSpotToSpot>(worker, $"{preface("", 1)} Worker should be transporting again");
                var item2 = newTask.SpotWithItemToPickup.ItemContainer.Item;
                newSpotToStoreItemIn = newTask.SpotToStoreItemIn;
                var newSpotWithItemToPickup = newTask.SpotWithItemToPickup;
                verify_ItemsAreEqual(item, item2);
                verify_BuildingsAreEqual(newSpotToStoreItemIn.Building, StorageRoom);
                verify_BuildingsAreEqual(newSpotWithItemToPickup.Building, WoodcuttersHut);
            }

            // Let worker finish transporting the item to the storage room
            waitUntilTaskDone(worker);
            getWorkerCurrentTaskAsType<Task_Idle>(worker, $"{preface("", 1)} Worker should be idle again after transporting item");

            verify_spotIsUnreserved(origSpotWithItemToPickup, $"{preface("", 1)} Original spot with item should be unreserved");
            verify_spotIsUnreserved(origSpotToStoreItemIn, $"{preface("", 1)} Original storage spot should be unreserved");
            verify_spotIsUnreserved(newSpotToStoreItemIn, $"{preface("", 1)} Final storage spot should be unreserved");

            verify_ItemInSpot(newSpotToStoreItemIn, item, $"{preface("", 1)} Final storage spot should have the item");
            if (newSpotToStoreItemIn != origSpotToStoreItemIn)
                verify_ItemInSpot(origSpotToStoreItemIn, null, $"{preface("", 1)} Original storage spot should be empty");
            verify_ItemInHand(worker, null, $"{preface("", 1)} Worker should not have the item in hand");
        }
    }
}