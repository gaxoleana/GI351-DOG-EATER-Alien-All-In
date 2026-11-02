using System;
using System.Reflection;
using UnityEditor;

namespace AlienAllIn.Editor
{
    [InitializeOnLoad]
    internal static class McpBridgeAutoConnect
    {
        const string AutoStartKey = "MCPForUnity.AutoStartOnLoad";
        const string UvxPathKey = "MCPForUnity.UvxPath";
        const string DefaultUvxPath = @"C:\Users\toone\.local\bin\uvx.exe";

        static McpBridgeAutoConnect()
        {
            if (UnityEngine.Application.isBatchMode) return;
            if (!EditorPrefs.GetBool(AutoStartKey, false))
                EditorPrefs.SetBool(AutoStartKey, true);
            if (string.IsNullOrEmpty(EditorPrefs.GetString(UvxPathKey, "")) && System.IO.File.Exists(DefaultUvxPath))
                EditorPrefs.SetString(UvxPathKey, DefaultUvxPath);
            EditorApplication.delayCall += TryStartBridge;
        }

        static void TryStartBridge()
        {
            try
            {
                Type locatorType = Type.GetType("MCPForUnity.Editor.Services.MCPServiceLocator, MCPForUnity.Editor");
                if (locatorType == null) return;
                PropertyInfo bridgeProp = locatorType.GetProperty("Bridge", BindingFlags.Public | BindingFlags.Static);
                object bridge = bridgeProp?.GetValue(null);
                if (bridge == null) return;
                PropertyInfo isConnectedProp = bridge.GetType().GetProperty("IsConnected", BindingFlags.Public | BindingFlags.Instance);
                if (isConnectedProp != null && isConnectedProp.GetValue(bridge) is bool connected && connected) return;
                MethodInfo startMethod = bridge.GetType().GetMethod("StartAsync", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                startMethod?.Invoke(bridge, null);
            }
            catch
            {
                // Ignore if MCP package is not loaded or busy.
            }
        }
    }
}
