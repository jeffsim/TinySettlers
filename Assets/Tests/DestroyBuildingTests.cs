using System.Collections.Generic;
using NUnit.Framework;

public class DestroyBuildingTests : TestBase
{
    [Test]
    public void DestroyBuilding_StoredResourcesRemainBehindOnGround()
    {
        LoadTestTown("testTown1");

        // Add items to storage
        var item = new ItemData() { DefnId = "wood" };
        MinersHut.AddItemToStorage(item);

        // Destroy the building, verify new state
        Town.DestroyBuilding(MinersHut);

        // Verify Map contains item
        Assert.IsTrue(Town.ItemsOnGround.Contains(item));
    }

    [Test]
    public void DestroyBuilding_ItemsCarriedToDestroyedBuildingAreCarriedToStorageInstead()
    {
        // TODO

        // i = 0; carrying item to drop in building that is destroyed; storage elsewhere exists - carry to that instead
        // i = 1; carrying item to drop in building that is destroyed; no available storage - destroy item for simplicity (could also drop it)
        for (int i = 0; i < 2; i++)
        {
            LoadTestTown("testTown1");

            // TODO
        }
    }

    [Test]
    public void DestroyBuilding_ItemsLeftOnGroundAreSubsequentlyPickedUp()
    {
        // TODO

        for (int i = 0; i < 2; i++)
        {
            LoadTestTown("testTown1");

            // TODO
        }
    }

    [Test]
    public void DestroyBuilding_WalkingToGatherResources()
    {
        // i = 0 ==> destroy building with resources being gathered
        // i = 1 ==> destroy building which the miner is assigned to
        for (int i = 0; i < 2; i++)
        {
            LoadTestTown("testTown1");

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.GatherResource);
            Assert.AreEqual(miner.CurrentTask.Type, TaskType.GatherResource);

            // wait until walking to target mine
            waitUntilTaskSubstate(miner, (int)WorkerTask_GatherResourceSubstate.GotoResourceBuilding);
            Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
            Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, miner);

