using NUnit.Framework;

namespace DestroyBuildingTests
{
    public partial class Selling : TestBase
    {
        // TODO: Test what happens if no storage (drops item on ground)

        [Test]
        public void DestroyDuring_SellingTask()
        {
            var substates = new WorkerTask_SellItemSubstate[] {
                WorkerTask_SellItemSubstate.GotoItemToSell,
                WorkerTask_SellItemSubstate.PickupItemToSell,
                WorkerTask_SellItemSubstate.SellItem
            };

            for (int i = 0; i < substates.Length; i++)
            {
                // Set up town with one market building in the middle, one item to sell in the market's storage, and one worker assigned to the building
                LoadTestTown("selling_town1");
                var seller = getAssignedWorker(Market);

                waitUntilTaskAndSubstate(seller, TaskType.SellItem, (int)substates[i]);

                // Destroy the building
                Town.DestroyBuilding(Market);

                // In all cases, seller should now be assigned to Camp and idling
                verify_WorkerTaskType(TaskType.Idle, seller);
                verify_AssignedBuilding(seller, Camp);
                verify_WorkerTaskSubstate((int)WorkerTask_IdleSubstate.ChooseHowLongToWait, seller);
                
                switch (substates[i])
                {
                    case WorkerTask_SellItemSubstate.GotoItemToSell:
                        verify_ItemInHand(seller, null);
                        break;
                    case WorkerTask_SellItemSubstate.PickupItemToSell:
                        verify_ItemInHand(seller, null);
                        break;
                    case WorkerTask_SellItemSubstate.SellItem:
                        verify_ItemInHand(seller, "wood");
                        break;
                }
            }
        }
    }
}