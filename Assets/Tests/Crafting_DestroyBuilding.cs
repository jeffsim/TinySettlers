using System;
using NUnit.Framework;
using UnityEngine;

namespace DestroyBuildingTests
{
    public partial class Crafting : TestBase
    {
        [Test]
        public void DestroyDuring_CraftingTask()
        {
            var substates = new WorkerTask_CraftItemSubstate[] {
                WorkerTask_CraftItemSubstate.GotoSpotWithResource,
                WorkerTask_CraftItemSubstate.PickupResource,
                WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot,
                WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot,
                WorkerTask_CraftItemSubstate.CraftGood,
                WorkerTask_CraftItemSubstate.PickupProducedGood
            };

            for (int i = 0; i < substates.Length; i++)
            {
                // Set up town with one crafting building in the middle, enough resources to craft one item, and one worker assigned to the building
                LoadTestTown("crafting_town1", i);
                var crafter = getAssignedWorker(CraftingStation);

                waitUntilTaskAndSubstate(crafter, TaskType.CraftGood, (int)substates[i]);

                // Destroy the building
                Town.DestroyBuilding(CraftingStation);

                // In all cases, crafter should now be assigned to Camp and idling
                verify_WorkerTaskType(TaskType.Idle, crafter);
                verify_AssignedBuilding(crafter, Camp);
                verify_WorkerTaskSubstate((int)WorkerTask_IdleSubstate.ChooseHowLongToWait, crafter);

                switch (substates[i])
                {
                    case WorkerTask_CraftItemSubstate.GotoSpotWithResource:
                        verify_ItemInHand(crafter, null);
                        verify_ItemsOnGround(2);
                        break;
                    case WorkerTask_CraftItemSubstate.PickupResource:
                        verify_ItemInHand(crafter, null);
                        break;
                    case WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot:
                        verify_ItemInHand(crafter, null);
                        break;
                    case WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot:
                        verify_ItemInHand(crafter, null);
                        break;
                    case WorkerTask_CraftItemSubstate.CraftGood:
                        verify_ItemInHand(crafter, null);
                        break;
                    case WorkerTask_CraftItemSubstate.PickupProducedGood:
                        verify_ItemInHand(crafter, "plank");
                        verify_ItemsOnGround(0);
                        break;
                }
            }
        }
    }
}