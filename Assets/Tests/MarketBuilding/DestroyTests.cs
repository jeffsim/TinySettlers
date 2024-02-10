using System.Linq;
using NUnit.Framework;
using UnityEngine;

public partial class MarketTests : MovePauseDestroyTestBase
{
    [Test]
    public void Market_DestroyTests()
    {
        // subtask=0: seller is walking to spot in Market with item to sell
        // subtask=1: seller is picking up item to sell
        // subtask=2: seller is selling item in hand
        // subtask=3: seller is selling item in hand (and there are no available storage spots when paused) => drop on ground?
        for (int subtask = 0; subtask < 4; subtask++)
        {
            // Test A: Destroy market while worker1 is selling item
            LoadTestTown("market_MovePauseDestroy", subtask);
            runDestroyTest("Test A", subtask, Market, Market);
        }
    }

    void runDestroyTest(string testName, int workerSubtask, BuildingData buildingToDestroy, BuildingData buildingToStoreItemIn)
    {
        BuildingData buildingWithItem = Market;
        BuildingData buildingWorker = Market;

        TestName = $"{testName}-{workerSubtask}: Destroy {buildingToDestroy.TestId} while {buildingWorker.TestId}'s worker is ";
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
        var worker = createWorkerInBuilding(buildingWorker);

        var itemDefn = GameDefns.Instance.ItemDefns["wood"];
        var spotWithWood = buildingWithItem.GetClosestUnreservedStorageSpotWithItem(worker.Location, itemDefn);
        var itemToBePickedUp = spotWithWood.ItemContainer.Item;
        var originalSpotWithItem = getStorageSpotInBuildingWithItem(buildingWithItem, itemToBePickedUp);
        var destroyedBuildingWithItemInIt = buildingWithItem == buildingToDestroy;
        var destroyedBuildingItemWillBeStoredIn = buildingToStoreItemIn == buildingToDestroy;
        var destroyedBuildingOfWorker = buildingWorker == buildingToDestroy;
        var workerOriginalAssignedBuilding = worker.Assignment.AssignedTo;

        switch (workerSubtask)
        {
            case 0: waitUntilTaskAndSubtask(worker, TaskType.SellItem, typeof(Subtask_WalkToItemSpot)); break;
            case 1: waitUntilTaskAndSubtask(worker, TaskType.SellItem, typeof(Subtask_PickupItemFromItemSpot)); break;
            case 2: waitUntilTaskAndSubtask(worker, TaskType.SellItem, typeof(Subtask_SellItemInHands)); break;
            case 3: waitUntilTaskAndSubtask(worker, TaskType.SellItem, typeof(Subtask_SellItemInHands)); break;
        }

        if (workerSubtask == 3)
            fillAllTownStorageWithItem("plank");

        int origNumItemsInTownStorage = GetNumItemsInTownStorage() + GetNumItemsInTownGatheringSpots();
        int origNumItemsOnGround = Town.ItemsOnGround.Count;
        int origNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;
        int origNumReservedStorageSpots = Town.Buildings.Sum(b => b.StorageSpots.Count(s => s.Reservation.IsReserved));
        int origNumReservedGatheringSpots = Town.Buildings.Sum(b => b.GatheringSpots.Count(s => s.Reservation.IsReserved));

        Town.DestroyBuilding(buildingToDestroy);

        // If the worker is returning with the item in hand, then we need to wait one Town turn so that the worker can decide to carry the item they're holding to the Camp.
        if (workerSubtask >= 2)
            updateTown();

        // Verify new state.
        verify_AssignedBuilding(worker, Camp);
        if (workerSubtask == 0 || workerSubtask == 1)// WorkerSubtask_WalkToItemSpot and WorkerSubtask_PickupItemFromBuilding
        {
            verify_ItemDefnInHand(worker, null);
            verify_WorkerTaskType(TaskType.Idle, worker);
            verify_ItemsOnGround(1);
        }
        else if (workerSubtask == 2) // WorkerSubtask_SellItemInHands
        {
            verify_ItemInHand(worker, itemToBePickedUp);
            verify_ItemInSpot(originalSpotWithItem, null);
            verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");

            verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, "Should still be delivering the item that the worker is holding even though their building was destroyed");
        }
        else // STORAGE FULL: WorkerSubtask_SellItemInHands
        {
            int newNumItemsInTownStorage = GetNumItemsInTownStorage() + GetNumItemsInTownGatheringSpots();
            int newNumItemsOnGround = Town.ItemsOnGround.Count;
            int newNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;
            int newNumReservedStorageSpots = Town.Buildings.Sum(b => b.StorageSpots.Count(s => s.Reservation.IsReserved));
            int newNumReservedGatheringSpots = Town.Buildings.Sum(b => b.GatheringSpots.Count(s => s.Reservation.IsReserved));

            Assert.AreEqual(origNumItemsInTownStorage + origNumItemsOnGround + origNumItemsInWorkersHands, newNumItemsInTownStorage + newNumItemsOnGround + newNumItemsInWorkersHands, $"{preface("", 1)} Number of items in town (in storage+onground) should not have changed");

            verify_ItemInHand(worker, null);
            verify_WorkerTaskType(TaskType.Idle, worker);
        }
    }
}