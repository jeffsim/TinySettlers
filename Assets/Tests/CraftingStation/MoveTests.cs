using NUnit.Framework;

public partial class CraftingStationTests : MovePauseDestroyTestBase
{
    [Test]
    public void CraftingStation_MoveTests()
    {
        // For this test we craft an item that requires two wood and generates 1 gardenplot.
        // only testing explicit goods here.

        // example scenario: 2 resources needed for crafting. subtasks:
        // --- transport resouce 1 to crafting spot
        // subtask=0: walk to resource in itemspot 1
        // subtask=1: pickup resource from itemspot 1
        // subtask=2: walk to crafting spot
        // subtask=3: drop resource in crafting spot
        // --- transport resouce 2 to crafting spot
        // subtask=4: walk to resource in itemspot 2
        // subtask=5: pickup resource from itemspot 2
        // subtask=6: walk to crafting spot
        // subtask=7: drop resource in crafting spot
        // --- ready to craft
        // subtask=8: craft item
        // subtask=9: walk to storage spot
        // subtask=10: drop item in storage spot
        for (int subtask = 0; subtask < 11; subtask++)
        {
            // Test A: Move craftingstation while worker1 is crafting item
            LoadTestTown("craftingstation_MovePauseDestroy", subtask);
            runCraftingTest("Test A", subtask);
        }
    }

    void runCraftingTest(string testName, int workerSubtask)
    {
        BuildingData buildingWithItem = CraftingStation;
        BuildingData buildingWorker = CraftingStation;
        var buildingToStoreItemIn = CraftingStation;
        var buildingToMove = CraftingStation;

        TestName = $"{testName}-{workerSubtask}: Move {buildingToMove.TestId} while {buildingWorker.TestId}'s worker is ";
        switch (workerSubtask)
        {
            case 0: TestName += $"walking to 1st storage spot to pick up 1st resource and bring to craftingspot"; break;
            case 1: TestName += $"picking up 1st resource"; break;
            case 2: TestName += $"Carrying 1st resource to craftingspot"; break;
            case 3: TestName += $"Dropping 1st resource in craftingspot"; break;
            case 4: TestName += $"walking to 2nd storage spot to pick up 2nd resource and bring to craftingspot"; break;
            case 5: TestName += $"picking up 2nd resource"; break;
            case 6: TestName += $"Carrying 2nd resource to craftingspot"; break;
            case 7: TestName += $"Dropping 2nd resource in craftingspot"; break;
            case 8: TestName += $"Crafting the item"; break;
            case 9: TestName += $"walking to storage spot to storage crafted item"; break;
            case 10: TestName += $"dropping crafted item in storage spot"; break;
        }
        TestName += "\n  ";
        // if (workerSubtask == 0) Debug.Log(TestName);

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = Town.CreateWorkerInBuilding(buildingWorker);
        forceMoveWorkerAwayFromAssignedBuilding(worker);

        waitUntilTaskAndSubtaskIndex(worker, TaskType.Task_CraftItem, workerSubtask);

        // Storage original state prior to making the change we're testing
        var originalSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
        var workerOriginalLoc = worker.Location;
        var workerOriginalTask = worker.AI.CurrentTask.Type;
        var workerOriginalSubtask = worker.AI.CurrentTask.CurSubTask.GetType();
        var workerOriginalLocRelativeToBuilding = worker.Location.WorldLoc - buildingToMove.Location.WorldLoc;
        var workerOriginalTargetRelativeToBuilding = worker.AI.CurrentTask.LastMoveToTarget.WorldLoc - buildingToMove.Location.WorldLoc;

        moveBuilding(buildingToMove, 1, 2);

        // Verify new state.  First verify storage spot in room remains reserved
        verify_spotReservedByWorker(originalSpotToStoreItemIn, worker);
        verify_WorkerTaskTypeAndSubtask(worker, workerOriginalTask, workerOriginalSubtask);

        var isWalking = workerSubtask == 0 || workerSubtask == 2 || workerSubtask == 4 || workerSubtask == 6 || workerSubtask == 9;
        if (isWalking)
        {
            var workerNewMoveTargetRelativeToBuilding = worker.AI.CurrentTask.LastMoveToTarget.WorldLoc - buildingToMove.Location.WorldLoc;
            verify_LocsAreEqual(workerOriginalTargetRelativeToBuilding, workerNewMoveTargetRelativeToBuilding);
            verify_LocsAreEqual(workerOriginalLoc, worker.Location);
        }
        else
        {
            var workerNewLocRelativeToBuilding = worker.Location.WorldLoc - buildingToMove.Location.WorldLoc;
            verify_LocsAreEqual(workerOriginalLocRelativeToBuilding, workerNewLocRelativeToBuilding);
        }
    }
}