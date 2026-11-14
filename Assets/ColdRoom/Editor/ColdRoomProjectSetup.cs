using System.IO;
using ColdRoom.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ColdRoom.EditorTools
{
    public static class ColdRoomProjectSetup
    {
        private const string Root = "Assets/ColdRoom";
        private const string ScenePath = Root + "/Scenes/ColdRoom.unity";
        private const string PanelSettingsPath = Root + "/Resources/ColdRoomPanelSettings.asset";
        private const string ThemePath = Root + "/UI/ColdRoomTheme.tss";

        [InitializeOnLoadMethod]
        private static void AutoScaffold()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    return;
                }

                if (File.Exists(ScenePath))
                {
                    return;
                }

                BuildAppScene();
            };
        }

        [MenuItem("ColdRoom/Setup/Rebuild App Scene")]
        public static void BuildAppScene()
        {
            EnsureFolders();
            ApplyPlayerSettings();

            PanelSettings panelSettings = EnsurePanelSettings();
            if (panelSettings == null)
            {
                Debug.LogError("[ColdRoom] Could not create PanelSettings. Check that ColdRoomTheme.tss imported correctly.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(7, 16, 25, 255);
            camera.cullingMask = 0;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var appObject = new GameObject("ColdRoomApp");
            var document = appObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            appObject.AddComponent<ColdRoomApp>();
            EditorUtility.SetDirty(document);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            Debug.Log("[ColdRoom] App scene created at " + ScenePath);
        }

        [MenuItem("ColdRoom/Setup/Open App Scene")]
        public static void OpenAppScene()
        {
            if (!File.Exists(ScenePath))
            {
                BuildAppScene();
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/ColdRoom");
            EnsureFolder("Assets/ColdRoom/Scenes");
            EnsureFolder("Assets/ColdRoom/UI");
            EnsureFolder("Assets/ColdRoom/Resources");
            EnsureFolder("Assets/ColdRoom/Scripts");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = "ColdRoom";
            PlayerSettings.companyName = "ColdRoom Logistics";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 800;
            PlayerSettings.runInBackground = true;
        }

        private static PanelSettings EnsurePanelSettings()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                Debug.LogError("[ColdRoom] Missing theme at " + ThemePath);
                return null;
            }

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            }

            panel.themeStyleSheet = theme;
            panel.scaleMode = PanelScaleMode.ConstantPixelSize;
            panel.scale = 1f;
            panel.clearColor = true;
            panel.colorClearValue = new Color32(7, 16, 25, 255);
            panel.sortingOrder = 0;
            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            return panel;
        }
    }
}
