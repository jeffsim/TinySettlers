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
            LoadTestTown("testTown1", i);

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.PickupGatherableResource);

            // wait until walking to target mine
            waitUntilTaskSubstate(miner, typeof(WorkerSubtask_WalkToItemSpot));

            var minerOriginalLoc = miner.Location.WorldLoc;

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.Assignment.AssignedTo, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // Verify reserved spots remain so
            Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
            Assert.AreEqual(StoneMine.GatheringSpots[0].Reservation.ReservedBy, miner);

            // verify miner is moving to new gathering location
            verify_LocsAreEqual(miner.AI.CurrentTask.LastMoveToTarget, StoneMine.GatheringSpots[0].Location);

            // verify miner's task remains the same
            Assert.AreEqual(TaskType.PickupGatherableResource, miner.AI.CurrentTask.Type);
            // Assert.AreEqual(typeof(WorkerSubtask_WalkToItemSpot), miner.AI.CurrentTask.substate);

            // verify miner is in same location; wasn't moved due to building moving
            verify_LocsAreEqual(minerOriginalLoc, miner.Location.WorldLoc, "step " + i);
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
            LoadTestTown("testTown1", i);

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.PickupGatherableResource);

            // wait until reaping
            waitUntilTaskSubstate(miner, typeof(WorkerSubtask_ReapGatherableResource));

            var minerOriginalLocRelativeToReapingBuilding = miner.Location.WorldLoc - StoneMine.Location.WorldLoc;

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.Assignment.AssignedTo, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // verify miner's task remains the same
            verify_WorkerTaskType(TaskType.PickupGatherableResource, miner);
            // Assert.AreEqual(typeof(WorkerSubtask_ReapGatherableResource), miner.AI.CurrentTask.substate);

            // verify miner is in new reapping location
            var minerNewLocRelativeToReapingBuilding = miner.Location.WorldLoc - StoneMine.Location.WorldLoc;
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
            LoadTestTown("testTown1", i);

            // Start gathering task
            var miner = getAssignedWorker(MinersHut.DefnId);
            waitUntilTask(miner, TaskType.PickupGatherableResource);

            // wait until reaping
            waitUntilTaskSubstate(miner, typeof(WorkerSubtask_PickupItemFromBuilding));

            var minerOriginalLocRelativeToReapingBuilding = miner.Location.WorldLoc - StoneMine.Location.WorldLoc;

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.Assignment.AssignedTo, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // verify miner's task remains the same
            verify_WorkerTaskType(TaskType.PickupGatherableResource, miner);
            // Assert.AreEqual(typeof(WorkerSubtask_PickupItemFromBuilding), miner.AI.CurrentTask.substate);

            // verify miner is in new reapping location
            var minerNewLocRelativeToReapingBuilding = miner.Location.WorldLoc - StoneMine.Location.WorldLoc;
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
            LoadTestTown("testTown1", i);
            var miner = getAssignedWorker(MinersHut.DefnId);

            // wait until task is to drop off resource in mind
            waitUntilTask(miner, TaskType.DeliverItemInHandToStorageSpot);
            verify_WorkerTaskSubstate(typeof(WorkerSubtask_WalkToItemSpot), miner);

            var minerOriginalLoc = miner.Location.WorldLoc;

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.Assignment.AssignedTo, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // verify miner is moving to the new storage loc
            // verify_LocsAreEqual(miner.AI.CurrentTask.LastMoveToTarget, miner.AssignedBuilding.StorageAreas[0].StorageSpots[0].WorldLoc, "step " + i);

            // verify miner is still moving to correct destination
            // verify_LocsAreEqual(miner.AI.CurrentTask.LastMoveToTarget, StoneMine.GatheringSpots[0].WorldLoc);

            // verify miner's task remains the same
            verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, miner);
            verify_WorkerTaskSubstate(typeof(WorkerSubtask_WalkToItemSpot), miner);

            // verify miner is in same location; wasn't moved due to building moving
            verify_LocsAreEqual(minerOriginalLoc, miner.Location.WorldLoc, "step " + i);
        }
    }

    // private void verify_WorkerTaskSubstate(WorkerTask_DeliverItemInHandToStorageSpotSubstate substate, WorkerData miner)
    // {
    //     Assert.NotNull(miner.AI.CurrentTask);
    //     Assert.AreEqual((int)substate, miner.AI.CurrentTask.substate);
    // }

    [Test]
    public void MoveBuilding_DroppingGatheredResources()
    {
        // i = 0 ==> move building with resources being gathered
        // i = 1 ==> move building which the miner is assigned to
        // i = 2 ==> move random other building
        for (int i = 0; i < 3; i++)
        {
            LoadTestTown("testTown1", i);
            var miner = getAssignedWorker(MinersHut.DefnId);

            // wait until task is to drop off resource in mind
            waitUntilTask(miner, TaskType.DeliverItemInHandToStorageSpot);

            // Wait until actually dropping
            waitUntilTaskSubstate(miner, typeof(WorkerSubtask_DropItemInItemSpot));

            var minerOriginalLocRelativeToDroppingBuilding = miner.Location.WorldLoc - MinersHut.Location.WorldLoc;

            // Move the building, verify new state
            if (i == 0) Town.MoveBuilding(StoneMine, 2, 0);
            else if (i == 1) Town.MoveBuilding(miner.Assignment.AssignedTo, 2, 0);
            else Town.MoveBuilding(Camp, 2, 0);

            // verify miner's task remains the same
            verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, miner);
            // Assert.AreEqual((int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot, miner.AI.CurrentTask.substate);

            // verify miner is in new dropping location
            var minerNewLocRelativeToDroppingBuilding = miner.Location.WorldLoc - MinersHut.Location.WorldLoc;
            verify_LocsAreEqual(minerOriginalLocRelativeToDroppingBuilding, minerNewLocRelativeToDroppingBuilding, "step " + i);
        }
    }
}
