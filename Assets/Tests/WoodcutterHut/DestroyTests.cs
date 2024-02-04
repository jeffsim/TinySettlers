// using NUnit.Framework;

// public partial class StorageRoomTests : TestBase
// {
//     [Test]
//     public void StorageRoom_DestroyTests()
//     {
//         //   subtask=0: Destroy [buildingToDestroy] while [workerToTest] is walking to [buildingWithItem] to pick something up to store in [buildingToStoreItemIn]
//         //   subtask=1: Destroy [buildingToDestroy] while [workerToTest] is picking up item in [buildingWithItem] to store in [buildingToStoreItemIn]
//         //   subtask=2: Destroy [buildingToDestroy] while [workerToTest] is walking to [buildingToStoreItemIn]
//         //   subtask=3: Destroy [buildingToDestroy] while [workerToTest] is dropping item in [buildingToStoreItemIn]
//         //   subtask=4: Destroy [buildingToDestroy] while [workerToTest] is walking to [buildingToStoreItemIn] and there are no available storage spots
//         //   subtask=5: Destroy [buildingToDestroy] while [workerToTest] is dropping item in [buildingToStoreItemIn] and there are no available storage spots
//         for (int subtask = 0; subtask < 6; subtask++)
//         {
//             // Test A: Destroy store1 while worker1 is getting an item from woodcutter to store in store1
//             // Test B: Destroy store1 while worker2 is getting an item from woodcutter to store in store1
//             // Test C: Destroy store2 while worker2 is getting an item from woodcutter to store in store1
//             // Test D: Destroy woodcu while worker1 is getting an item from woodcutter to store in store1
//             // Test E: Destroy woodcu while worker2 is getting an item from woodcutter to store in store1
//             BuildingData store1, store2;
//             SetupDestroyTest(subtask, out store1, out store2); runDestroyTest("Test A", subtask, store1, store1, WoodcuttersHut, store1);
//             SetupDestroyTest(subtask, out store1, out store2); runDestroyTest("Test B", subtask, store1, store2, WoodcuttersHut, store1);
//             SetupDestroyTest(subtask, out store1, out store2); runDestroyTest("Test C", subtask, store2, store2, WoodcuttersHut, store1);
//             SetupDestroyTest(subtask, out store1, out store2); runDestroyTest("Test D", subtask, WoodcuttersHut, store1, WoodcuttersHut, store1);
//             SetupDestroyTest(subtask, out store1, out store2); runDestroyTest("Test E", subtask, WoodcuttersHut, store2, WoodcuttersHut, store1);
//         }
//     }

//     void runDestroyTest(string testName, int workerSubtask, BuildingData buildingToDestroy, BuildingData buildingWorker, BuildingData buildingWithItem, BuildingData buildingToStoreItemIn)
//     {
//         TestName = $"{testName}: Destroy {buildingToDestroy.TestId} while {buildingWorker.TestId}'s worker is ";
//         switch (workerSubtask)
//         {
//             case 0: TestName += $"walking to {buildingWithItem.TestId} to pickup item and bring to {buildingToStoreItemIn.TestId}"; break;
//             case 1: TestName += $"picking up item in {buildingWithItem.TestId} to bring to {buildingToStoreItemIn.TestId}"; break;
//             case 2: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId}"; break;
//             case 3: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId}"; break;
//             case 4: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId} and there are no available storage spots"; break;
//             case 5: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId} and there are no available storage spots"; break;
//         }
//         TestName += "\n  ";
//         // if (workerSubtask == 0) Debug.Log(TestName);

//         // Create the worker and wait until they get to the to-be-tested subtask
//         var worker = Town.CreateWorkerInBuilding(buildingWorker);
//         var itemToBePickedUp = buildingWithItem.GetUnreservedItemInStorage(GameDefns.Instance.ItemDefns["plank"]);
//         var originalSpotWithItem = getStorageSpotInBuildingWithItem(buildingWithItem, itemToBePickedUp);
//         var destroyedBuildingWithItemInIt = buildingWithItem == buildingToDestroy;
//         var destroyedBuildingItemWillBeStoredIn = buildingToStoreItemIn == buildingToDestroy;
//         var destroyedBuildingOfWorker = buildingWorker == buildingToDestroy;
//         var workerOriginalAssignedBuilding = worker.Assignment.AssignedTo;

//         switch (workerSubtask)
//         {
//             case 0: waitUntilTaskAndSubtask(worker, TaskType.PickupItemInStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
//             case 1: waitUntilTaskAndSubtask(worker, TaskType.PickupItemInStorageSpot, typeof(WorkerSubtask_PickupItemFromBuilding)); break;
//             case 2: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
//             case 3: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_DropItemInItemSpot)); break;
//             case 4: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
//             case 5: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_DropItemInItemSpot)); break;
//         }
//         var originalSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
//         Assert.IsNotNull(originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have reserved a spot in {buildingToStoreItemIn.TestId} to store the item in");

