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
        // 0    walk to resource in itemspot 1
        // 1    pickup resource from itemspot 1
        // 2    unreserve resource spot 1 IFF it's not the spot we're dropping the crafted item into; otherwise do a noop
        // 3    walk to crafting spot
        // 4    drop resource in crafting spot
        // --- transport resouce 2 to crafting spot
        // 5    walk to resource in itemspot 2
        // 6    pickup resource from itemspot 2
        // 7    unreserve resource spot 1 IFF it's not the spot we're dropping the crafted item into; otherwise do a noop
        // 8    walk to crafting spot
        // 9    drop resource in crafting spot
        // --- ready to craft
        // 10   craft item
        // 11   walk to storage spot
        // 12   drop item in storage spot
        for (int subtask = 0; subtask < 13; subtask++)
        {
            if (subtask == 2 || subtask == 7) continue;

            // Test A: Move craftingstation while worker1 is crafting item
            LoadTestTown("craftingstation_MovePauseDestroy", subtask);
            runMoveTest("Test A", subtask);
        }
    }

    void runMoveTest(string testName, int workerSubtask)
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
            case 2: TestName += $"Unreserving 1st resource storagespot; shouldn't hit this"; break;
            case 3: TestName += $"Carrying 1st resource to craftingspot"; break;
            case 4: TestName += $"Dropping 1st resource in craftingspot"; break;
            case 5: TestName += $"walking to 2nd storage spot to pick up 2nd resource and bring to craftingspot"; break;
            case 6: TestName += $"picking up 2nd resource"; break;
            case 7: TestName += $"Unreserving 2nd resource storagespot; shouldn't hit this"; break;
            case 8: TestName += $"Carrying 2nd resource to craftingspot"; break;
            case 9: TestName += $"Dropping 2nd resource in craftingspot"; break;
            case 10: TestName += $"Crafting the item"; break;
            case 11: TestName += $"walking to storage spot to storage crafted item"; break;
            case 12: TestName += $"dropping crafted item in storage spot"; break;
        }
        TestName += "\n  ";
        // if (workerSubtask == 0) Debug.Log(TestName);

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = createWorkerInBuilding(buildingWorker);
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

        var isWalking = workerSubtask == 0 || workerSubtask == 3 || workerSubtask == 5 || workerSubtask == 8 || workerSubtask == 11;
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