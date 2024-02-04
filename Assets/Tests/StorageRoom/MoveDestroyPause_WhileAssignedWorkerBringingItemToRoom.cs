using NUnit.Framework;

public partial class StorageRoomTests : TestBase
{
    [Test]
    public void StorageRoom_DestroyBuilding_WhileAssignedWorkerBringingItemToRoom()
    {
        //   Destroy storageroom while assigned worker is getting an item to store in the room
        //   i=0: Destroy storage while assigned worker is walking to another building to pick something up to store in storage room
        //         => Storage spot in other building is unreserved, item is still in other storage spot, worker is idle and assigned to Camp
        //   i=1: Destroy storage while assigned worker is picking up item in another building to store in storage room
        //         => Storage spot in other building is unreserved, item is still in other storage spot, worker is idle and assigned to Camp
        //   i=2: Destroy storage while assigned worker is delivering item that it picked up in another building to store in storage room
        //         => Storage spot in other building is unreserved, Item should be in worker's hands, they should be carrying it to Camp and assigned to Camp
        //   i=3: Destroy storage while assigned worker is dropping off something in storageroom
        //         => Storage spot in other building is unreserved, Item should be in worker's hands, they should be carrying it to Camp and assigned to Camp

        for (int i = 0; i < 4; i++)
        {
            LoadTestTown("storageRoom_assignedworker", i);

            // Town starts out with a camp, a storage room (with 1 worker), and a woodcutter's hut (with 1 woodplank in it). 
            // The worker will pick up the woodplank and store it in the storage room.

            var testBuilding = StorageRoom;
            var buildingWithItem = WoodcuttersHut;
            var worker = getAssignedWorker(testBuilding);
            var itemToBePickedUp = buildingWithItem.GetUnreservedItemInStorage(GameDefns.Instance.ItemDefns["plank"]);
            var originalStorageSpotInBuildingWithItem = getStorageSpotInBuildingWithItem(buildingWithItem, itemToBePickedUp);

            // Wait until the worker gets to the to-be-tested subtask
            switch (i)
            {
                case 0: waitUntilTaskAndSubtask(worker, TaskType.PickupItemInStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
                case 1: waitUntilTaskAndSubtask(worker, TaskType.PickupItemInStorageSpot, typeof(WorkerSubtask_PickupItemFromBuilding)); break;
                case 2: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
                case 3: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_DropItemInItemSpot)); break;
            }

            Town.DestroyBuilding(testBuilding);

            // If the worker is returning with the item in hand, then we need to wait one Town turn so that the worker can decide to carry the item they're holding to the Camp.
            if (i >= 2)
                updateTown();

            // Verify new state.
            verify_spotIsUnreserved(originalStorageSpotInBuildingWithItem, "Storage spot that originally contained the item should be unreserved in all cases");
            verify_AssignedBuilding(worker, Camp);

            switch (i)
            {
                case 0: // WorkerSubtask_WalkToItemSpot.  Storage spot in other building is unreserved, item is still in other storage spot, worker is idle
                    verify_ItemDefnInHand(worker, null);
                    verify_ItemInStorageSpot(originalStorageSpotInBuildingWithItem, itemToBePickedUp);
                    verify_WorkerTaskType(TaskType.Idle, worker);
                    break;

                case 1: // WorkerSubtask_PickupItemFromBuilding. Storage spot in other building is unreserved, item is still in other storage spot, worker is idle
                    verify_ItemDefnInHand(worker, null);
                    verify_ItemInStorageSpot(originalStorageSpotInBuildingWithItem, itemToBePickedUp);
                    verify_WorkerTaskType(TaskType.Idle, worker);
                    break;

                case 2: // WorkerSubtask_WalkToItemSpot. Storage spot in other building is unreserved, Item should be in worker's hands, they should be carrying it to Camp and assigned to Camp
                    verify_ItemInHand(worker, itemToBePickedUp);
                    verify_WorkerTaskTypeAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot), "Should be carrying item to Camp now");
                    verify_BuildingsAreEqual(((WorkerSubtask_WalkToItemSpot)worker.AI.CurrentTask.CurSubTask).ItemSpot.Building, Camp);
                    verify_ItemInStorageSpot(originalStorageSpotInBuildingWithItem, null);
                    break;

                case 3: // WorkerSubtask_DropItemInItemSpot. Storage spot in other building is unreserved, Item should be in worker's hands, they should be carrying it to Camp and assigned to Camp
                    verify_ItemInHand(worker, itemToBePickedUp);
                    verify_WorkerTaskTypeAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot), "Should be carrying item to Camp now");
                    verify_BuildingsAreEqual(((WorkerSubtask_WalkToItemSpot)worker.AI.CurrentTask.CurSubTask).ItemSpot.Building, Camp);
                    verify_ItemInStorageSpot(originalStorageSpotInBuildingWithItem, null);
                    break;
            }
        }
    }
}