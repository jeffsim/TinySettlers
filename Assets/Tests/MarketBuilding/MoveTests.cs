using NUnit.Framework;

public partial class MarketTests : MovePauseDestroyTestBase
{
    [Test]
    public void Market_MoveTests()
    {
        //   subtask=0: woodcutter is walking to forest to gather wood to store in [buildingToStoreItemIn]
        //   subtask=1: woodcutter is gathering wood in forest to store in [buildingToStoreItemIn]
        //   subtask=2: woodcutter is picking up wood in forest to store in [buildingToStoreItemIn]
        //   subtask=3: woodcutter is walking to [buildingToStoreItemIn]
        //   subtask=4: woodcutter is dropping wood in [buildingToStoreItemIn]
        for (int subtask = 0; subtask < 5; subtask++)
        {
            // Test A: Move woodcutter while worker1 is getting wood from forest to store in store1
            // Test B: Move forest     while worker1 is getting wood from forest to store in store1
            // Test C: Move store1     while worker1 is getting wood from forest to store in store1
            // Test D: Move store2     while worker1 is getting wood from forest to store in store1 (so that store2 is closer; switch to that)
            BuildingData store1, store2;
            PrepMPDTest("woodcutter_MovePauseDestroy", subtask);
            SetupMPDTest(out store1, out store2); runMoveTest("Test A", subtask, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runMoveTest("Test B", subtask, Forest, store1);
            SetupMPDTest(out store1, out store2); runMoveTest("Test C", subtask, store1, store1);
            SetupMPDTest(out store1, out store2); runMoveTest("Test D", subtask, store2, store1);

            // Following tests disable store1 and store2 before running so that woodcutter can only store in woodcutter
            // Test E: Move woodcutter while worker1 is getting wood from forest to store in woodcutter
            // Test F: Move forest     while worker1 is getting wood from forest to store in woodcutter
            SetupMPDTest(out store1, out store2, true); runMoveTest("Test E", subtask, WoodcuttersHut, WoodcuttersHut);
            SetupMPDTest(out store1, out store2, true); runMoveTest("Test F", subtask, Forest, WoodcuttersHut);
        }
    }

    void runMoveTest(string testName, int workerSubtask, BuildingData buildingToMove, BuildingData buildingToStoreItemIn)
    {
        BuildingData buildingWithItem = Forest;
        BuildingData buildingWorker = WoodcuttersHut;

        TestName = $"{testName}-{workerSubtask}: Move {buildingToMove.TestId} while {buildingWorker.TestId}'s worker is ";
        switch (workerSubtask)
        {
            case 0: TestName += $"walking to {buildingWithItem.TestId} to gather wood and bring to {buildingToStoreItemIn.TestId}"; break;
            case 1: TestName += $"gathering wood in {buildingWithItem.TestId} to bring to {buildingToStoreItemIn.TestId}"; break;
            case 2: TestName += $"picking up wood in {buildingWithItem.TestId} to bring to {buildingToStoreItemIn.TestId}"; break;
            case 3: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId}"; break;
            case 4: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId}"; break;
        }
        TestName += "\n  ";
        // if (workerSubtask == 0) Debug.Log(TestName);

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = Town.CreateWorkerInBuilding(buildingWorker);
        switch (workerSubtask)
        {
            case 0: waitUntilTaskAndSubtask(worker, TaskType.PickupGatherableResource, typeof(WorkerSubtask_WalkToItemSpot)); break;
            case 1: waitUntilTaskAndSubtask(worker, TaskType.PickupGatherableResource, typeof(WorkerSubtask_ReapGatherableResource)); break;
            case 2: waitUntilTaskAndSubtask(worker, TaskType.PickupGatherableResource, typeof(WorkerSubtask_PickupItemFromBuilding)); break;
            case 3: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
            case 4: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_DropItemInItemSpot)); break;
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

        moveBuilding(buildingToMove, 1, 1);

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

            case 1: // WorkerSubtask_ReapGatherableResource.
                if (movedBuildingWithItemInIt)
                    verify_LocsAreEqual(workerOriginalLocRelativeToBuilding, workerNewLocRelativeToBuilding);
                verify_LocsAreEqual(workerOriginalMoveTarget, worker.AI.CurrentTask.LastMoveToTarget);
                break;

            case 2: // WorkerSubtask_PickupItemFromBuilding.
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