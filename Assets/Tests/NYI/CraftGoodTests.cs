// using NUnit.Framework;

// public class CraftGoodTests : TestBase
// {
//     [Test]
//     public void BasicCraftingWorks()
//     {
//         LoadTestTown("testTown1");

//         // Verify starting state
//         var miner = getAssignedWorker(MinersHut.DefnId);
//         Assert.AreEqual(miner.CurrentTask.Type, TaskType.Idle);

//         // Start gathering task
//         waitUntilTask(miner, TaskType.GatherResource);
//         Assert.AreEqual(miner.CurrentTask.Type, TaskType.GatherResource);

//         // Verify Worker reserved a gathering spot in the mine
//         Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
//         Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, miner);

//         // wait until actually gathering resource in target building
//         waitUntilTaskSubstate(miner, (int)WorkerTask_GatherResourceSubstate.GatherResourceInBuilding);
//         Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
//         Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, miner);

//         // wait until done gathering; gathering spot should no longer be reserve
//         waitUntilTaskSubstate(miner, (int)WorkerTask_GatherResourceSubstate.ReturnToAssignedBuilding);
//         Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 0);

//         // wait until dropping; should still be unreserved
//         waitUntilTaskSubstate(miner, (int)WorkerTask_GatherResourceSubstate.DropGatheredResource);
//         Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 0);

//         // wait until restarted task; should be reserved again
//         waitUntilNewTask(miner, TaskType.GatherResource);
//         Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
//         Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, miner);
//     }
// }
