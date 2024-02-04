using System;
using NUnit.Framework;

public class StorageRoomTests : TestBase
{
    [Test]
    public void StorageRoomBasic()
    {
        // DropItem in storage room: unreserves storage spot
        LoadTestTown("storageRoom_1");
        var testBuilding = StorageRoom;
        var worker = getAssignedWorker(testBuilding);
        waitUntilTask(worker, TaskType.PickupItemInStorageSpot);
        var reservedStorageSpot = getStorageSpotInBuildingReservedByWorker(testBuilding, worker);
        waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_DropItemInItemSpot));
        waitUntilTaskDone(worker);
        verify_spotIsUnreserved(reservedStorageSpot);
    }

    [Test]
    public void StorageRoom_PauseBuilding()
    {
        //   Pause storageroom while assigned worker is getting an item to store in the room
        //   i=0: Pause storage while assigned worker is walking to another building to pick something up to store in storage room
        //         => Storage spot in storage room is unreserved, storage spot in other building is unreserved, item is still in other storage spot, worker is idle
        //   i=1: Pause storage while assigned worker is picking up item in another building to store in storage room
        //         => Storage spot in storage room is unreserved, storage spot in other building is unreserved, item is still in other storage spot, worker is idle
        //   i=2: Pause storage while assigned worker is delivering item that it picked up in another building to store in storage room
        //         => Storage spot in storage room is unreserved, storage spot in other building is unreserved, Item should be in worker's hands, they should be carrying it to Camp
        //   i=3: Pause storage while assigned worker is dropping off something in storageroom
        //         => Storage spot in storage room is unreserved, storage spot in other building is unreserved, Item should be in worker's hands, they should be carrying it to Camp

        for (int i = 0; i < 4; i++)
        {
            LoadTestTown("storageRoom_1", i);

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
            var originalStorageSpotInTestBuilding = getStorageSpotInBuildingReservedByWorker(testBuilding, worker);

            testBuilding.TogglePaused();

            // Verify new state.
            verify_WorkerTaskType(TaskType.Idle, worker);
            verify_spotIsUnreserved(originalStorageSpotInBuildingWithItem, "Storage spot that originally contained the item should be unreserved in all cases");
            verify_spotIsUnreserved(originalStorageSpotInTestBuilding, "Storage spot that item was going to be stored in should be unreserved in all cases");
            verify_ItemInStorageSpot(originalStorageSpotInTestBuilding, null);

            switch (i)
            {
                case 0: // WorkerSubtask_WalkToItemSpot.  Storage spot in storage room is unreserved, storage spot in other building is unreserved, item is still in other storage spot, worker is idle
                    verify_ItemDefnInHand(worker, null);
                    verify_ItemInStorageSpot(originalStorageSpotInBuildingWithItem, itemToBePickedUp);
                    break;

                case 1: // WorkerSubtask_PickupItemFromBuilding.  Storage spot in storage room is unreserved, storage spot in other building is unreserved, item is still in other storage spot, worker is idle
                    verify_ItemDefnInHand(worker, null);
                    verify_ItemInStorageSpot(originalStorageSpotInBuildingWithItem, itemToBePickedUp);
                    break;

                case 2: // WorkerSubtask_WalkToItemSpot.  Storage spot in storage room is unreserved, storage spot in other building is unreserved, Item should be in worker's hands, they should be carrying it to Camp
                    verify_ItemInHand(worker, itemToBePickedUp);
                    verify_WorkerTaskTypeAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot), "Should be carrying item to Camp now");
                    verify_BuildingsAreEqual(((WorkerSubtask_WalkToItemSpot)worker.AI.CurrentTask.CurSubTask).ItemSpot.Building, Camp);
                    verify_ItemInStorageSpot(originalStorageSpotInBuildingWithItem, null);
                    break;

                case 3: // WorkerSubtask_DropItemInItemSpot.  Storage spot in storage room is unreserved, storage spot in other building is unreserved, Item should be in worker's hands, they should be carrying it to Camp
                    verify_ItemInHand(worker, itemToBePickedUp);
                    verify_WorkerTaskTypeAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot), "Should be carrying item to Camp now");
                    verify_BuildingsAreEqual(((WorkerSubtask_WalkToItemSpot)worker.AI.CurrentTask.CurSubTask).ItemSpot.Building, Camp);
                    verify_ItemInStorageSpot(originalStorageSpotInBuildingWithItem, null);
                    break;
            }
        }
    }

    [Test]
    public void StorageRoom_MoveBuilding()
    {
        //   Move storageroom while assigned worker is getting an item to store in the room
        //   i=0: Move storage while assigned worker is walking to another building to pick something up to store in storage room
        //             verify spot remains reserved, worker remains walking to other building, Location and MoveTarget are unchanged
        //   i=1: Move storage while assigned worker is picking up item in another building to store in storage room
        //             verify spot remains reserved, worker remains walking to it, Location and MoveTarget are unchanged
        //   i=2: Move storage while assigned worker is delivering item that it picked up in another building to store in storage room
        //             verify spot remains reserved, worker remains walking to it, movetarget has been updated, Location is unchanged
        //   i=3: Move storage while assigned worker is dropping off something in storageroom
        //             verify spot remains reserved, worker remains walking to it, movetarget is unchanged, Location has been updated
        //   Same as above, but using worker from another room.

        for (int i = 0; i < 4; i++)
        {
            LoadTestTown("storageRoom_1", i);

            // Town starts out with a camp, a storage room (with 1 worker), and a woodcutter's hut (with 1 woodplank in it). 
            // The worker will pick up the woodplank and store it in the storage room.

            var testBuilding = StorageRoom;
            var worker = getAssignedWorker(testBuilding);

            // Wait until the worker gets to the to-be-tested subtask
            switch (i)
            {
                case 0: waitUntilTaskAndSubtask(worker, TaskType.PickupItemInStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
                case 1: waitUntilTaskAndSubtask(worker, TaskType.PickupItemInStorageSpot, typeof(WorkerSubtask_PickupItemFromBuilding)); break;
                case 2: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
                case 3: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_DropItemInItemSpot)); break;
            }

            // Storage original state prior to making the change we're testing
            var originalStorageSpotInTestBuilding = getStorageSpotInBuildingReservedByWorker(testBuilding, worker);
            var workerOriginalLoc = worker.Location;
            var workerOriginalMoveTarget = worker.AI.CurrentTask.LastMoveToTarget;
            var workerOriginalTask = worker.AI.CurrentTask.Type;
            var workerOriginalSubtask = worker.AI.CurrentTask.CurSubTask.GetType();

            var workerOriginalLocRelativeToBuilding = worker.Location.WorldLoc - testBuilding.Location.WorldLoc;
            var workerOriginalTargetRelativeToBuilding = worker.AI.CurrentTask.LastMoveToTarget.WorldLoc - testBuilding.Location.WorldLoc;

            Town.MoveBuilding(testBuilding, 2, 0);

            // Verify new state.  First verify storage spot in room remains reserved
            verify_spotStillReservedByWorker(originalStorageSpotInTestBuilding, testBuilding, worker);
            verify_WorkerTaskTypeAndSubtask(worker, workerOriginalTask, workerOriginalSubtask);

            var workerNewLocRelativeToBuilding = worker.Location.WorldLoc - testBuilding.Location.WorldLoc;
            var workerNewMoveTargetRelativeToBuilding = worker.AI.CurrentTask.LastMoveToTarget.WorldLoc - testBuilding.Location.WorldLoc;

            switch (i)
            {
                case 0: // WorkerSubtask_WalkToItemSpot.  Verify worker task and subtask remain unchanged, Location and MoveTarget are unchanged
                    verify_LocsAreEqual(workerOriginalLoc, worker.Location);
                    verify_LocsAreEqual(workerOriginalMoveTarget, worker.AI.CurrentTask.LastMoveToTarget);
                    break;

                case 1: // WorkerSubtask_PickupItemFromBuilding.  Verify worker task and subtask remain unchanged, Location and MoveTarget are unchanged
                    verify_LocsAreEqual(workerOriginalLoc, worker.Location);
                    verify_LocsAreEqual(workerOriginalMoveTarget, worker.AI.CurrentTask.LastMoveToTarget);
                    break;

                case 2: // WorkerSubtask_WalkToItemSpot.  Verify worker task and subtask remain unchanged, movetarget has been updated, Location is unchanged
                    verify_LocsAreEqual(workerOriginalLoc, worker.Location);
                    verify_LocsAreEqual(workerOriginalTargetRelativeToBuilding, workerNewMoveTargetRelativeToBuilding);
                    break;

                case 3: // WorkerSubtask_DropItemInItemSpot.  Verify worker task and subtask remain unchanged, movetarget is unchanged, Location has been updated
                    verify_LocsAreEqual(workerOriginalLocRelativeToBuilding, workerNewLocRelativeToBuilding);
                    verify_LocsAreEqual(workerOriginalMoveTarget, worker.AI.CurrentTask.LastMoveToTarget);
                    break;
            }
        }
    }
}