            // Destroy the building, verify new state
            if (i == 0)
            {
                Town.DestroyBuilding(StoneMine);

                // Verify building is removed from Map
                Assert.IsTrue(!Town.Buildings.Contains(StoneMine));

                // Verify miner's task is now to return to assigned building
                Assert.AreEqual(miner.CurrentTask.Type, TaskType.Idle);

                // verify storage in assigned building has been unreserved
                Assert.AreEqual(MinersHut.NumAvailableStorageSpots, MinersHut.Defn.NumStorageAreas * 9);
            }
            else
            {
                Town.DestroyBuilding(miner.AssignedBuilding);

                // Verify miner is now assigned to Camp and walking to it
                Assert.AreEqual(miner.AssignedBuilding, Camp);
                Assert.AreEqual(miner.CurrentTask.Type, TaskType.Idle);

                // Verify gathering spots have been unreserved
                Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 0);
                Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, null);
            }
        }
    }

    [Test]
    public void DestroyBuilding_WhileGatheringResources()
    {
        // i = 0 ==> destroy building with resources being gathered
        // i = 1 ==> destroy building which the miner is assigned to
        for (int i = 0; i < 2; i++)
        {
            LoadTestTown("testTown1");

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.GatherResource);
            Assert.AreEqual(miner.CurrentTask.Type, TaskType.GatherResource);

            // wait until gathering resources
            waitUntilTaskSubstate(miner, (int)WorkerTask_GatherResourceSubstate.GatherResourceInBuilding);
            Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
            Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, miner);

            // Destroy the building, verify new state
            if (i == 0)
            {
                Town.DestroyBuilding(StoneMine);

                // Verify miner's task is now to return to assigned building
                Assert.AreEqual(miner.CurrentTask.Type, TaskType.Idle);

                // verify storage in assigned building has been unreserved
                Assert.AreEqual(MinersHut.NumAvailableStorageSpots, MinersHut.Defn.NumStorageAreas * 9);
            }
            else
            {
                Town.DestroyBuilding(miner.AssignedBuilding);

                // Verify miner is now assigned to Camp and walking to it
                Assert.AreEqual(miner.AssignedBuilding, Camp);
                Assert.AreEqual(miner.CurrentTask.Type, TaskType.Idle);

                // Verify gathering spots have been unreserved
                Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 0);
                Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, null);
            }
        }
    }

    [Test]
    public void DestroyBuilding_WhileReturningWithGatheredResource()
    {
        // i = 0 ==> destroy building with resources being gathered
        // i = 1 ==> destroy building which the miner is assigned to
        for (int i = 0; i < 2; i++)
        {
            LoadTestTown("testTown1");

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.GatherResource);
            Assert.AreEqual(miner.CurrentTask.Type, TaskType.GatherResource);

            // wait until returning
            waitUntilTaskSubstate(miner, (int)WorkerTask_GatherResourceSubstate.ReturnToAssignedBuilding);

            // Destroy the building, verify new state
            if (i == 0)
            {
                Town.DestroyBuilding(StoneMine);

                // Verify miner's task is the same
                Assert.AreEqual(miner.CurrentTask.Type, TaskType.GatherResource);
                Assert.AreEqual(miner.CurrentTask.substate, (int)WorkerTask_GatherResourceSubstate.ReturnToAssignedBuilding);

                // verify storage in assigned building has NOT been unreserved
                Assert.AreEqual(MinersHut.NumAvailableStorageSpots, MinersHut.Defn.NumStorageAreas * 9 - 1);

            }
            else
            {
                Town.DestroyBuilding(miner.AssignedBuilding);

                // Verify miner is now assigned to Camp and walking to it
                Assert.AreEqual(miner.AssignedBuilding, Camp);
                Assert.AreEqual(miner.CurrentTask.Type, TaskType.Idle);

                // TODO: What to do with the item that the Worker was carrying?
                // 1. try to carry it to nearest available storagespot, if any
                // 2. if none, then destroy it (for simplicity. don't drop it)

                // Verify gathering spots have been unreserved
                Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 0);
                Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, null);
            }
        }
    }

    [Test]
    public void DestroyBuilding_WhileDroppingGatheredResource()
    {
        // i = 0 ==> destroy building with resources being gathered
        // i = 1 ==> destroy building which the miner is assigned to
        for (int i = 0; i < 2; i++)
        {
            LoadTestTown("testTown1");

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.GatherResource);
            Assert.AreEqual(miner.CurrentTask.Type, TaskType.GatherResource);

            // wait until dropping resources
            waitUntilTaskSubstate(miner, (int)WorkerTask_GatherResourceSubstate.DropGatheredResource);

            // Destroy the building, verify new state
            if (i == 0)
            {
                Town.DestroyBuilding(StoneMine);

                // Verify miner's task is the same
                Assert.AreEqual(miner.CurrentTask.Type, TaskType.GatherResource);
                Assert.AreEqual(miner.CurrentTask.substate, (int)WorkerTask_GatherResourceSubstate.DropGatheredResource);

                // verify storage in assigned building has NOT been unreserved
                Assert.AreEqual(MinersHut.NumAvailableStorageSpots, MinersHut.Defn.NumStorageAreas * 9 - 1);
            }
            else
            {
                Town.DestroyBuilding(miner.AssignedBuilding);

                // Verify miner is now assigned to Camp and walking to it
                Assert.AreEqual(miner.AssignedBuilding, Camp);
                Assert.AreEqual(miner.CurrentTask.Type, TaskType.Idle);

                // TODO: What to do with the item that the Worker was dropping?
                //       options: drop it, destroy it, carry it to storage (if any)

                // Verify gathering spots have been unreserved
                Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 0);
                Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, null);
            }
        }
    }
}
