using System;
using NUnit.Framework;

public partial class WoodcutterHutTests : MovePauseDestroyTestBase
{
    [Test]
    public void WoodcutterHut_MoveTests()
    {
        //   subtask=0: woodcutter is walking to forest to gather wood to store in [buildingToStoreItemIn]
        //   subtask=1: woodcutter is gathering wood in forest to store in [buildingToStoreItemIn]
        //   subtask=2: woodcutter is picking up wood in forest to store in [buildingToStoreItemIn]
        //   subtask=3: woodcutter is unreserving spot in forest (shouldn't hit this since should complete instantly)
        //   subtask=4: woodcutter is walking to [buildingToStoreItemIn]
        //   subtask=5: woodcutter is dropping wood in [buildingToStoreItemIn]
        for (int subtask = 0; subtask < 6; subtask++)
        {
            if (subtask == 3) continue;
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
            case 3: TestName += $"Unreserving gathering spot. SHOULDN'T HIT THIS"; break;
            case 4: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId}"; break;
            case 5: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId}"; break;
        }
        TestName += "\n  ";
        // if (workerSubtask == 0) Debug.Log(TestName);

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = Town.CreateWorkerInBuilding(buildingWorker);

        waitUntilTaskAndSubtaskIndex(worker, TaskType.GatherResource, workerSubtask);

        var movedBuildingWithItemInIt = buildingWithItem == buildingToMove;
        var movedBuildingItemWillBeStoredIn = buildingToStoreItemIn == buildingToMove;
        var originalTask = worker.AI.CurrentTask as Task_GatherResource;
        var originalSpotToGatherFrom = originalTask.SpotToGatherFrom;
        var originalSpotToStoreItemIn = originalTask.SpotToStoreItemIn;
        var workerOriginalLoc = worker.Location;
        var workerOriginalMoveTarget = worker.AI.CurrentTask.LastMoveToTarget;
        var workerOriginalTask = worker.AI.CurrentTask.Type;
        var workerOriginalSubtask = worker.AI.CurrentTask.CurSubTask.GetType();
        var workerOriginalLocRelativeToSpotToGatherFrom = worker.Location.WorldLoc - originalSpotToGatherFrom.Location.WorldLoc;
        var workerOriginalTargetRelativeToSpotToGatherFrom = worker.AI.CurrentTask.LastMoveToTarget.WorldLoc - originalSpotToGatherFrom.Location.WorldLoc;
        var workerOriginalLocRelativeToSpotToStoreIn = worker.Location.WorldLoc - originalSpotToStoreItemIn.Location.WorldLoc;
        var workerOriginalTargetRelativeToSpotToStoreIn = worker.AI.CurrentTask.LastMoveToTarget.WorldLoc - originalSpotToStoreItemIn.Location.WorldLoc;

        moveBuilding(buildingToMove, 1, 1);

        var newTask = worker.AI.CurrentTask as Task_GatherResource;
        Assert.AreEqual(originalTask, newTask, $"{preface("", 1)} Task shouldn't have changed");

        var newSpotToGatherFrom = newTask.SpotToGatherFrom;
        var newSpotToStoreItemIn = newTask.SpotToStoreItemIn;
        var workerNewLocRelativeToSpotToGatherFrom = worker.Location.WorldLoc - newSpotToGatherFrom.Location.WorldLoc;
        var workerNewTargetRelativeToSpotToGatherFrom = worker.AI.CurrentTask.LastMoveToTarget.WorldLoc - newSpotToGatherFrom.Location.WorldLoc;
        var workerNewLocRelativeToSpotToStoreIn = worker.Location.WorldLoc - newSpotToStoreItemIn.Location.WorldLoc;
        var workerNewMoveTargetRelativeToSpotToStoreIn = worker.AI.CurrentTask.LastMoveToTarget.WorldLoc - newSpotToStoreItemIn.Location.WorldLoc;

        verify_spotReservedByWorker(newSpotToStoreItemIn, worker);
        if (originalSpotToStoreItemIn != newSpotToStoreItemIn)
            verify_spotIsUnreserved(originalSpotToStoreItemIn);
        verify_WorkerTaskTypeAndSubtask(worker, workerOriginalTask, workerOriginalSubtask);

        switch (workerSubtask)
        {
            case 0: // WorkerSubtask_WalkToItemSpot.
                verify_LocsAreEqual(workerOriginalLoc, worker.Location);
                if (movedBuildingWithItemInIt)
                    verify_LocsAreEqual(workerOriginalTargetRelativeToSpotToGatherFrom, workerNewTargetRelativeToSpotToGatherFrom);
                break;

            case 1: // WorkerSubtask_ReapGatherableResource.
                if (movedBuildingWithItemInIt)
                    verify_LocsAreEqual(workerOriginalLocRelativeToSpotToGatherFrom, workerNewLocRelativeToSpotToGatherFrom);
                verify_LocsAreEqual(workerOriginalMoveTarget, worker.AI.CurrentTask.LastMoveToTarget);
                break;

            case 2: // WorkerSubtask_PickupItemFromBuilding.
                if (movedBuildingWithItemInIt)
                    verify_LocsAreEqual(workerOriginalLocRelativeToSpotToGatherFrom, workerNewLocRelativeToSpotToGatherFrom);
                verify_LocsAreEqual(workerOriginalMoveTarget, worker.AI.CurrentTask.LastMoveToTarget);
                break;

            case 3:
                Assert.Fail("Shouldn't hit this subtask");
                break;

            case 4: // WorkerSubtask_WalkToItemSpot.
                verify_LocsAreEqual(workerOriginalLoc, worker.Location);
                if (movedBuildingItemWillBeStoredIn)
                    verify_LocsAreEqual(workerOriginalTargetRelativeToSpotToStoreIn, workerNewMoveTargetRelativeToSpotToStoreIn);
                break;

            case 5: // WorkerSubtask_DropItemInItemSpot.
                if (movedBuildingItemWillBeStoredIn)
                    verify_LocsAreEqual(workerOriginalLocRelativeToSpotToStoreIn, workerNewLocRelativeToSpotToStoreIn);
                verify_LocsAreEqual(workerOriginalMoveTarget, worker.AI.CurrentTask.LastMoveToTarget);
                break;
        }
    }

    private void waitUntilTaskAndSubtaskIndex(WorkerData worker, Type type, int workerSubtask)
    {
        throw new NotImplementedException();
    }
}