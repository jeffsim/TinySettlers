using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.Compilation;
#endif
using UnityEngine;
using UnityEngine.UIElements;

public enum Direction
{
    right,
    left,
    down,
    up,
    rightUp,
    leftUp,
    rightDown,
    leftDown,
    none
}

public static class Utilities
{
#if UNITY_EDITOR
    public static void ForceRecompile()
    {
#if UNITY_2019_3_OR_NEWER
        CompilationPipeline.RequestScriptCompilation();
#elif UNITY_2017_1_OR_NEWER
        var editorAssembly = Assembly.GetAssembly(typeof(Editor));
        var editorCompilationInterfaceType = editorAssembly.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface");
        var dirtyAllScriptsMethod = editorCompilationInterfaceType.GetMethod("DirtyAllScripts", BindingFlags.Static | BindingFlags.Public);
        dirtyAllScriptsMethod.Invoke(editorCompilationInterfaceType, null);
#endif
    }
#endif

    public static Vector3 LocationWithinDistance(Vector3 worldLoc, float maxDistance)
    {
        var circle = UnityEngine.Random.insideUnitCircle * maxDistance;
        return worldLoc + (Vector3)circle;
    }

    internal static string getNeedsDebugString(List<NeedData> needs, bool includeBuildingId)
    {
        var str = "";
        needs.Sort((a, b) => (int)((b.Priority - a.Priority) * 1000));

        foreach (var need in needs)
        {
            str += need.Priority.ToString("0.00") + " ";
            if (includeBuildingId)
            {
                if (need.Type != NeedType.PickupAbandonedItem)
                    str += need.BuildingWithNeed + ": ";
            }
            str += getNeedText(need) + "\n";
        }
        return str;
    }

    static string getNeedText(NeedData need)
    {
        var str = "";
        switch (need.Type)
        {
            case NeedType.ClearStorage: str += "Clear Storage"; break;
            case NeedType.ConstructionWorker: str += "Construction Worker"; break;
            case NeedType.CraftingOrConstructionMaterial: str += "Need Item: " + need.NeededItem; break;
            case NeedType.GatherResource: str += "Gather (" + need.NeededItem.Id + ")"; break;
            case NeedType.PersistentRoomNeed: str += "Persistent need"; break;
            case NeedType.PickupAbandonedItem: str += "Pickup item: " + need.AbandonedItemToPickup; break;
            case NeedType.SellGood: str += "Sell good: " + need.NeededItem; break;
            default: Debug.LogError("unknown need type " + need.Type); break;
        };
        return str;
    }

    public static bool areFloatsEqual(float f1, float f2)
    {
        return Mathf.Abs(f1 - f2) < Mathf.Epsilon;
    }

    public static string SerializeObject<T>(T dataObject)
    {
        if (dataObject == null)
        {
            return string.Empty;
        }
        try
        {
            using (StringWriter stringWriter = new StringWriter())
            {
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(stringWriter, dataObject);
                return stringWriter.ToString();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            return string.Empty;
        }
    }

    public static float Clamp(float value, int min, int max)
    {
        return Math.Max(0, Math.Min(1, value));
    }

    public static T DeserializeObject<T>(string xml)
         where T : new()
    {
        if (string.IsNullOrEmpty(xml))
        {
            return new T();
        }
        try
        {
            using (var stringReader = new StringReader(xml))
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(stringReader);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            return new T();
        }
    }

    public static void addPrefabToGO(GameObject go, GameObject prefab)
    {
        GameObject instantiatedPrefab = GameObject.Instantiate(prefab);
        instantiatedPrefab.transform.SetParent(go.transform, false);
    }

    public static T GetRequiredComponent<T>(MonoBehaviour go)
    {
        var comp = go.GetComponent<T>();
        // DebugMgr.Assert(comp != null, go.name + " Requires a component of type " + typeof(T).ToString());
        return comp;
    }

    public static T FindGameObject<T>() where T : Component
    {
        // For fuck's sake; Resources.FindObjectsOfTypeAll includes objects that aren't in the scene.
        // And of course I can't use Object.FindObjectsOfType since that fucker doesn't include inactive objects
        // And of course I don't want to have my in-scene objects start out active just so I can find them and then set them to inactive
        foreach (T comp in Resources.FindObjectsOfTypeAll<T>() as T[])
        {
#if UNITY_EDITOR
            if (!EditorUtility.IsPersistent(comp.gameObject.transform.root.gameObject))
                return comp;
#else
                return comp;
#endif
        }
        return null;
    }

    public static string Pluralize(string str)
    {
        // hackity
        string lower = str.ToLower();
        if (lower.EndsWith("y"))
            return str.Substring(0, str.Length - 1) + "ies";
        if (lower.EndsWith("s"))
            return str.Substring(0, str.Length - 1);
        return str + "s";
    }
}

public static class GameObjectExtensions
{
    /// <summary>
    /// Checks if a GameObject has been destroyed.
    /// </summary>
    /// <param name="gameObject">GameObject reference to check for destructedness</param>
    /// <returns>If the game object has been marked as destroyed by UnityEngine</returns>
    public static bool IsDestroyed(this GameObject gameObject)
    {
        // UnityEngine overloads the == opeator for the GameObject type
        // and returns null when the object has been destroyed, but 
        // actually the object is still there but has not been cleaned up yet
        // if we test both we can determine if the object has been destroyed.
        return gameObject == null && !ReferenceEquals(gameObject, null);
    }

