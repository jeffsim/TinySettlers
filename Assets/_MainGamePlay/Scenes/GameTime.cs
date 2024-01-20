using System;
using UnityEngine;

// this exists so that we can replace it in Tests
public static class GameTime
{
    // TODO (CLEANUP): UGH, all of this
    static public bool IsTest;

    static float _lastSetNonZeroTimeScale = 1;
    static float _testTimeScale = 1;
    static public float timeScale
    {
        get
        {
            if (IsTest) return _testTimeScale;
            else return Time.timeScale;
        }
        set
        {
            if (value != 0) _lastSetNonZeroTimeScale = value;
            if (IsTest) _testTimeScale = value;
            else
                Time.timeScale = value;
        }
    }

    static float _testTime;
    static public float time
    {
        get
        {
            return _testTime;
        }
        set
        {
            _testTime = value;
        }
    }


    static float _testDeltaTime;
    static public float deltaTime
    {
        get
        {
            if (IsTest) return _testDeltaTime;
            else return Time.deltaTime;
        }
        set
        {
            if (IsTest) _testDeltaTime = value;
            else
                Debug.Assert(false, "not allowed");
        }
    }

    static public bool IsPaused => timeScale == 0;

    static public void Test_Update()
    {
        // Assume 1/100th second per tick for tests
        deltaTime = .01f * _testTimeScale;
        time += deltaTime;
    }

    static public void Update()
    {
        time += Time.unscaledDeltaTime;
    }

    static public void TogglePause()
    {
        if (IsPaused)
            timeScale = _lastSetNonZeroTimeScale;
        else
            timeScale = 0;
    }
}