using NUnit.Framework;

public partial class MarketTests : MovePauseDestroyTestBase
{
    [Test]
    public void Market_BasicFunctionality()
    {
        // place wood in the Market's storagespots.  should sell it
        LoadTestTown("market_MovePauseDestroy");

        var worker = Town.CreateWorkerInBuilding(Market);
        Market.StorageSpots[0].ItemContainer.Item = CreateItem("wood");

        var origGold = Town.Gold;

        waitUntilTask(worker, TaskType.SellItem);
        var item = (worker.AI.CurrentTask as Task_SellItem).SpotWithItemToSell.ItemContainer.Item;
        waitUntilTaskDone(worker);

        verify_spotIsUnreserved( Market.StorageSpots[0]);
        verify_ItemInHand(worker, null);
        Assert.AreEqual(origGold + item.Defn.BaseSellPrice, Town.Gold, $"{preface("",1)} Expected gold to increase by the sell price of the item");
    }
}