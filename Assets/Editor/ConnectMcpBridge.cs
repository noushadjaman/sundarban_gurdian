using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ConnectMcpBridge
{
    static ConnectMcpBridge()
    {
        EditorApplication.delayCall += async () =>
        {
            try
            {
                Debug.Log("[ConnectMcpBridge] Initializing MCP bridge connection...");
                var serviceLocatorType = Type.GetType("MCPForUnity.Editor.Services.MCPServiceLocator, MCPForUnity.Editor");
                if (serviceLocatorType == null)
                {
                    Debug.LogError("[ConnectMcpBridge] MCPServiceLocator type not found in MCPForUnity.Editor assembly.");
                    return;
                }

                var bridgeProp = serviceLocatorType.GetProperty("Bridge", BindingFlags.Public | BindingFlags.Static);
                var bridge = bridgeProp?.GetValue(null);
                if (bridge == null)
                {
                    Debug.LogError("[ConnectMcpBridge] Bridge service instance is null.");
                    return;
                }

                var startMethod = bridge.GetType().GetMethod("StartAsync", Type.EmptyTypes);
                if (startMethod == null)
                {
                    Debug.LogError("[ConnectMcpBridge] StartAsync method not found on bridge.");
                    return;
                }

                var task = (Task<bool>)startMethod.Invoke(bridge, null);
                bool result = await task;
                Debug.Log($"[ConnectMcpBridge] MCP Bridge StartAsync completed with result: {result}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConnectMcpBridge] Error starting bridge: {ex}");
            }
        };
    }
}
