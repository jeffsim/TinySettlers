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
        Task.text = "AI:\n" + worker.Data.CurrentTask.ToDebugString() + "\n";
        if (worker.Data.ItemInHand != null)
            Items.text = "In hand: " + worker.Data.ItemInHand + "\n";
        else
            Items.text = "empty handed";
    }

    public void OnDestroyClicked() => scene.DestroyWorker(worker);
}
