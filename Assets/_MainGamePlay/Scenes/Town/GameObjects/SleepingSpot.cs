using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SleepingSpot : MonoBehaviour
{
    public GameObject ReservedIndicator;
    public GameObject Highlight;
    [NonSerialized] public SleepingSpotData Data;
    SceneWithMap scene;
    public TextMeshPro WorkerInSpot;

    int index;

    internal void Initialize(SceneWithMap scene, SleepingSpotData data, int index, Building building)
    {
        this.Data = data;
        this.index = index;
        this.scene = scene;
        name = "Sleeping Spot " + index;
        transform.position = new Vector3(data.Location.WorldLoc.x, data.Location.WorldLoc.y, -.5f);

        // spot.OnItemRemoved += OnItemRemoved;
    }

    void OnDestroy()
    {
        //  spot.OnItemRemoved -= OnItemRemoved;
    }

    public void OnMouseUp()
    {
        // if (!EventSystem.current.IsPointerOverGameObject())
            // scene.OnSleepingSpotClicked(this);
    }

    private void OnItemRemoved(ItemData item)
    {

    }

    void Update()
    {
        ReservedIndicator.SetActive(Data.Reservation.IsReserved);
        if (Data.Reservation.IsReserved)
        {
            WorkerInSpot.text = Data.Reservation.ReservedBy.ToString();
            WorkerInSpot.color = Color.blue;
        }
        else
        {
            WorkerInSpot.text = "E";
            WorkerInSpot.color = Color.white;
        }
        
        // if (spot.IsEmpty)
        //     GetComponentInChildren<Renderer>().material.color = Color.black;
        // else
        // {
        //     GetComponentInChildren<Renderer>().material.color = spot.ItemInStorage.Defn.Color;
        //     name = "Storage " + index + " - " + spot.ItemInStorage.Defn.FriendlyName;
        // }

        // ReservedIndicator.SetActive(Data.Reservation.IsReserved);

        // // highlight this spot if this Sleeping spot is reserved by the currently selected worker
        // bool showHighlight = false;
        // if (scene.WorkerDetails.gameObject.activeSelf && scene.WorkerDetails.worker != null && scene.WorkerDetails.worker.Data.AI.CurrentTask.HasReservedSpot(Data))
        //     showHighlight = true;
        // Highlight.SetActive(showHighlight);

        // var scaleSmall = new Vector3(0, 0, 0);
        // var scaleNormal = new Vector3(1, 1, 1);
        // var ItemInSpotRectTransform = ItemInSpot.GetComponent<RectTransform>();
        // if (Data.ItemContainer.Item != null)
        // {
        //     ItemInSpot.text = Data.ItemContainer.Item.Defn.Id.Substring(0, 2);
        //     ItemInSpot.color = Color.green;
        //     ItemInSpotRectTransform.localScale = scaleNormal;
        // }
        // else if (Data.ItemGrownInSpotDefnId != null)
        // {
        //     ItemInSpot.text = Data.ItemGrownInSpotDefnId.Substring(0, 2);
        //     ItemInSpot.color = Color.Lerp(Color.red, Color.green, Data.PercentGrown);
        //     ItemInSpotRectTransform.localScale = Vector3.Lerp(scaleSmall, scaleNormal, Data.PercentGrown);
        // }
    }
}
