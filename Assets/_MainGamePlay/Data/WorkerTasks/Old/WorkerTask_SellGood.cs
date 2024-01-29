// using System;
// using System.Collections.Generic;
// using UnityEngine;

// public enum WorkerTask_SellGoodSubstate
// {
//     GotoSpotWithGoodToSell = 0,
//     SellGood = 1,
// };

// [Serializable]
// public class WorkerTask_SellGood : WorkerTask
// {
//     public override string ToString() => "Sell good (" +(StorageSpotWithGoodToSell.ItemInSpot==null?"NULL" : StorageSpotWithGoodToSell.ItemInSpot.Defn.Id) + ")";
//     public override TaskType Type => TaskType.SellGood;

//     [SerializeField] StorageSpotData StorageSpotWithGoodToSell;

//     public const float secondsToSell = 1;

//     // Used to draw walking lines in debug mode
//     public override bool IsWalkingToTarget => substate == (int)WorkerTask_SellGoodSubstate.GotoSpotWithGoodToSell;

//     public override ItemDefn GetTaskItem() => StorageSpotWithGoodToSell.ItemInSpot.Defn;

//     public override string ToDebugString()
//     {
//         var str = "Sell\n";
//         str += "  Item: " + StorageSpotWithGoodToSell.ItemInSpot.Defn.Id + "\n";
//         str += "  FromStorageSpot: " + StorageSpotWithGoodToSell.InstanceId + " (" + StorageSpotWithGoodToSell.Building.DefnId + ")\n";
//         str += "  substate: " + substate;
//         switch (substate)
//         {
//             case (int)WorkerTask_SellGoodSubstate.GotoSpotWithGoodToSell: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, StorageSpotWithGoodToSell.WorldLoc).ToString("0.0"); break;
//             case (int)WorkerTask_SellGoodSubstate.SellGood: str += "; per = " + getPercentSubstateDone(secondsToSell); break;
//             default: Debug.LogError("unknown substate " + substate); break;
//         }
//         return str;
//     }

//     // TODO: Pooling
//     public static WorkerTask_SellGood Create(WorkerData worker, NeedData need, StorageSpotData storageSpotWithItem)
//     {
//         return new WorkerTask_SellGood(worker, need, storageSpotWithItem);
//     }

//     private WorkerTask_SellGood(WorkerData worker, NeedData need, StorageSpotData storageSpotWithItem) : base(worker, need)
//     {
//         Need = need;
//         StorageSpotWithGoodToSell = storageSpotWithItem;
//     }

//     public override void Start()
//     {
//         base.Start();
//         reserveStorageSpot(StorageSpotWithGoodToSell);
//     }

//     public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
//     {
//         if (building != Worker.AssignedBuilding) return;

//         // If we're moving towards the building that was moved, then update our movement target
//         // If we're working in the building that was moved, then update our location
//         // Note: can't always just move our world loc if our building moved, because we may be moving to the
//         // building (e.g. immediately after being assigned to it)
//         WorkerTask_SellGoodSubstate s = (WorkerTask_SellGoodSubstate)substate;
//         if (s == WorkerTask_SellGoodSubstate.GotoSpotWithGoodToSell)
//         {
//             LastMoveToTarget += building.WorldLoc - previousWorldLoc;
//         }
//         else if (s == WorkerTask_SellGoodSubstate.SellGood)
//         {
//             Worker.WorldLoc += building.WorldLoc - previousWorldLoc;
//         }
//     }

//     public override void Update()
//     {
//         base.Update();

//         switch (substate)
//         {
//             case (int)WorkerTask_SellGoodSubstate.GotoSpotWithGoodToSell:
//                 if (MoveTowards(StorageSpotWithGoodToSell.WorldLoc, distanceMovedPerSecond))
//                     gotoSubstate((int)WorkerTask_SellGoodSubstate.SellGood);
//                 break;

//             case (int)WorkerTask_SellGoodSubstate.SellGood: // craft
//                 if (getPercentSubstateDone(secondsToSell) == 1)
//                 {
//                     // Done dropping.  Add the item into the storage spot.  Complete the task first so that the spot is unreserved so that we can add to it
//                     CompleteTask();
//                     Worker.Town.ItemSold(StorageSpotWithGoodToSell.ItemInSpot);
//                     StorageSpotWithGoodToSell.RemoveItem();
//                 }
//                 break;


//             default:
//                 Debug.LogError("unknown substate " + substate);
//                 break;
//         }
//     }
// }