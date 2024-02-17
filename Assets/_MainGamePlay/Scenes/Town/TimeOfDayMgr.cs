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
        float dawnStart = 5;
        float duskEnd = 22;
        float sunAngle;
        if (hourOfDay < dawnStart)
            sunAngle = 90f;
        else if (hourOfDay <= duskEnd)
            sunAngle = Mathf.Lerp(90f, -90f, (hourOfDay - dawnStart) / (duskEnd - dawnStart));
        else
            sunAngle = -90f;


        Sun.transform.localEulerAngles = new Vector3(sunAngle, 50, 0);


        // Color ambientColor, dayColor = Color.white, nightColor = new(0.5f, 0.5f, 0.8f);
        // if (hourOfDay <= 3f * hour) // before 3AM: nightcolor
        // {
        //     sunAngle = 180f;
        //     ambientColor = nightColor;
        // }
        // else if (hourOfDay <= 6f * hour) // 3AM-6AM: Transition to night color (morning)
        // {
        //     float t = (hourOfDay * hour - 3f) / 3f;
        //     sunAngle = Mathf.Lerp(180f, 90f, t);
        //     ambientColor = Color.Lerp(nightColor, dayColor, t);
        // }
        // else if (hourOfDay <= 18f * hour) // 6AM-6PM: Daytime
        // {
        //     float t = (hourOfDay * hour - 6f) / 6f;
        //     ambientColor = dayColor;
        //     sunAngle = Mathf.Lerp(90f, -90f, t);
        // }
        // else if (hourOfDay < 21f * hour) // 6PM-9PM: Transition to day color (evening)
        // {
        //     float t = (hourOfDay * hour - 18f) / 18f;
        //     ambientColor = Color.Lerp(dayColor, nightColor, t);
        //     sunAngle = Mathf.Lerp(-90f, -180f, t);
        // }
        // else // after 7PM: nightcolor
        // {
        //     ambientColor = nightColor;
        //     sunAngle = -180f;
        // }
        // RenderSettings.ambientLight = ambientColor;
        // Sun.transform.localEulerAngles = new Vector3(sunAngle, 50, 0);
    }

    public void Update1()
    {
        if (GameTime.IsPaused) return;
        if (Sun == null) return;

        // timeOfDay ranges from 0 (midnight start of day) to 1 (midnight start of next day)
        var timeOfDay = Town.TimeMgr.TimeOfDay;

        // Calculate the sun angle based on the time of day. Example values:
        //  midnight-3AM: timeOfDay<.125; sunAngle = 180 degrees
        //  3AM:  timeOfDay=.125; sunAngle = 180 degrees
        //  6AM:  timeOfDay=.25; sunAngle = 90 degrees
        //  12PM: timeOfDay=.5; sunAngle = 0 degrees
        //  6PM: timeOfDay=.75; sunAngle = -90 degrees
        //  9PM: timeOfDay=.875; sunAngle = -180 degrees
        //  9PM-midnight: timeOfDay>.875; sunAngle = 180 degrees
        // var sunAngle = ...;
        // Sun.transform.localEulerAngles = new Vector3(sunAngle, 50, 0);

        // Set light color to light blue at night (between 6PM and 6AM) and white other times; transition smoothly over 1 hour
        // Example values:
        //  midnight-3AM: timeOfDay<.125; lightColor = Blue
        //  3AM:  timeOfDay=.125; lightColor = Blue
        //  4AM:  timeOfDay=.1667; lightColor = White
        //  5AM:  timeOfDay=.2083; lightColor = Color.Lerp(Blue, White, .2083/.25)
        //  6AM:  timeOfDay=.25; lightColor = White
        //  12PM: timeOfDay=.5; lightColor = White
        //  6PM: timeOfDay=.75; lightColor = White
        //  7PM: timeOfDay=.7917; lightColor = Color.Lerp(White, Blue, .7917/.75)
        //  8PM: timeOfDay=.8333; lightColor = Color.Lerp(White, Blue, .8333/.75)
        //  9PM: timeOfDay=.875; lightColor = Blue
        //  9PM-midnight: timeOfDay>.875; lightColor = Blue
        // var lightColor = ...
        // Sun.color = lightColor;

        // Similarly, Ambient color is white during the day; when transitioning to night, it should transition to Color(0.5f,0.5f, .8f)
        // and when transitioning to day, it should transition back to white. Use same logic as above
        // var ambientColor = ...
        // RenderSettings.ambientLight = ambientColor;
    }
}