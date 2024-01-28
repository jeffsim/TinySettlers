using System.Collections.Generic;
using NUnit.Framework;

public class GatherResourceTests : TestBase
{
    [Test]
    public void GatheringSpotsAreReserved()
    {
        LoadTestTown("testTown1");

        // Verify starting state
        var miner = getAssignedWorker(MinersHut.DefnId);
        Assert.AreEqual(miner.CurrentTask.Type, TaskType.Idle);

        // Start gathering task
        waitUntilTask(miner, TaskType.PickupGatherableResource);

        // Verify Worker reserved a gathering spot in the mine
        Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
        Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, miner);

        // Verify Worker reserved a storage spot in the Miners hut (which is closer to the mine than the camp)
        Assert.AreEqual(MinersHut.NumReservedStorageSpots, 1);
        Assert.AreEqual((miner.CurrentTask as WorkerTask_PickupGatherableResource).ReservedStorageSpot.ReservedBy, miner);

        // wait until actually gathering (reaping) resource in target building
        waitUntilTaskSubstate(miner, (int)WorkerTask_PickupGatherableResourceSubstate.ReapGatherableResource);

        // wait until done gathering (reaping) and are now picking up
        waitUntilTaskSubstate(miner, (int)WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource);

        // wait until done picking up; gathering spot should no longer be reserve
        waitUntilTaskDone(miner);
        Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 0);

        // wait until restarted task; should be reserved again
        waitUntilTask(miner, TaskType.PickupGatherableResource);
        Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
        Assert.AreEqual(StoneMine.GatheringSpots[0].ReservedBy, miner);
    }

    [Test]
    public void GatherUntilFull()
    {
        LoadTestTown("testTown1");

        var miner = getAssignedWorker(MinersHut.DefnId);

        // Gather until 9 items in miner's hut storage; at that point, miner should be idle
        float breakTime = GameTime.time + 100;
        while (GameTime.time < breakTime && MinersHut.NumItemsInStorage() < 9)
            updateTown();

        Assert.AreEqual(miner.CurrentTask.Type, TaskType.Idle);
        Assert.AreEqual(MinersHut.StorageAreas[0].NumItemsInStorage(StoneMine.Defn.ResourcesThatCanBeGatheredFromHere[0]), 9);
    }

    [Test]
    public void GatheringSpotsAreReserved_PingPongGatherers()
    {
        // setup: miners hut with 3 miners; stonemine with 1 gathering spot.  Verify they tradeoff correctly
        LoadTestTown("testTown2");

        // wait until a miner has a gathering task, two are idle
        List<WorkerData> gatherers, idlers;
        waitUntilAssignments(out gatherers, 1, out idlers, 2);
        WorkerData gatherer0 = gatherers[0], idler0 = idlers[0], idler1 = idlers[1];

        // Verify Gatherer reserved a gathering spot in the mine
        Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
        verify_anyGatheringSpotInBuildingReservedByWorker(StoneMine, gatherer0);

        // wait until actually gathering resource in target building
        waitUntilTaskSubstate(gatherer0, (int)WorkerTask_GatherResourceSubstate.GatherResourceInBuilding);
        Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
        verify_anyGatheringSpotInBuildingReservedByWorker(StoneMine, gatherer0);

        // wait until done gathering; gathering spot should now be reserved by one of the idlers and they should be going
        waitUntilTaskSubstate(gatherer0, (int)WorkerTask_GatherResourceSubstate.ReturnToAssignedBuilding);
        updateTown();
        Assert.AreEqual(StoneMine.NumReservedGatheringSpots, 1);
        // verify_anyGatheringSpotInBuildingReservedByWorker(StoneMine, gatherer0);
        // Assert.IsTrue(StoneMine.WorkersThatReservedGatheringSpots.Contains(idler0) || StoneMine.WorkersThatReservedGatheringSpots.Contains(idler1));

        // waitUntilAssignments(out gatherers, 2, out idlers, 1);
    }

    private void verify_anyGatheringSpotInBuildingReservedByWorker(BuildingData building, WorkerData worker)
    {
        foreach (var spot in building.GatheringSpots)
            if (spot.IsReserved && spot.ReservedBy == worker)
                return;
        Assert.Fail("spot not reserved by worker in " + building.DefnId);
    }

    private void waitUntilAssignments(out List<WorkerData> gatherers, int numGatherers, out List<WorkerData> idlers, int numIdlers)
    {
        float breakTime = GameTime.time + 30;

        gatherers = new List<WorkerData>();
        idlers = new List<WorkerData>();
        while (GameTime.time < breakTime)
        {
            updateTown();
            foreach (var worker in Town.Workers)
                if (worker.CurrentTask.Type == TaskType.GatherResource)
                    gatherers.Add(worker);
                else if (worker.CurrentTask.Type == TaskType.Idle)
                    idlers.Add(worker);
            if (gatherers.Count == numGatherers && idlers.Count == numIdlers)
                break;
            gatherers.Clear();
            idlers.Clear();
        }
        Assert.IsTrue(GameTime.time < breakTime, "stuck in loop in waitUntilAssignments");
    }
}
