using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MoveBuildingTests : TestBase
{
    [Test]
    public void MoveBuilding_WalkingToGatherResources()
    {
        // i = 0 ==> move building with resources being gathered
        // i = 1 ==> move building which the miner is assigned to
        // i = 2 ==> move random other building
        for (int i = 0; i < 3; i++)
        {
            LoadTestTown("testTown1");

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.GatherResource);

            // wait until walking to target mine
            waitUntilTaskSubstate(miner, (int)WorkerTask_GatherResourceSubstate.GotoResourceBuilding);

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.AssignedBuilding, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // verify miner's task remains the same
            Assert.AreEqual(miner.CurrentTask.Type, TaskType.GatherResource);

            // Verify reserved spots remain so
            Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
            Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, miner);

            // verify miner is moving to new gathering location
            verify_LocsAreEqual(miner.CurrentTask.LastMoveToTarget, StoneMine.GatheringSpots[0].WorldLoc);
        }
    }

    [Test]
    public void MoveBuilding_GatheringResources()
    {
        // i = 0 ==> move building with resources being gathered
        // i = 1 ==> move building which the miner is assigned to
        // i = 2 ==> move random other building
        for (int i = 0; i < 3; i++)
        {
            LoadTestTown("testTown1");

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.GatherResource);

            // wait until walking to target mine
            waitUntilTaskSubstate(miner, (int)WorkerTask_GatherResourceSubstate.GatherResourceInBuilding);

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.AssignedBuilding, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // verify miner's task remains the same
            Assert.AreEqual(miner.CurrentTask.Type, TaskType.GatherResource);

            // Verify reserved spots remain so
            Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
            Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, miner);

            // verify miner is in new gathering location
            verify_LocsAreEqual(miner.WorldLoc, StoneMine.GatheringSpots[0].WorldLoc, "step " + i);
        }
    }

    [Test]
    public void MoveBuilding_ReturningWithGatheredResources()
    {
        // i = 0 ==> move building with resources being gathered
        // i = 1 ==> move building which the miner is assigned to
        // i = 2 ==> move random other building
        for (int i = 0; i < 3; i++)
        {
            LoadTestTown("testTown1");

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.GatherResource);

            // wait until walking to target mine
            waitUntilTaskSubstate(miner, (int)WorkerTask_GatherResourceSubstate.ReturnToAssignedBuilding);

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.AssignedBuilding, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // verify miner's task remains the same
            Assert.AreEqual(miner.CurrentTask.Type, TaskType.GatherResource);

            // verify miner is moving to the new storage loc
            verify_LocsAreEqual(miner.CurrentTask.LastMoveToTarget, miner.AssignedBuilding.StorageAreas[0].StorageSpots[0].WorldLoc, "step " + i);
        }
    }

    [Test]
    public void MoveBuilding_DroppingGatheredResources()
    {
        // i = 0 ==> move building with resources being gathered
        // i = 1 ==> move building which the miner is assigned to
        // i = 2 ==> move random other building
        for (int i = 0; i < 3; i++)
        {
            LoadTestTown("testTown1");

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.GatherResource);

            // wait until walking to target mine
            waitUntilTaskSubstate(miner, (int)WorkerTask_GatherResourceSubstate.DropGatheredResource);

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.AssignedBuilding, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // verify miner's task remains the same
            Assert.AreEqual(miner.CurrentTask.Type, TaskType.GatherResource);

            // verify miner is in new dropping location
            verify_LocsAreEqual(miner.WorldLoc, miner.AssignedBuilding.StorageAreas[0].StorageSpots[0].WorldLoc, "step " + i);
        }
    }
}
