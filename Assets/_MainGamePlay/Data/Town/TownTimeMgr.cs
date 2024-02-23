using System;

[Serializable]
public class TownTimeMgr
{
    public override string ToString() => "Time " + TimeOfDay;
    
    public float TimeOfDay; // 0 = midnight; 1 = end of day

    public TownTimeMgr()
    {
        TimeOfDay = .25f; // start every Town at 6AM
    }

    public void Update()
    {
        TimeOfDay += GameTime.deltaTime * Settings.Current.RealTimeToGameTimeMultiplier;
        if (TimeOfDay >= 1)
            TimeOfDay = 0;
    }
}