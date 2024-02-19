using System;
using System.Collections.Generic;
using UnityEngine;

public interface ILocationProvider
{
    LocationComponent Location { get; }
}

[Serializable]
public class LocationComponent
{
    public override string ToString() => WorldLoc.ToString();

    public Vector3 WorldLoc;

    public static LocationComponent operator -(LocationComponent loc1, LocationComponent loc2) => new(loc1.WorldLoc - loc2.WorldLoc);
    public static LocationComponent operator +(LocationComponent loc1, LocationComponent loc2) => new(loc1.WorldLoc + loc2.WorldLoc);

    public LocationComponent() => WorldLoc = Vector3.zero;
    public LocationComponent(Vector3 worldLoc) => WorldLoc = worldLoc;
    public void SetWorldLoc(Vector3 location) => WorldLoc = location;
    public void SetWorldLoc(LocationComponent location) => WorldLoc = location.WorldLoc;
    public void SetWorldLoc(float x, float y, float z) => WorldLoc.Set(x, y, z);

    internal float DistanceTo(LocationComponent location) => Vector2.Distance(new(WorldLoc.x, WorldLoc.z), new(location.WorldLoc.x, location.WorldLoc.z));

    public bool WithinDistanceOf(LocationComponent location, float closeEnough) => DistanceTo(location) <= closeEnough;

    public T GetClosest<T>(List<T> locsToCheck, Func<T, bool> isValidCallback = null) where T : ILocationProvider => GetClosest(locsToCheck, out _, isValidCallback);
    public T GetClosest<T>(List<T> locsToCheck, out float closestDist, Func<T, bool> isValidCallback = null) where T : ILocationProvider
    {
        T closest = default;
        closestDist = float.MaxValue;
        foreach (var locToCheck in locsToCheck)
            if (isValidCallback == null || isValidCallback(locToCheck))
            {
                float dist = DistanceTo(locToCheck.Location);
                if (dist < closestDist)
                {
                    closest = locToCheck;
                    closestDist = dist;
                }
            }
        return closest;
    }

    internal void MoveTowards(LocationComponent loc1, LocationComponent loc2, float t)
    {
        WorldLoc = Vector3.MoveTowards(loc1.WorldLoc, loc2.WorldLoc, t);
    }

    internal Vector3 GetWorldLocRelativeTo(LocationComponent location, float dy) => new(WorldLoc.x - location.WorldLoc.x, dy, WorldLoc.z - location.WorldLoc.z);
}