//         if (workerSubtask > 3)
//             fillAllTownStorageWithItem("plank");
//         int origNumItemsInTownStorage = GetNumItemsInTownStorage();
//         int origNumItemsOnGround = Town.ItemsOnGround.Count;
//         int origNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;

//         Town.DestroyBuilding(buildingToDestroy);

//         // If the worker is returning with the item in hand, then we need to wait one Town turn so that the worker can decide to carry the item they're holding to the Camp.
//         if (workerSubtask >= 2)
//             updateTown();

//         // Verify new state.
//         if (workerSubtask == 0 || workerSubtask == 1)// WorkerSubtask_WalkToItemSpot and WorkerSubtask_PickupItemFromBuilding
//         {
//             verify_ItemDefnInHand(worker, null);
//             if (destroyedBuildingWithItemInIt)
//             {
//                 verify_WorkerTaskType(TaskType.Idle, worker);
//                 verify_ItemsOnGround(1);
//             }
//             else
//             {
//                 verify_ItemInStorageSpot(originalSpotWithItem, itemToBePickedUp);
//                 verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
//                 if (destroyedBuildingOfWorker)
//                 {
//                     verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
//                     verify_WorkerTaskType(TaskType.Idle, worker);
//                 }
//                 else if (destroyedBuildingItemWillBeStoredIn)
//                 {
//                     verify_spotIsReserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
//                     verify_WorkerTaskType(TaskType.PickupItemInStorageSpot, worker);
//                     Assert.AreNotEqual(((WorkerTask_PickupItemFromStorageSpot)worker.AI.CurrentTask).ReservedSpotToStoreItemIn.Building, buildingToDestroy, $"{preface()} Worker should have reserved a spot in another building to store the item in");
//                 }
//             }
//         }
//         else if (workerSubtask == 2 || workerSubtask == 3) // WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot
//         {
//             verify_ItemInHand(worker, itemToBePickedUp);
//             verify_ItemInStorageSpot(originalSpotWithItem, null);
//             verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");

//             if (destroyedBuildingOfWorker)
//             {
//                 verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, "Should still be delivering the item that the worker is holding even though their building was destroyed");
//                 if (destroyedBuildingItemWillBeStoredIn)
//                 {
//                     verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
//                     Assert.AreNotEqual(worker.StorageSpotReservedForItemInHand.Building, originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have reserved a spot in a different building to store the item in");
//                 }
//                 else
//                 {
//                     verify_spotIsReserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should still be reserved");
//                     Assert.AreEqual(worker.StorageSpotReservedForItemInHand.Building, originalSpotToStoreItemIn.Building, $"{preface("", 1)} Worker should still have reserved the same spot to store the item in");
//                 }
//             }
//             if (destroyedBuildingWithItemInIt)
//             {
//                 verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker);
//                 verify_spotIsReserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be reserved");
//             }
//             if (destroyedBuildingItemWillBeStoredIn)
//             {
//                 verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker);
//                 verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
//                 Assert.AreNotEqual(worker.StorageSpotReservedForItemInHand.Building, originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have reserved a spot in a different building to store the item in");
//             }
//         }
//         else // STORAGE FULL: WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot 
//         {
//             int newNumItemsInTownStorage = GetNumItemsInTownStorage();
//             int newNumItemsOnGround = Town.ItemsOnGround.Count;
//             int newNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;

//             verify_AssignedBuilding(worker, destroyedBuildingOfWorker ? Camp : workerOriginalAssignedBuilding);
//             Assert.AreEqual(origNumItemsInTownStorage + origNumItemsOnGround + origNumItemsInWorkersHands, newNumItemsInTownStorage + newNumItemsOnGround + newNumItemsInWorkersHands, $"{preface("", 1)} Number of items in town (in storage+onground) should not have changed");

//             // worker had a reserved spot in store1;
//             //  if store1 was destroyed then their reservation should be removed and they should be assigned to camp and should have no item.  we were able to fill it above.
//             //  if store2 was destroyed then their reservation should be valid and they should be assigned to origBuilding and should still be carrying to store1.  we couldn't fill it above.
//             //  if woodcutter was destroyed then their reservation should be valid and they should be assigned to origBuilding and should still be carrying to store1.  we couldn't fill it above.
//             if (buildingToDestroy.TestId == "store1")
//             {
//                 verify_ItemInHand(worker, null);
//                 verify_WorkerTaskType(TaskType.Idle, worker);
//                 verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
//             }
//             else
//             {
//                 verify_ItemInHand(worker, itemToBePickedUp);
//                 verify_spotStillReservedByWorker(originalSpotToStoreItemIn, originalSpotToStoreItemIn.Building, worker);
//                 verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, "Should still be delivering the item that the worker is holding");
//             }
//         }
//     }

//     void SetupDestroyTest(int subtask, out BuildingData store1, out BuildingData store2)
//     {
//         LoadTestTown("storageRoom_MovePauseDestroy", subtask);
//         store1 = getBuildingByTestId("store1");
//         store2 = getBuildingByTestId("store2");
//     }
// }