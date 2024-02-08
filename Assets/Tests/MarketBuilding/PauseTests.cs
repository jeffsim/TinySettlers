using NUnit.Framework;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine.UIElements;

public partial class MarketTests : MovePauseDestroyTestBase
{
    [Test]
    public void Market_PauseTests()
    {
        // subtask=0: seller is walking to spot in Market with item to sell
        // subtask=1: seller is picking up item to sell
        // subtask=2: seller is selling item in hand
        // subtask=3: seller is selling item in hand (and there are no available storage spots when paused) => drop on ground?
        for (int subtask = 0; subtask < 4; subtask++)
        {
            // Test A: Pause market while worker1 is selling item
            LoadTestTown("market_MovePauseDestroy", subtask);
            runPauseTest("Test A", subtask, Market, Market);
        }
    }

    void runPauseTest(string testName, int workerSubtask, BuildingData buildingToPause, BuildingData buildingToStoreItemIn)
    {
        BuildingData buildingWithItem = Market;
        BuildingData buildingWorker = Market;

        TestName = $"{testName}-{workerSubtask}: Pause {buildingToPause.TestId} while {buildingWorker.TestId}'s worker is ";
        switch (workerSubtask)
        {
            case 0: TestName += $"walking to spot in Market with item to sell"; break;
            case 1: TestName += $"picking up item to sell"; break;
            case 2: TestName += $"selling item in hand"; break;
            case 3: TestName += $"selling item in hand (and there are no spots to store it)"; break;
        }
        TestName += "\n  ";
        // if (workerSubtask == 0) Debug.Log(TestName);

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = Town.CreateWorkerInBuilding(buildingWorker);
        forceMoveWorkerAwayFromAssignedBuilding(worker);

        // Create the worker and wait until they get to the to-be-tested subtask
        var itemDefn = GameDefns.Instance.ItemDefns["wood"];
        var spotWithWood = buildingWithItem.GetClosestUnreservedStorageSpotWithItem(worker.Location, itemDefn);
        var itemToBePickedUp = spotWithWood.ItemContainer.Item;
        var originalSpotWithItem = getStorageSpotInBuildingWithItem(buildingWithItem, itemToBePickedUp);
        var pausedBuildingWithItemInIt = buildingWithItem == buildingToPause;
        var pausedBuildingItemWillBeStoredIn = buildingToStoreItemIn == buildingToPause;
        var pausedBuildingOfWorker = buildingWorker == buildingToPause;
        var workerOriginalAssignedBuilding = worker.Assignment.AssignedTo;

        switch (workerSubtask)
        {
            case 0: waitUntilTaskAndSubtask(worker, TaskType.SellItem, typeof(WorkerSubtask_WalkToItemSpot)); break;
            case 1: waitUntilTaskAndSubtask(worker, TaskType.SellItem, typeof(WorkerSubtask_PickupItemFromBuilding)); break;
            case 2: waitUntilTaskAndSubtask(worker, TaskType.SellItem, typeof(WorkerSubtask_SellItemInHands)); break;
            case 3: waitUntilTaskAndSubtask(worker, TaskType.SellItem, typeof(WorkerSubtask_SellItemInHands)); break;
        }

        var originalSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
        Assert.IsNotNull(originalSpotToStoreItemIn, $"{preface()} Worker should have reserved a spot in {buildingToStoreItemIn.TestId} to store the item in");

        if (workerSubtask == 3)
            fillAllTownStorageWithItem("plank");
        int origNumItemsInTownStorage = GetNumItemsInTownStorage();
        int origNumItemsOnGround = Town.ItemsOnGround.Count;
        int origNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;

        buildingToPause.TogglePaused();

        // If the worker is holding the item to sell it, then we need to wait one Town turn so that the worker can decide to carry the item they're holding to the Camp.
        if (workerSubtask >= 2)
            updateTown();

        // Verify new state.
        if (workerSubtask == 0 || workerSubtask == 1)// WorkerSubtask_WalkToItemSpot and WorkerSubtask_PickupItemFromBuilding
        {
            verify_ItemDefnInHand(worker, null);
            verify_ItemInSpot(originalSpotWithItem, itemToBePickedUp);
            verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
            verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
            verify_WorkerTaskType(TaskType.Idle, worker);
        }
        else if (workerSubtask == 2) // WorkerSubtask_SellItemInHands
        {
            verify_ItemInHand(worker, itemToBePickedUp);
            verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");

            verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker);
            verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
            Assert.AreNotEqual(worker.StorageSpotReservedForItemInHand.Building, originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have reserved a spot in a different building to store the item in");
        }
        else // STORAGE FULL: WorkerSubtask_SellItemInHands
        {
            int newNumItemsInTownStorage = GetNumItemsInTownStorage();
            int newNumItemsOnGround = Town.ItemsOnGround.Count;
            int newNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;

            verify_AssignedBuilding(worker, workerOriginalAssignedBuilding);
            Assert.AreEqual(origNumItemsInTownStorage + origNumItemsOnGround + origNumItemsInWorkersHands, newNumItemsInTownStorage + newNumItemsOnGround + newNumItemsInWorkersHands, $"{preface("", 1)} Number of items in town (in storage+onground) should not have changed");

            verify_ItemInHand(worker, null);
            verify_WorkerTaskType(TaskType.Idle, worker);
            verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
        }
    }
}