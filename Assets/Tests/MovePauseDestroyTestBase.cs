public abstract class MovePauseDestroyTestBase : TestBase
{
    string mpdTownDefnId;
    int mpdSubtask;
    
    protected void PrepMPDTest(string townDefnId, int subtask)
    {
        mpdTownDefnId = townDefnId;
        mpdSubtask = subtask;
    }
    protected void SetupMPDTest(out BuildingData store1, out BuildingData store2, bool destroyStores = false)
    {
        LoadTestTown(mpdTownDefnId, mpdSubtask);
        store1 = getBuildingByTestId("store1");
        store2 = getBuildingByTestId("store2");
        if (destroyStores)
        {
            Town.DestroyBuilding(store1);
            Town.DestroyBuilding(store2);
        }
    }
    protected void SetupMPDTest(string townDefnId, int subtask, out BuildingData store1, out BuildingData store2, bool destroyStores = false)
    {
        LoadTestTown(townDefnId, subtask);
        store1 = getBuildingByTestId("store1");
        store2 = getBuildingByTestId("store2");
        if (destroyStores)
        {
            Town.DestroyBuilding(store1);
            Town.DestroyBuilding(store2);
        }
    }
}
