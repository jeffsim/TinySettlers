using NUnit.Framework;

public partial class MarketTests : MovePauseDestroyTestBase
{
    [Test]
    public void Market_MoveTests()
    {
        //   subtask=0: seller is walking to spot in Market with item to sell
        //   subtask=1: seller is picking up item to sell
        //   subtask=2: seller is selling item in hand
        for (int subtask = 0; subtask < 3; subtask++)
        {
            // Test A: Move market while worker1 is selling item
            LoadTestTown("market_MovePauseDestroy", subtask);
            runMoveTest("Test A", subtask);
        }
    }

    void runMoveTest(string testName, int workerSubtask)
    {
        var buildingWithItem = Market;
        var buildingWorker = Market;
        var buildingToStoreItemIn = Market;
        var buildingToMove = Market;

        TestName = $"{testName}-{workerSubtask}: Move Market while worker is ";
        switch (workerSubtask)
        {
            case 0: TestName += $"walking to spot in Market with item to sell"; break;
            case 1: TestName += $"picking up item to sell"; break;
            case 2: TestName += $"selling item in hand"; break;
        }
        TestName += "\n  ";
        // if (workerSubtask == 0) Debug.Log(TestName);

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = Town.CreateWorkerInBuilding(buildingWorker);
        forceMoveWorkerAwayFromAssignedBuilding(worker);
        
        switch (workerSubtask)
        {
            case 0: waitUntilTaskAndSubtask(worker, TaskType.SellItem, typeof(WorkerSubtask_WalkToItemSpot)); break;
            case 1: waitUntilTaskAndSubtask(worker, TaskType.SellItem, typeof(WorkerSubtask_PickupItemFromBuilding)); break;
            case 2: waitUntilTaskAndSubtask(worker, TaskType.SellItem, typeof(WorkerSubtask_SellItemInHands)); break;
        }

        var movedBuildingWithItemInIt = true;
        var movedBuildingItemWillBeStoredIn = true;

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