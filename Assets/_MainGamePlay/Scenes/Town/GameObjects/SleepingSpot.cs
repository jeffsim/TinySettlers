using System;
using TMPro;
using UnityEngine;

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
        transform.position = data.Location.WorldLoc;

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
        ReservedIndicator.SetActive(Data.Reservable.IsReserved);
        if (Data.Reservable.IsReserved)
        {
            WorkerInSpot.text = Data.Reservable.ReservedBy.ToString();
            WorkerInSpot.color = Color.blue;
        }
        else
        {
            WorkerInSpot.text = "E";
            WorkerInSpot.color = Color.white;
        }
    }
}
