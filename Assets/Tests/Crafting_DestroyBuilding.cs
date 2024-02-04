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
            Assert.True(false, "nyi port");
            // var substates = new WorkerTask_CraftItemSubstate[] {
            //     WorkerTask_CraftItemSubstate.GotoSpotWithResource,
            //     WorkerTask_CraftItemSubstate.PickupResource,
            //     WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot,
            //     WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot,
            //     WorkerTask_CraftItemSubstate.CraftGood,
            //     WorkerTask_CraftItemSubstate.PickupProducedGood
            // };

            // for (int i = 0; i < substates.Length; i++)
            // {
            //     // Set up town with one crafting building in the middle, enough resources to craft one item, and one worker assigned to the building
            //     LoadTestTown("crafting_town1", i);
            //     var crafter = getAssignedWorker(CraftingStation);
            //     forceMoveWorkerAwayFromAssignedBuilding(crafter); // ensure that worker task works predictably below.

            //     waitUntilTaskAndSubstate(crafter, TaskType.CraftGood, (int)substates[i]);

            //     // Destroy the building
            //     Town.DestroyBuilding(CraftingStation);

            //     // In all cases, crafter should now be assigned to Camp and idling
            //     verify_WorkerTaskType(TaskType.Idle, crafter);
            //     verify_AssignedBuilding(crafter, Camp);
            //     // verify_WorkerTaskSubstate((int)WorkerTask_IdleSubstate.ChooseHowLongToWait, crafter);

            //     switch (substates[i])
            //     {
            //         case WorkerTask_CraftItemSubstate.GotoSpotWithResource:
            //             // Worker hasn't yet done anything in the building; after destroying the building, they should 
            //             // be holding nothing and the crafting resources still in storage should be abandoned the ground
            //             verify_ItemInHand(crafter, null);
            //             verify_ItemsOnGround(2);
            //             break;
            //         case WorkerTask_CraftItemSubstate.PickupResource:
            //             // Worker reached the first resource and is in the process of picking it up; after destroying the building, they should 
            //             // be holding nothing and the crafting resources still in storage should be abandoned the ground
            //             verify_ItemInHand(crafter, null);
            //             verify_ItemsOnGround(2);
            //             break;
            //         case WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot:
            //             // The Crafting code 'cheats' a bit in that: we don't actually pick up the item, and we we automatically consume it when its picked up
            //             // So if the building is destroyed at this point, the worker should be holding nothing and only the second (not yet picked up) crafting
            //             // resource should be on the ground
            //             verify_ItemInHand(crafter, null);
            //             verify_ItemsOnGround(1);
            //             break;
            //         case WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot:
            //             // The Crafting code 'cheats' a bit in that: we don't actually pick up the item, and we we automatically consume it when its picked up
            //             // So if the building is destroyed at this point, the worker should be holding nothing and only the second (not yet picked up) crafting
            //             // resource should be on the ground
            //             verify_ItemInHand(crafter, null);
            //             verify_ItemsOnGround(1);
            //             break;
            //         case WorkerTask_CraftItemSubstate.CraftGood:
            //             // The Crafting code 'cheats' a bit in that: we don't actually pick up the item, and we we automatically consume it when its picked up.
            //             // So if the building is destroyed at this point, the worker should be holding nothing and the crafting resources should not be on the ground
            //             verify_ItemInHand(crafter, null);
            //             verify_ItemsOnGround(0);
            //             break;
            //         case WorkerTask_CraftItemSubstate.PickupProducedGood:
            //             verify_ItemInHand(crafter, "plank");
            //             verify_ItemsOnGround(0);
            //             break;
            //     }
            // }
        }
    }
}