using NUnit.Framework;

public class MoveBuildingTests : TestBase
{
    [Test]
    public void MoveBuilding_GatheringResources_WalkingTo()
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
    public void MoveBuilding_GatheringResources_Reaping()
    {
        // i = 0 ==> move building with resources being gathered
        // i = 1 ==> move building which the miner is assigned to
        // i = 2 ==> move random other building
        for (int i = 0; i < 3; i++)
        {
            LoadTestTown("testTown1");

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.PickupGatherableResource);

            // wait until reaping
            waitUntilTaskSubstate(miner, (int)WorkerTask_PickupGatherableResourceSubstate.ReapGatherableResource);

            var minerOriginalLocRelativeToReapingBuilding = miner.WorldLoc - StoneMine.WorldLoc;

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.AssignedBuilding, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // verify miner's task remains the same
            verify_WorkerTaskType(TaskType.PickupGatherableResource, miner);
            Assert.AreEqual((int)WorkerTask_PickupGatherableResourceSubstate.ReapGatherableResource, miner.CurrentTask.substate);

            // verify miner is in new reapping location
            var minerNewLocRelativeToReapingBuilding = miner.WorldLoc - StoneMine.WorldLoc;
            verify_LocsAreEqual(minerOriginalLocRelativeToReapingBuilding, minerNewLocRelativeToReapingBuilding, "step " + i);
        }
    }

    [Test]
    public void MoveBuilding_GatheringResources_PickingUp()
    {
        // i = 0 ==> move building with resources being gathered
        // i = 1 ==> move building which the miner is assigned to
        // i = 2 ==> move random other building
        for (int i = 0; i < 3; i++)
        {
            LoadTestTown("testTown1");

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.PickupGatherableResource);

            // wait until reaping
            waitUntilTaskSubstate(miner, (int)WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource);

            var minerOriginalLocRelativeToReapingBuilding = miner.WorldLoc - StoneMine.WorldLoc;

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.AssignedBuilding, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // verify miner's task remains the same
            verify_WorkerTaskType(TaskType.PickupGatherableResource, miner);
            Assert.AreEqual((int)WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource, miner.CurrentTask.substate);

            // verify miner is in new reapping location
            var minerNewLocRelativeToReapingBuilding = miner.WorldLoc - StoneMine.WorldLoc;
            verify_LocsAreEqual(minerOriginalLocRelativeToReapingBuilding, minerNewLocRelativeToReapingBuilding, "step " + i);
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
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.AssignedBuilding, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // verify miner's task remains the same
            verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, miner);
            Assert.AreEqual((int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot, miner.CurrentTask.substate);

            // verify miner is in new dropping location
            var minerNewLocRelativeToDroppingBuilding = miner.WorldLoc - MinersHut.WorldLoc;
            verify_LocsAreEqual(minerOriginalLocRelativeToDroppingBuilding, minerNewLocRelativeToDroppingBuilding, "step " + i);
        }
    }
}
