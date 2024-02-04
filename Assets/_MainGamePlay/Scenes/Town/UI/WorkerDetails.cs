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
        Task.text = $"Task: {worker.Data.AI.CurrentTask}\n{worker.Data.AI.CurrentTask.CurSubTask}";
        if (worker.Data.Hands.HasItem)
            Items.text = "In hand: " + worker.Data.Hands.Item + "\n";
        else
            Items.text = "empty handed";
    }

    public void OnDestroyClicked() => scene.DestroyWorker(worker);
}
