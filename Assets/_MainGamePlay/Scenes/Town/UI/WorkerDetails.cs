using System;
using TMPro;
using UnityEngine;

public class WorkerDetails : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Task;
    public TextMeshProUGUI Items;
    public Worker worker;
    SceneWithMap scene;

    public void ShowForWorker(SceneWithMap scene, Worker worker)
    {
        gameObject.SetActive(true);
        this.scene = scene;
        this.worker = worker;
        Name.text = worker.ToString();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (worker == null)
            return;

        var str = "AI:\n";
        str += worker.Data.CurrentTask.ToDebugString() + "\n";

        // foreach (var need in building.Data.Needs)
        // {
        //     switch (need.Type)
        //     {
        //         case NeedType.ClearStorage: str += "Clear Storage"; break;
        //         case NeedType.ConstructionWorker: str += "Const Worker"; break;
        //         case NeedType.CraftingOrConstructionMaterial: str += "Need Item (" + need.NeededItem.Id + ")"; break;
        //         case NeedType.GatherResource: str += "Gather (" + need.NeededItem.Id + ")"; break;
        //         case NeedType.PersistentRoomNeed: str += "Persistent need"; break;
        //     };
        //     str += ": " + need.Priority + ", " + need.WorkersMeetingNeed.Count + "\n";
        // }
        Task.text = str;

        // if (building.Data.Defn.CanStoreItems)
        // {
        //     str = "Items:\n";
        //     foreach (var area in building.Data.StorageAreas)
        //         foreach (var spot in area.StorageSpots)
        //             if (spot.ItemInStorage != null)
        //                 str += spot.ItemInStorage.DefnId + "\n";
        //     Items.text = str;
        // }
    }

    public void OnDestroyClicked() => scene.DestroyWorker(worker);
}
