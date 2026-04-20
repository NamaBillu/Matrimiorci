using UnityEngine;

public static class Logger
{
    public static void Log(string message)
    {
        if (Debug.isDebugBuild)
        {
            Debug.Log(message);
        }
    }
}