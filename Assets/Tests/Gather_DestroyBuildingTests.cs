using NUnit.Framework;

namespace DestroyBuildingTests
{
    public partial class Gathering : TestBase
    {
        [Test]
        public void Gather_DestroyBuildingTests_WalkingTo()
        {
            // i = 0 ==> destroy building with resources being gathered
            // i = 1 ==> destroy building which the miner is assigned to
            // i = 2 ==> destroy random other building
            for (int i = 0; i < 3; i++)
            {
                LoadTestTown("testTown4", i);

                // Start gathering task
                var miner = getAssignedWorker(MinersHut.DefnId);
                waitUntilTask(miner, TaskType.PickupGatherableResource);

                // wait until walking to target mine
                waitUntilTaskSubstate(miner, (int)WorkerTask_PickupGatherableResourceSubstate.GotoGatheringSpot);

                // Destroy the building
                Town.DestroyBuilding(new BuildingData[] { StoneMine, MinersHut, Market }[i]);

                switch (i)
                {
                    case 0: // destroy building with resources being gathered
                            // Miner should be idling
                        verify_WorkerTaskType(TaskType.Idle, miner);
                        verify_AssignedBuilding(miner, MinersHut);
                        verify_WorkerTaskSubstate((int)WorkerTask_IdleSubstate.ChooseHowLongToWait, miner);
                        break;
                    case 1: //  destroy building which the miner is assigned to
                            // Miner should be idling and reassigned
                        verify_WorkerTaskType(TaskType.Idle, miner);
                        verify_AssignedBuilding(miner, Camp);
                        verify_WorkerTaskSubstate((int)WorkerTask_IdleSubstate.ChooseHowLongToWait, miner);
                        break;
                    case 2: //destroy random other building
                            // no change
                        verify_WorkerTaskType(TaskType.PickupGatherableResource, miner);
                        verify_WorkerTaskSubstate((int)WorkerTask_PickupGatherableResourceSubstate.GotoGatheringSpot, miner);
                        break;
                }
            }
        }

        [Test]
        public void Gather_DestroyBuildingTests_Reaping()
        {
            // i = 0 ==> destroy building with resources being gathered
            // i = 1 ==> destroy building which the miner is assigned to
            // i = 2 ==> destroy random other building
            for (int i = 0; i < 3; i++)
            {
                LoadTestTown("testTown4", i);

                // Start gathering task
                var miner = getAssignedWorker(MinersHut.DefnId);
                waitUntilTask(miner, TaskType.PickupGatherableResource);

                // wait until reaping
                waitUntilTaskSubstate(miner, (int)WorkerTask_PickupGatherableResourceSubstate.ReapGatherableResource);

                // Destroy the building
                Town.DestroyBuilding(new BuildingData[] { StoneMine, MinersHut, Market }[i]);

                switch (i)
                {
                    case 0: // destroy building with resources being gathered
                            // Miner should be idling
                        verify_WorkerTaskType(TaskType.Idle, miner);
                        verify_AssignedBuilding(miner, MinersHut);
                        verify_WorkerTaskSubstate((int)WorkerTask_IdleSubstate.ChooseHowLongToWait, miner);
                        break;
                    case 1: //  destroy building which the miner is assigned to
                            // Miner should be idling and reassigned
                        verify_WorkerTaskType(TaskType.Idle, miner);
                        verify_AssignedBuilding(miner, Camp);
                        verify_WorkerTaskSubstate((int)WorkerTask_IdleSubstate.ChooseHowLongToWait, miner);
                        break;
                    case 2: //destroy random other building
                            // no change
                        verify_WorkerTaskType(TaskType.PickupGatherableResource, miner);
                        verify_WorkerTaskSubstate((int)WorkerTask_PickupGatherableResourceSubstate.ReapGatherableResource, miner);
                        break;
                }
            }
        }
        [Test]
        public void Gather_DestroyBuildingTests_PickingUp()
        {
            // i = 0 ==> destroy building with resources being gathered
            // i = 1 ==> destroy building which the miner is assigned to
            // i = 2 ==> destroy random other building
            for (int i = 0; i < 3; i++)
            {
                LoadTestTown("testTown4", i);

                // Start gathering task
                var miner = getAssignedWorker(MinersHut.DefnId);
                waitUntilTask(miner, TaskType.PickupGatherableResource);

                // wait until reaping
                waitUntilTaskSubstate(miner, (int)WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource);

                // Destroy the building
                Town.DestroyBuilding(new BuildingData[] { StoneMine, MinersHut, Market }[i]);

                switch (i)
                {
                    case 0: // destroy building with resources being gathered
                            // Even though building was destroyed, miner should still be picking up resources
                        verify_WorkerTaskSubstate((int)WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource, miner);
                        verify_WorkerTaskType(TaskType.PickupGatherableResource, miner);
                        verify_AssignedBuilding(miner, MinersHut);
                        break;
                    case 1: //  destroy building which the miner is assigned to
                            // Miner should be idling and reassigned
                        verify_WorkerTaskType(TaskType.Idle, miner);
                        verify_AssignedBuilding(miner, Camp);
                        verify_WorkerTaskSubstate((int)WorkerTask_IdleSubstate.ChooseHowLongToWait, miner);
                        break;
                    case 2: //destroy random other building
                            // no change
                        verify_WorkerTaskType(TaskType.PickupGatherableResource, miner);
                        verify_WorkerTaskSubstate((int)WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource, miner);
                        break;
                }
            }
        }
    }
}