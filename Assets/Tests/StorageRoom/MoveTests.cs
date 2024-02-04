using NUnit.Framework;
using UnityEngine;

public partial class StorageRoomTests : TestBase
{
    [Test]
    public void StorageRoom_MoveTests()
    {
        //   subtask=0: Move [buildingToMove] while [workerToTest] is walking to [buildingWithItem] to pick something up to store in [buildingToStoreItemIn]
        //   subtask=1: Move [buildingToMove] while [workerToTest] is picking up item in [buildingWithItem] to store in [buildingToStoreItemIn]
        //   subtask=2: Move [buildingToMove] while [workerToTest] is walking to [buildingToStoreItemIn]
        //   subtask=3: Move [buildingToMove] while [workerToTest] is dropping item in [buildingToStoreItemIn]
        for (int subtask = 0; subtask < 4; subtask++)
        {
            // Test A: Move store1     while worker1 is getting an item from woodcutter to store in store1
            // Test B: Move store1     while worker2 is getting an item from woodcutter to store in store1
            // Test C: Move store2     while worker2 is getting an item from woodcutter to store in store1
            // Test D: Move woodcutter while worker1 is getting an item from woodcutter to store in store1
            // Test E: Move woodcutter while worker1 is getting an item from woodcutter to store in store2
            BuildingData store1, store2;
            SetupMoveTest(subtask, out store1, out store2); runMoveTest("Test A", subtask, store1, store1, WoodcuttersHut, store1);
            SetupMoveTest(subtask, out store1, out store2); runMoveTest("Test B", subtask, store1, store2, WoodcuttersHut, store1);
            SetupMoveTest(subtask, out store1, out store2); runMoveTest("Test C", subtask, store2, store2, WoodcuttersHut, store1);
            SetupMoveTest(subtask, out store1, out store2); runMoveTest("Test D", subtask, WoodcuttersHut, store1, WoodcuttersHut, store1);
            SetupMoveTest(subtask, out store1, out store2); runMoveTest("Test E", subtask, WoodcuttersHut, store1, WoodcuttersHut, store2);
        }
    }

    void runMoveTest(string testName, int workerSubtask, BuildingData buildingToMove, BuildingData buildingWorker, BuildingData buildingWithItem, BuildingData buildingToStoreItemIn)
    {
        TestName = $"{testName}: Move {buildingToMove.TestId} while {buildingWorker.TestId}'s worker is ";
        switch (workerSubtask)
        {
            case 0: TestName += $"walking to {buildingWithItem.TestId} to pickup item and bring to {buildingToStoreItemIn.TestId}"; break;
            case 1: TestName += $"picking up item in {buildingWithItem.TestId} to bring to {buildingToStoreItemIn.TestId}"; break;
            case 2: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId}"; break;
            case 3: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId}"; break;
        }
        TestName += "\n  ";

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = Town.CreateWorkerInBuilding(buildingWorker);
        switch (workerSubtask)
        {
            case 0: waitUntilTaskAndSubtask(worker, TaskType.PickupItemInStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
            case 1: waitUntilTaskAndSubtask(worker, TaskType.PickupItemInStorageSpot, typeof(WorkerSubtask_PickupItemFromBuilding)); break;
            case 2: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
            case 3: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_DropItemInItemSpot)); break;
        }

        var movedBuildingWithItemInIt = buildingWithItem == buildingToMove;
        var movedBuildingItemWillBeStoredIn = buildingToStoreItemIn == buildingToMove;

        // Storage original state prior to making the change we're testing
        var originalSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
        var workerOriginalLoc = worker.Location;
        var workerOriginalMoveTarget = worker.AI.CurrentTask.LastMoveToTarget;
        var workerOriginalTask = worker.AI.CurrentTask.Type;
        var workerOriginalSubtask = worker.AI.CurrentTask.CurSubTask.GetType();

        var workerOriginalLocRelativeToBuilding = worker.Location.WorldLoc - buildingToMove.Location.WorldLoc;
        var workerOriginalTargetRelativeToBuilding = worker.AI.CurrentTask.LastMoveToTarget.WorldLoc - buildingToMove.Location.WorldLoc;

        Town.MoveBuilding(buildingToMove, 2, 0);

        // Verify new state.  First verify storage spot in room remains reserved
        verify_spotStillReservedByWorker(originalSpotToStoreItemIn, buildingToStoreItemIn, worker);
        verify_WorkerTaskTypeAndSubtask(worker, workerOriginalTask, workerOriginalSubtask);

        var workerNewLocRelativeToBuilding = worker.Location.WorldLoc - buildingToMove.Location.WorldLoc;
        var workerNewMoveTargetRelativeToBuilding = worker.AI.CurrentTask.LastMoveToTarget.WorldLoc - buildingToMove.Location.WorldLoc;

        switch (workerSubtask)
        {
            case 0: // WorkerSubtask_WalkToItemSpot.
                verify_LocsAreEqual(workerOriginalLoc, worker.Location);
                if (movedBuildingWithItemInIt)
                    verify_LocsAreEqual(workerOriginalTargetRelativeToBuilding, workerNewMoveTargetRelativeToBuilding);
                break;

            case 1: // WorkerSubtask_PickupItemFromBuilding.
                if (movedBuildingWithItemInIt)
                    verify_LocsAreEqual(workerOriginalLocRelativeToBuilding, workerNewLocRelativeToBuilding);
                verify_LocsAreEqual(workerOriginalMoveTarget, worker.AI.CurrentTask.LastMoveToTarget);
                break;

            case 2: // WorkerSubtask_WalkToItemSpot.
                verify_LocsAreEqual(workerOriginalLoc, worker.Location);
                if (movedBuildingItemWillBeStoredIn)
                    verify_LocsAreEqual(workerOriginalTargetRelativeToBuilding, workerNewMoveTargetRelativeToBuilding);
                break;

            case 3: // WorkerSubtask_DropItemInItemSpot.
                if (movedBuildingItemWillBeStoredIn)
                    verify_LocsAreEqual(workerOriginalLocRelativeToBuilding, workerNewLocRelativeToBuilding);
                verify_LocsAreEqual(workerOriginalMoveTarget, worker.AI.CurrentTask.LastMoveToTarget);
                break;
        }
    }

    void SetupMoveTest(int subtask, out BuildingData store1, out BuildingData store2)
    {
        LoadTestTown("storageRoom_move1", subtask);
        store1 = getBuildingByTestId("store1");
        store2 = getBuildingByTestId("store2");
    }
}