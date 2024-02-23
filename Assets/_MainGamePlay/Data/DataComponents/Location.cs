using System;
using System.Collections.Generic;
using UnityEngine;

public interface ILocation
{
    Location Location { get; }
}

[Serializable]
public class Location
{
    public override string ToString() => WorldLoc.ToString();

    public Vector3 WorldLoc;

    public static Location operator -(Location loc1, Location loc2) => new(loc1.WorldLoc - loc2.WorldLoc);
    public static Location operator +(Location loc1, Location loc2) => new(loc1.WorldLoc + loc2.WorldLoc);

    public Location() => WorldLoc = Vector3.zero;
    public Location(Vector3 worldLoc) => WorldLoc = worldLoc;
    public void SetWorldLoc(Vector3 location) => WorldLoc = location;
    public void SetWorldLoc(Location location) => WorldLoc = location.WorldLoc;
    public void SetWorldLoc(float x, float y, float z) => WorldLoc.Set(x, y, z);

    internal float DistanceTo(Location location) => Vector2.Distance(new(WorldLoc.x, WorldLoc.z), new(location.WorldLoc.x, location.WorldLoc.z));

    public bool WithinDistanceOf(Location location, float closeEnough) => DistanceTo(location) <= closeEnough;

    public T GetClosest<T>(List<T> locsToCheck, Func<T, bool> isValidCallback = null) where T : ILocation => GetClosest(locsToCheck, out _, isValidCallback);
    public T GetClosest<T>(List<T> locsToCheck, out float closestDist, Func<T, bool> isValidCallback = null) where T : ILocation
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

    internal void MoveTowards(Location loc1, Location loc2, float t)
    {
        WorldLoc = Vector3.MoveTowards(loc1.WorldLoc, loc2.WorldLoc, t);
    }

    internal Vector3 GetWorldLocRelativeTo(Location location, float dy) => new(WorldLoc.x - location.WorldLoc.x, dy, WorldLoc.z - location.WorldLoc.z);
}