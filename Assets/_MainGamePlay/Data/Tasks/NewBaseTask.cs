using System;

[Serializable]
public class NewBaseTask : Task
{
    public NewBaseTask(WorkerData worker, NeedData needData) : base(worker, needData)
    {
    }
    
    public override void Start()
    {
        TaskState = TaskState.Started;

        foreach (var spot in SpotsToReserveOnStart)
            ReserveSpot(spot);

        SubtaskIndex = 0;
        CurSubTask = GetNextSubtask();
        CurSubTask.Start();
    }

    public override void GotoNextSubstate()
    {
        CurSubTask?.SubtaskComplete();

        SubtaskIndex++;
        CurSubTask = GetNextSubtask();
        if (CurSubTask == null)
            AllSubtasksComplete();
        else
        {
            CurSubTask.Start();
            if (CurSubTask.InstantlyComplete)
                GotoNextSubstate();
        }
    }

    public override void OnSubtaskStart() { }
    public override void InitializeStateMachine() { }
}