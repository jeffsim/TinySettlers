using System;
using UnityEngine;

public class TimeOfDayMgr : MonoBehaviour
{
    public Light Sun;
    [NonSerialized] TownData Town;

    public void InitializeForTown(TownData town)
    {
        Town = town;
    }

    public void Update()
    {
        if (GameTime.IsPaused) return;
        if (Sun == null) return;

        // timeOfDay ranges from 0 (midnight start of day) to 1 (midnight start of next day)
        var hour = 1f / 24f;
        var hourOfDay = Town.TimeMgr.TimeOfDay / hour;

        // full day = 90 to -90.  6AM to 9PM
        float dawnStart = 5, dawnEnd = 7;
        float duskStart = 20, duskEnd = 22;
        float sunAngle;

        if (hourOfDay < dawnStart) sunAngle = 180f;
        else if (hourOfDay <= duskEnd) sunAngle = Mathf.Lerp(180f, 0f, (hourOfDay - dawnStart) / (duskEnd - dawnStart));
        else sunAngle = 0f;
        Sun.transform.localEulerAngles = new(sunAngle, 30, 0);

        Color ambientColor, dayColor = Color.white, nightColor = new(0.7f, 0.7f, 1);
        if (hourOfDay < dawnStart) ambientColor = nightColor;
        else if (hourOfDay <= dawnEnd) ambientColor = Color.Lerp(nightColor, dayColor, (hourOfDay - dawnStart) / (dawnEnd - dawnStart));
        else if (hourOfDay <= duskStart) ambientColor = dayColor;
        else if (hourOfDay <= duskEnd) ambientColor = Color.Lerp(dayColor, nightColor, (hourOfDay - duskStart) / (duskEnd - duskStart));
        else ambientColor = nightColor;
        RenderSettings.ambientLight = ambientColor;
    }
}