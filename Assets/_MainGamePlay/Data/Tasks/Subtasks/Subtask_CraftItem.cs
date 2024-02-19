using UnityEngine;

public class Subtask_CraftItem : Subtask
{
    protected override float RunTime => 1;
    [SerializeField] IMultipleItemSpotInBuilding ItemsSpot;
    public override ItemData GetTaskItem() => CraftedItem;// GameDefns.Instance.ItemDefns[CraftingItemDefnId];
    public string CraftingItemDefnId;
    public ItemData CraftedItem;

    public Subtask_CraftItem(Task parentTask, string craftingItemDefnId, IMultipleItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemsSpot = itemSpot;
        CraftingItemDefnId = craftingItemDefnId;
        UpdateWorkerLocWhenBuildingMoves(ItemsSpot.Building);
        CraftedItem = new ItemData() { DefnId = CraftingItemDefnId };
    }

    public override void SubtaskComplete()
    {
        Task.Worker.Hands.SetItem(CraftedItem);

        // Consume the resources in the CraftingSpot
        ItemsSpot.ItemsContainer.ClearItems();
    }
}