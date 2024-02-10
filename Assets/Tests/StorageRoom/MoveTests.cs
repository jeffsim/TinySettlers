using NUnit.Framework;

public partial class StorageRoomTests : MovePauseDestroyTestBase
{
    [Test]
    public void StorageRoom_MoveTests()
    {
        // subtask=0: Move [buildingToMove] while [workerToTest] is walking to [buildingWithItem] to pick something up to store in [buildingToStoreItemIn]
        // subtask=1: Move [buildingToMove] while [workerToTest] is picking up item in [buildingWithItem] to store in [buildingToStoreItemIn]
        // subtask=2: worker is unreserving spot item was in (shouldn't hit this since should complete instantly)
        // subtask=3: Move [buildingToMove] while [workerToTest] is walking to [buildingToStoreItemIn]
        // subtask=4: Move [buildingToMove] while [workerToTest] is dropping item in [buildingToStoreItemIn]
        for (int subtask = 0; subtask < 5; subtask++)
        {
            if (subtask == 2) continue;
            // Test A: Move store1     while worker1 is getting an item from woodcutter to store in store1
            // Test B: Move store1     while worker2 is getting an item from woodcutter to store in store1
            // Test C: Move store2     while worker2 is getting an item from woodcutter to store in store1
            // Test D: Move woodcutter while worker1 is getting an item from woodcutter to store in store1
            // Test E: Move woodcutter while worker1 is getting an item from woodcutter to store in store2
            BuildingData store1, store2;
            PrepMPDTest("storageRoom_MovePauseDestroy", subtask);
            SetupMPDTest(out store1, out store2); runMoveTest("Test A", subtask, store1, store1, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runMoveTest("Test B", subtask, store1, store2, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runMoveTest("Test C", subtask, store2, store2, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runMoveTest("Test D", subtask, WoodcuttersHut, store1, WoodcuttersHut, store1);
       //     SetupMPDTest(out store1, out store2); runMoveTest("Test E", subtask, WoodcuttersHut, store1, WoodcuttersHut, store2);
        }
    }

    void runMoveTest(string testName, int workerSubtask, BuildingData buildingToMove, BuildingData buildingWorker, BuildingData buildingWithItem, BuildingData buildingToStoreItemIn)
    {
        TestName = $"{testName}-{workerSubtask}: Move {buildingToMove.TestId} while {buildingWorker.TestId}'s worker is ";
        switch (workerSubtask)
        {
            case 0: TestName += $"walking to {buildingWithItem.TestId} to pickup item and bring to {buildingToStoreItemIn.TestId}"; break;
            case 1: TestName += $"picking up item in {buildingWithItem.TestId} to bring to {buildingToStoreItemIn.TestId}"; break;
            case 2: TestName += $"Unreserving storage spot. SHOULDN'T HIT THIS"; break;
            case 3: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId}"; break;
            case 4: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId}"; break;
        }
        TestName += "\n  ";

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = Town.CreateWorkerInBuilding(buildingWorker);

        waitUntilTaskAndSubtaskIndex(worker, TaskType.TransportItemFromSpotToSpot, workerSubtask);

        var movedBuildingWithItemInIt = buildingWithItem == buildingToMove;
        var movedBuildingItemWillBeStoredIn = buildingToStoreItemIn == buildingToMove;
        var originalTask = worker.AI.CurrentTask as Task_TransportItemFromSpotToSpot;
        var originalSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
        Assert.NotNull(originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have a reserved spot in {buildingToStoreItemIn.TestId} to store item in");
        var workerOriginalLoc = worker.Location;
        var workerOriginalMoveTarget = worker.AI.CurrentTask.LastMoveToTarget;
        var workerOriginalTask = worker.AI.CurrentTask.Type;
        var workerOriginalSubtask = worker.AI.CurrentTask.CurSubTask.GetType();

        var workerOriginalLocRelativeToBuilding = worker.Location.WorldLoc - buildingToMove.Location.WorldLoc;
        var workerOriginalTargetRelativeToBuilding = worker.AI.CurrentTask.LastMoveToTarget.WorldLoc - buildingToMove.Location.WorldLoc;

        moveBuilding(buildingToMove, 2, 0);

        var newTask = worker.AI.CurrentTask as Task_TransportItemFromSpotToSpot;
        Assert.AreEqual(originalTask, newTask, $"{preface("", 1)} Task shouldn't have changed");

        verify_spotReservedByWorker(originalSpotToStoreItemIn, worker);
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

            case 3: // WorkerSubtask_WalkToItemSpot.
                verify_LocsAreEqual(workerOriginalLoc, worker.Location);
                if (movedBuildingItemWillBeStoredIn)
                    verify_LocsAreEqual(workerOriginalTargetRelativeToBuilding, workerNewMoveTargetRelativeToBuilding);
                break;

            case 4: // WorkerSubtask_DropItemInItemSpot.
                if (movedBuildingItemWillBeStoredIn)
                    verify_LocsAreEqual(workerOriginalLocRelativeToBuilding, workerNewLocRelativeToBuilding);
                verify_LocsAreEqual(workerOriginalMoveTarget, worker.AI.CurrentTask.LastMoveToTarget);
                break;
        }
    }
}