using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GatheringSpot : MonoBehaviour
{
    public GameObject ReservedIndicator;
    public GameObject Highlight;
    [NonSerialized] public GatheringSpotData Data;
    SceneWithMap scene;
    public TextMeshPro ItemInSpot;

    int index;

    internal void Initialize(SceneWithMap scene, GatheringSpotData data, int index, Building building)
    {
        this.Data = data;
        this.index = index;
        this.scene = scene;
        name = "Gathering Spot " + index;
        transform.position = data.Location.WorldLoc;

        // spot.OnItemRemoved += OnItemRemoved;
    }

    void OnDestroy()
    {
        //  spot.OnItemRemoved -= OnItemRemoved;
    }

    public void OnMouseUp()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            scene.OnGatheringSpotClicked(this);
    }

    private void OnItemRemoved(ItemData item)
    {

    }

    void Update()
    {
        // if (spot.IsEmpty)
        //     GetComponentInChildren<Renderer>().material.color = Color.black;
        // else
        // {
        //     GetComponentInChildren<Renderer>().material.color = spot.ItemInStorage.Defn.Color;
        //     name = "Storage " + index + " - " + spot.ItemInStorage.Defn.FriendlyName;
        // }
        ReservedIndicator.SetActive(Data.Reservation.IsReserved);

        // highlight this spot if this gathering spot is reserved by the currently selected worker
        bool showHighlight = false;
        if (scene.WorkerDetails.gameObject.activeSelf && scene.WorkerDetails.worker != null && scene.WorkerDetails.worker.Data.AI.CurrentTask.HasReservedSpot(Data))
            showHighlight = true;
        Highlight.SetActive(showHighlight);

        var scaleSmall = new Vector3(0, 0, 0);
        var scaleNormal = new Vector3(1, 1, 1);
        var ItemInSpotRectTransform = ItemInSpot.GetComponent<RectTransform>();
        if (Data.ItemContainer.Item != null)
        {
            ItemInSpot.text = Data.ItemContainer.Item.Defn.Id.Substring(0, 2);
            ItemInSpot.color = Color.green;
            ItemInSpotRectTransform.localScale = scaleNormal;
        }
        else if (Data.ItemGrownInSpotDefnId != null)
        {
            ItemInSpot.text = Data.ItemGrownInSpotDefnId.Substring(0, 2);
            ItemInSpot.color = Color.Lerp(Color.red, Color.green, Data.PercentGrown);
            ItemInSpotRectTransform.localScale = Vector3.Lerp(scaleSmall, scaleNormal, Data.PercentGrown);
        }
    }
}
