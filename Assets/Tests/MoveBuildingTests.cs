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
        // i = 2 ==> move random other building (but not in a way that impacts where we would choose to gather)
        for (int i = 0; i < 3; i++)
        {
            LoadTestTown("testTown1");

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.PickupGatherableResource);

            // wait until walking to target mine
            waitUntilTaskSubstate(miner, (int)WorkerTask_PickupGatherableResourceSubstate.GotoGatheringSpot);

            var minerOriginalLoc = miner.WorldLoc;

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.AssignedBuilding, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // Verify reserved spots remain so
            Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
            Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, miner);

            // verify miner is moving to new gathering location
            verify_LocsAreEqual(miner.CurrentTask.LastMoveToTarget, StoneMine.GatheringSpots[0].WorldLoc);

            // verify miner's task remains the same
            Assert.AreEqual(TaskType.PickupGatherableResource, miner.CurrentTask.Type);
            Assert.AreEqual((int)WorkerTask_PickupGatherableResourceSubstate.GotoGatheringSpot, miner.CurrentTask.substate);

            // verify miner is in same location; wasn't moved due to building moving
            verify_LocsAreEqual(minerOriginalLoc, miner.WorldLoc, "step " + i);
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
            var miner = getAssignedWorker(MinersHut.DefnId);

            // wait until task is to drop off resource in mind
            waitUntilTask(miner, TaskType.DeliverItemInHandToStorageSpot);
            Assert.AreEqual((int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.GotoStorageSpotToDeliverItemTo, miner.CurrentTask.substate);

            var minerOriginalLoc = miner.WorldLoc;

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.AssignedBuilding, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // verify miner is moving to the new storage loc
            // verify_LocsAreEqual(miner.CurrentTask.LastMoveToTarget, miner.AssignedBuilding.StorageAreas[0].StorageSpots[0].WorldLoc, "step " + i);


            // verify miner is still moving to correct destination
            // verify_LocsAreEqual(miner.CurrentTask.LastMoveToTarget, StoneMine.GatheringSpots[0].WorldLoc);

            // verify miner's task remains the same
            verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, miner);
            verify_WorkerTaskSubstate(WorkerTask_DeliverItemInHandToStorageSpotSubstate.GotoStorageSpotToDeliverItemTo, miner);

            Assert.AreEqual((int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.GotoStorageSpotToDeliverItemTo, miner.CurrentTask.substate);

            // verify miner is in same location; wasn't moved due to building moving
            verify_LocsAreEqual(minerOriginalLoc, miner.WorldLoc, "step " + i);
        }
    }

    private void verify_WorkerTaskSubstate(WorkerTask_DeliverItemInHandToStorageSpotSubstate substate, WorkerData miner)
    {
        Assert.NotNull(miner.CurrentTask);
        Assert.AreEqual((int)substate, miner.CurrentTask.substate);
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
            var miner = getAssignedWorker(MinersHut.DefnId);

            // wait until task is to drop off resource in mind
            waitUntilTask(miner, TaskType.DeliverItemInHandToStorageSpot);

            // Wait until actually dropping
            waitUntilTaskSubstate(miner, (int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot);

            var minerOriginalLocRelativeToDroppingBuilding = miner.WorldLoc - MinersHut.WorldLoc;

            // Move the building, verify new state
            if (i == 0)
                Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1)
                Town.MoveBuilding(miner.AssignedBuilding, 2, 0);
            else
                Town.MoveBuilding(Camp, 2, 0);

            // verify miner's task remains the same
            verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, miner);
            Assert.AreEqual((int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot, miner.CurrentTask.substate);

            // verify miner is in new dropping location
            var minerNewLocRelativeToDroppingBuilding = miner.WorldLoc - MinersHut.WorldLoc;

            verify_LocsAreEqual(minerOriginalLocRelativeToDroppingBuilding, minerNewLocRelativeToDroppingBuilding, "step " + i);
        }
    }
}
