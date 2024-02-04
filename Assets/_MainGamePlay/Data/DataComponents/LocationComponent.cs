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
    public LocationComponent ParentLoc;

    public Vector3 LocalLoc;
    public Vector3 WorldLoc;

    public LocationComponent() => WorldLoc = Vector2.zero;
    public LocationComponent(LocationComponent sourceLoc) => WorldLoc = sourceLoc.WorldLoc;
    public LocationComponent(Vector3 worldLoc) => WorldLoc = worldLoc;
    public LocationComponent(Vector2 worldLoc) => WorldLoc = worldLoc;
    public LocationComponent(float worldX, float worldY) => WorldLoc = new(worldX, worldY);

    internal float DistanceTo(LocationComponent location) => Vector2.Distance(WorldLoc, location.WorldLoc);
    public bool WithinDistanceOf(LocationComponent location, float closeEnough) => DistanceTo(location) <= closeEnough;

    public static LocationComponent operator -(LocationComponent loc1, LocationComponent loc2) => new(loc1.LocalLoc - loc2.LocalLoc);
    public static LocationComponent operator +(LocationComponent loc1, LocationComponent loc2) => new(loc1.LocalLoc + loc2.LocalLoc);

    // todo: following doesn't account for ParentLoc
    // TODO: Shouldn't be able to set worldloc directly.
    public void SetWorldLoc(LocationComponent location) => WorldLoc = location.WorldLoc;
    public void SetWorldLoc(float x, float y) => WorldLoc.Set(x, y, 0);

    public LocationComponent(LocationComponent parentLoc, Vector2 localLoc) : this(parentLoc, localLoc.x, localLoc.y)
    {
    }

    public LocationComponent(LocationComponent parentLoc, float localX, float localY)
    {
        LocalLoc = new(localX, localY);
        ParentLoc = parentLoc;
        UpdateWorldLoc();
    }

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
        WorldLoc = Vector2.MoveTowards(loc1.WorldLoc, loc2.WorldLoc, t);
    }

    public void UpdateWorldLoc()
    {
        WorldLoc = LocalLoc;
        if (ParentLoc != null)
            WorldLoc += ParentLoc.WorldLoc;
    }

    internal void OffsetMinus(LocationComponent loc1, LocationComponent loc2)
    {
        LocalLoc.x = loc1.LocalLoc.x - loc2.LocalLoc.x;
        LocalLoc.y = loc1.LocalLoc.y - loc2.LocalLoc.y;
        UpdateWorldLoc();
    }
}