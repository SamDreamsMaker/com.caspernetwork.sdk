using UnityEngine;
using UnityEditor;
using CasperSDK.Core.Configuration;

namespace CasperSDK.Samples.Editor
{
    /// <summary>
    /// Custom Editor for CasperWalletDemoController.
    /// Shows a "Create Network Config" button when the field is empty.
    /// </summary>
    [CustomEditor(typeof(CasperWalletDemoController))]
    public class CasperWalletDemoControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _networkConfigProp;

        private void OnEnable()
        {
            _networkConfigProp = serializedObject.FindProperty("_networkConfig");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw the NetworkConfig field
            EditorGUILayout.PropertyField(_networkConfigProp);

            // If NetworkConfig is null, show helper button
            if (_networkConfigProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "No Network Config assigned. Using Testnet defaults.\n" +
                    "Create one to customize network settings (Mainnet, custom RPC, etc.)",
                    MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Create Network Config", GUILayout.Width(180)))
                {
                    CreateAndAssignNetworkConfig();
                }
                
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void CreateAndAssignNetworkConfig()
        {
            // Create the NetworkConfig asset
            var config = ScriptableObject.CreateInstance<NetworkConfig>();
            
            // Determine save path
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Network Config",
                "NetworkConfig",
                "asset",
                "Choose where to save the Network Config");

            if (string.IsNullOrEmpty(path))
                return;

            // Save and assign
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            
            _networkConfigProp.objectReferenceValue = config;
            serializedObject.ApplyModifiedProperties();
            
            // Select the new asset
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            
            Debug.Log($"[CasperSDK] NetworkConfig created at: {path}");
        }
    }
}