    public static GameObject FindChildByName(this GameObject parent, string name)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform child = parent.transform.GetChild(i);
            if (child.name == name)
                return child.gameObject;
            GameObject grandChild = child.FindChildByName(name);
            if (grandChild != null)
                return grandChild;
        }
        return null;
    }

    public static void RemoveAllChildren(this GameObject parent)
    {
        int tries = 0;
        while (parent.transform.childCount > 0)
        {
            Transform child = parent.transform.GetChild(0);
            child.gameObject.RemoveAllChildren();
            UnityEngine.Object.DestroyImmediate(child.gameObject);
            if (tries++ > 10000)
            {
                Debug.Log("Stuck in loop in removeallchildren");
                return;
            }
        }
    }
}

public static class TransformExtensions
{
    public static GameObject FindObjectWithTag(this Transform parent, string tag)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.tag == tag)
                return child.gameObject;
            if (child.childCount > 0)
            {
                var go = FindObjectWithTag(child, tag);
                if (go != null)
                    return go;
            }
        }
        return null;
    }

    public static List<GameObject> FindObjectsWithTag(this Transform parent, string tag)
    {
        List<GameObject> taggedGameObjects = new List<GameObject>();

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.tag == tag)
            {
                taggedGameObjects.Add(child.gameObject);
            }
            if (child.childCount > 0)
            {
                taggedGameObjects.AddRange(FindObjectsWithTag(child, tag));
            }
        }
        return taggedGameObjects;
    }

    public static GameObject FindChildByName(this Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    //#if UNITY_EDITOR
    //    public class AssignMaterial : ScriptableWizard
    //    {
    //        public bool runRecursively = true;
    //        public Material theMaterial;
    //        String strHelp = "Select Game Objects";
    //        GameObject[] gos;

    //        void OnWizardUpdate()
    //        {
    //            helpString = strHelp;
    //            isValid = (theMaterial != null);
    //        }

    //        void OnWizardCreate()
    //        {
    //            gos = Selection.gameObjects;
    //            foreach (GameObject go in gos)
    //            {
    //                changeMaterial(go);
    //            }
    //        }

    //        void changeMaterial(GameObject go)
    //        {
    //            if (go.GetComponent<Renderer>())
    //            {
    //                go.GetComponent<Renderer>().material = theMaterial;
    //            }
    //            if (runRecursively == true)
    //            {
    //                for (int i = 0; i < go.transform.childCount; i++)
    //                {
    //                    changeMaterial(go.transform.GetChild(i).gameObject);
    //                }
    //            }
    //        }

    //        [MenuItem("Custom/Assign Material", false, 4)]
    //        static void assignMaterial()
    //        {
    //            ScriptableWizard.DisplayWizard("Assign Material", typeof(AssignMaterial), "Assign");
    //        }
    //    }
    //#endif


#if UNITY_EDITOR

    // // Used to debug serialization
    // private void DEBUG_FindSerializingObjectsOfType(object property, Type typeToFind)
    // {
    //     if (property == null)
    //     {
    //         return;
    //     }

    //     var props = property.GetType()
    //                         .GetProperties()
    //                         .Where(p => p.PropertyType.IsClass &&
    //                                    !p.PropertyType.IsPrimitive)
    //                         .ToList();

    //     foreach (var prop in props)
    //     {
    //         if (prop.PropertyType == typeof(EntityVisual))
    //         {
    //             var address = prop.GetValue(property) as EntityVisual;
    //             Debug.Log(address);
    //             // addresses?.Add(address);
    //         }

    //         var next = prop.GetValue(obj: property);

    //         // When property is a collection
    //         if (next is IEnumerable collection)
    //         {
    //             foreach (var item in collection)
    //                 DEBUG_FindSerializingObjectsOfType(item, typeToFind);
    //         }
    //         else
    //         {
    //             DEBUG_FindSerializingObjectsOfType(next, typeToFind);
    //         }
    //     }
    // }
#endif
}
