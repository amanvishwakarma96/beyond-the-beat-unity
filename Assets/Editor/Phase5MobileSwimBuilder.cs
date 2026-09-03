using System;
using System.Linq;
using BeyondTheBeat.CameraSystem;
using BeyondTheBeat.UI;
using BeyondTheBeat.Water;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyondTheBeat.Editor
{
    internal static class Phase5MobileSwimBuilder
    {
        private const string ScenePath = Phase5OceanBuilder.Phase5ScenePath;
        private const string CanvasName = "MobileDrivingCanvas";
        private const string DrivingControlsName = "DrivingControls";
        private const string VehicleName = "PrototypeVehicle";
        private const string CameraName = "GameplayCamera";
        private const string SwimPrototypeRootName = "Phase5SwimPrototype";
        private const string SwimActorName = "SwimPrototypeActor";
        private const string SwimControlsName = "SwimControls";
        private const string EnterSwimName = "SwimModeEnter";
        private const string ExitSwimName = "SwimModeExit";
        private const string ValidationDocPath = "Docs/Validation/PHASE_5_MOBILE_SWIM_CAMERA.md";

        [MenuItem("Beyond The Beat/Phase 5/Build Mobile Swim + Camera Integration")]
        public static void BuildMobileSwimCameraIntegration()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException($"Phase 5 mobile swim build requires scene '{ScenePath}'.");
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject canvasObject = RequireRoot(scene, CanvasName);
            GameObject vehicle = RequireRoot(scene, VehicleName);
            GameObject cameraObject = RequireRoot(scene, CameraName);
            GameObject swimRoot = RequireRoot(scene, SwimPrototypeRootName);
            Transform swimmer = swimRoot.transform.Find(SwimActorName);
            if (swimmer == null)
            {
                throw new InvalidOperationException($"Missing '{SwimActorName}' under '{SwimPrototypeRootName}'.");
            }

            MobileDrivingInput drivingInput = canvasObject.GetComponent<MobileDrivingInput>() ??
                                              throw new InvalidOperationException("MobileDrivingCanvas is missing MobileDrivingInput.");
            CameraFollow cameraFollow = cameraObject.GetComponent<CameraFollow>() ??
                                        throw new InvalidOperationException("GameplayCamera is missing CameraFollow.");
            SwimController swimController = swimmer.GetComponent<SwimController>() ??
                                            throw new InvalidOperationException("SwimPrototypeActor is missing SwimController.");
            Transform drivingControls = canvasObject.transform.Find(DrivingControlsName) ??
                                        throw new InvalidOperationException("DrivingControls root is missing from MobileDrivingCanvas.");

            RemoveChild(canvasObject.transform, SwimControlsName);
            RemoveChild(canvasObject.transform, EnterSwimName);

            AquaticModeCoordinator existingCoordinator = canvasObject.GetComponent<AquaticModeCoordinator>();
            if (existingCoordinator != null)
            {
                UnityEngine.Object.DestroyImmediate(existingCoordinator);
            }

            GameObject swimControlsObject = new GameObject(SwimControlsName, typeof(RectTransform));
            swimControlsObject.transform.SetParent(canvasObject.transform, false);
            StretchFullScreen(swimControlsObject.GetComponent<RectTransform>());
            MobileSwimInput swimInput = swimControlsObject.AddComponent<MobileSwimInput>();

            TouchHoldButton left = CreateTouchButton(swimControlsObject.transform, "SwimLeft", "LEFT", new Vector2(145f, 145f), new Vector2(170f, 170f), false);
            TouchHoldButton right = CreateTouchButton(swimControlsObject.transform, "SwimRight", "RIGHT", new Vector2(345f, 145f), new Vector2(170f, 170f), false);
            TouchHoldButton back = CreateTouchButton(swimControlsObject.transform, "SwimBack", "BACK", new Vector2(-365f, 145f), new Vector2(175f, 175f), true);
            TouchHoldButton forward = CreateTouchButton(swimControlsObject.transform, "SwimForward", "SWIM", new Vector2(-165f, 145f), new Vector2(190f, 190f), true);
            TouchHoldButton dive = CreateTouchButton(swimControlsObject.transform, "Dive", "DIVE", new Vector2(-355f, 360f), new Vector2(180f, 84f), true);
            TouchHoldButton surface = CreateTouchButton(swimControlsObject.transform, "Surface", "SURFACE", new Vector2(-155f, 360f), new Vector2(190f, 84f), true);

            ConfigureSwimInput(swimInput, swimController, left, right, forward, back, dive, surface);

            GameObject enterControl = CreateModeButton(canvasObject.transform, EnterSwimName, "SWIM TEST", new Vector2(0f, -58f), new Vector2(230f, 72f), new Vector2(0.5f, 1f));
            GameObject exitControl = CreateModeButton(swimControlsObject.transform, ExitSwimName, "DRIVE", new Vector2(0f, -58f), new Vector2(190f, 72f), new Vector2(0.5f, 1f));

            AquaticModeCoordinator coordinator = canvasObject.AddComponent<AquaticModeCoordinator>();
            ConfigureCoordinator(
                coordinator,
                drivingInput,
                swimInput,
                swimController,
                cameraFollow,
                vehicle.transform,
                swimmer,
                drivingControls.gameObject,
                swimControlsObject,
                enterControl,
                exitControl);

            Button enterButton = enterControl.GetComponent<Button>();
            Button exitButton = exitControl.GetComponent<Button>();
            UnityEventTools.AddPersistentListener(enterButton.onClick, coordinator.EnterSwimMode);
            UnityEventTools.AddPersistentListener(exitButton.onClick, coordinator.ExitSwimMode);
            EditorUtility.SetDirty(enterButton);
            EditorUtility.SetDirty(exitButton);

            coordinator.SetSwimMode(false, true, false);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Unable to save mobile swim integration into '{ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = swimControlsObject;
            Debug.Log("[Beyond The Beat] Phase 5 mobile swim controls and single-camera handoff integration created.");
        }

        [MenuItem("Beyond The Beat/Phase 5/Validate Mobile Swim + Camera Integration")]
        public static void ValidateMobileSwimCameraIntegration()
        {
            if (!ValidateMobileSwimCameraIntegrationInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static bool ValidateMobileSwimCameraIntegrationOrThrow()
        {
            if (ValidateMobileSwimCameraIntegrationInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateMobileSwimCameraIntegrationInternal(out string message)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                message = $"[Beyond The Beat] Phase 5 mobile swim validation FAIL: scene missing at '{ScenePath}'.";
                return false;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene scene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject canvasObject = FindRoot(scene, CanvasName);
                GameObject vehicle = FindRoot(scene, VehicleName);
                GameObject cameraObject = FindRoot(scene, CameraName);
                GameObject swimRoot = FindRoot(scene, SwimPrototypeRootName);
                Transform swimmer = swimRoot != null ? swimRoot.transform.Find(SwimActorName) : null;
                Transform drivingControls = canvasObject != null ? canvasObject.transform.Find(DrivingControlsName) : null;
                Transform swimControls = canvasObject != null ? canvasObject.transform.Find(SwimControlsName) : null;
                Transform enterControl = canvasObject != null ? canvasObject.transform.Find(EnterSwimName) : null;
                Transform exitControl = swimControls != null ? swimControls.Find(ExitSwimName) : null;

                MobileDrivingInput drivingInput = canvasObject != null ? canvasObject.GetComponent<MobileDrivingInput>() : null;
                AquaticModeCoordinator coordinator = canvasObject != null ? canvasObject.GetComponent<AquaticModeCoordinator>() : null;
                MobileSwimInput swimInput = swimControls != null ? swimControls.GetComponent<MobileSwimInput>() : null;
                SwimController swimController = swimmer != null ? swimmer.GetComponent<SwimController>() : null;
                CameraFollow cameraFollow = cameraObject != null ? cameraObject.GetComponent<CameraFollow>() : null;

                bool structurePass = canvasObject != null && vehicle != null && cameraObject != null && swimmer != null &&
                                     drivingControls != null && swimControls != null && enterControl != null && exitControl != null &&
                                     drivingInput != null && swimInput != null && swimController != null && coordinator != null && cameraFollow != null;

                bool inputPass = structurePass &&
                                 swimInput.SwimController == swimController &&
                                 swimInput.HasRequiredControls &&
                                 swimInput.DirectTouchFallbackEnabled &&
                                 swimInput.LegacyTouchFallbackEnabled &&
                                 swimControls.GetComponentsInChildren<TouchHoldButton>(true).Length == 6;

                bool mappingPass = ValidateInputMapping(swimInput);

                bool cameraPass = structurePass &&
                                  coordinator.CameraFollow == cameraFollow &&
                                  coordinator.VehicleCameraTarget == vehicle.transform &&
                                  coordinator.SwimCameraTarget == swimmer &&
                                  scene.GetRootGameObjects()
                                      .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                                      .Count(candidate => candidate.enabled) == 1;

                bool handoffPass = structurePass && ValidateModeHandoff(
                    coordinator,
                    drivingInput,
                    swimInput,
                    cameraFollow,
                    vehicle.transform,
                    swimmer,
                    drivingControls.gameObject,
                    swimControls.gameObject,
                    enterControl.gameObject,
                    exitControl.gameObject);

                bool buttonsPass =
                    enterControl != null && enterControl.GetComponent<Button>()?.onClick.GetPersistentEventCount() > 0 &&
                    exitControl != null && exitControl.GetComponent<Button>()?.onClick.GetPersistentEventCount() > 0;

                bool inheritedPass =
                    FindRoot(scene, "Phase5OceanArea") != null &&
                    FindRoot(scene, "Phase4FreeRoamActivities") != null &&
                    FindRoot(scene, "ParkingPrototype") != null &&
                    FindRoot(scene, "Phase1MissionSystem") != null &&
                    FindRoot(scene, "Phase3RestrictedArea") != null;

                bool buildSettingsPass = EditorBuildSettings.scenes.Length == 1 &&
                                         EditorBuildSettings.scenes[0].enabled &&
                                         string.Equals(EditorBuildSettings.scenes[0].path, ScenePath, StringComparison.Ordinal);
                bool docPass = AssetDatabase.LoadAssetAtPath<TextAsset>(ValidationDocPath) != null || System.IO.File.Exists(ValidationDocPath);

                bool pass = structurePass && inputPass && mappingPass && cameraPass && handoffPass && buttonsPass &&
                            inheritedPass && buildSettingsPass && docPass;

                message = pass
                    ? "[Beyond The Beat] Phase 5 mobile swim/camera validation PASS: shared mobile canvas, six swim controls, deterministic input mapping, drive↔swim camera target handoff, one enabled gameplay camera, inherited roots, single-scene build settings and validation documentation are intact."
                    : "[Beyond The Beat] Phase 5 mobile swim/camera validation FAIL: " +
                      $"structure={structurePass}, input={inputPass}, mapping={mappingPass}, camera={cameraPass}, handoff={handoffPass}, " +
                      $"buttons={buttonsPass}, inherited={inheritedPass}, buildSettings={buildSettingsPass}, doc={docPass}.";
                return pass;
            }
            finally
            {
                if (openedForValidation && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static bool ValidateInputMapping(MobileSwimInput input)
        {
            if (input == null)
            {
                return false;
            }

            input.EvaluateButtonStatesForValidation(
                true, false, true, false, true, false,
                out Vector2 move, out bool dive, out bool surface);
            bool diagonal = move.x < -0.6f && move.y > 0.6f && dive && !surface;

            input.EvaluateButtonStatesForValidation(
                false, true, false, true, true, true,
                out move, out dive, out surface);
            bool conflict = move.x > 0.6f && move.y < -0.6f && !dive && surface;
            return diagonal && conflict;
        }

        private static bool ValidateModeHandoff(
            AquaticModeCoordinator coordinator,
            MobileDrivingInput drivingInput,
            MobileSwimInput swimInput,
            CameraFollow cameraFollow,
            Transform vehicleTarget,
            Transform swimTarget,
            GameObject drivingControls,
            GameObject swimControls,
            GameObject enterControl,
            GameObject exitControl)
        {
            coordinator.SetSwimMode(false, false, false);
            bool driveBaseline = !coordinator.IsSwimMode && drivingInput.enabled && !swimInput.InputEnabled &&
                                 drivingControls.activeSelf && !swimControls.activeSelf && enterControl.activeSelf &&
                                 !exitControl.activeSelf && cameraFollow.Target == vehicleTarget;

            coordinator.SetSwimMode(true, false, false);
            bool swimMode = coordinator.IsSwimMode && !drivingInput.enabled && swimInput.InputEnabled &&
                            !drivingControls.activeSelf && swimControls.activeSelf && !enterControl.activeSelf &&
                            exitControl.activeSelf && cameraFollow.Target == swimTarget;

            coordinator.SetSwimMode(false, false, false);
            bool driveRestored = !coordinator.IsSwimMode && drivingInput.enabled && !swimInput.InputEnabled &&
                                 drivingControls.activeSelf && !swimControls.activeSelf && enterControl.activeSelf &&
                                 cameraFollow.Target == vehicleTarget;
            return driveBaseline && swimMode && driveRestored;
        }

        private static void ConfigureSwimInput(
            MobileSwimInput input,
            SwimController controller,
            TouchHoldButton left,
            TouchHoldButton right,
            TouchHoldButton forward,
            TouchHoldButton back,
            TouchHoldButton dive,
            TouchHoldButton surface)
        {
            SerializedObject serialized = new SerializedObject(input);
            SetObjectReference(serialized, "swimController", controller);
            SetObjectReference(serialized, "moveLeftButton", left);
            SetObjectReference(serialized, "moveRightButton", right);
            SetObjectReference(serialized, "moveForwardButton", forward);
            SetObjectReference(serialized, "moveBackButton", back);
            SetObjectReference(serialized, "diveButton", dive);
            SetObjectReference(serialized, "surfaceButton", surface);
            SetBool(serialized, "enableDirectTouchFallback", true);
            SetBool(serialized, "enableLegacyTouchFallback", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            input.SetInputEnabled(false);
        }

        private static void ConfigureCoordinator(
            AquaticModeCoordinator coordinator,
            MobileDrivingInput drivingInput,
            MobileSwimInput swimInput,
            SwimController swimController,
            CameraFollow cameraFollow,
            Transform vehicleTarget,
            Transform swimTarget,
            GameObject drivingControls,
            GameObject swimControls,
            GameObject enterControl,
            GameObject exitControl)
        {
            SerializedObject serialized = new SerializedObject(coordinator);
            SetObjectReference(serialized, "drivingInput", drivingInput);
            SetObjectReference(serialized, "swimInput", swimInput);
            SetObjectReference(serialized, "swimController", swimController);
            SetObjectReference(serialized, "cameraFollow", cameraFollow);
            SetObjectReference(serialized, "vehicleCameraTarget", vehicleTarget);
            SetObjectReference(serialized, "swimCameraTarget", swimTarget);
            SetObjectReference(serialized, "drivingControlsRoot", drivingControls);
            SetObjectReference(serialized, "swimControlsRoot", swimControls);
            SetObjectReference(serialized, "enterSwimControl", enterControl);
            SetObjectReference(serialized, "exitSwimControl", exitControl);
            SetBool(serialized, "startInSwimMode", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TouchHoldButton CreateTouchButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            bool rightAnchor)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TouchHoldButton));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            Vector2 anchor = rightAnchor ? new Vector2(1f, 0f) : Vector2.zero;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            Color normal = new Color(0.04f, 0.18f, 0.28f, 0.92f);
            Color pressed = new Color(0.06f, 0.58f, 0.72f, 1f);
            image.color = normal;
            image.raycastTarget = true;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 8f);
            labelRect.offsetMax = new Vector2(-8f, -8f);

            Text text = labelObject.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 28;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 28;
            text.color = Color.white;
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            TouchHoldButton button = buttonObject.GetComponent<TouchHoldButton>();
            button.ConfigureVisual(image, normal, pressed);
            return button;
        }

        private static GameObject CreateModeButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.02f, 0.36f, 0.50f, 0.94f);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 6f);
            labelRect.offsetMax = new Vector2(-8f, -6f);
            Text text = labelObject.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 24;
            text.color = Color.white;
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return buttonObject;
        }

        private static void StretchFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            return FindRoot(scene, name) ?? throw new InvalidOperationException($"Required root '{name}' is missing from Phase 5 scene.");
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => string.Equals(root.name, name, StringComparison.Ordinal));
        }

        private static void RemoveChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void SetObjectReference(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(name) ??
                                          throw new InvalidOperationException($"Missing serialized object property '{name}'.");
            property.objectReferenceValue = value;
        }

        private static void SetBool(SerializedObject serialized, string name, bool value)
        {
            SerializedProperty property = serialized.FindProperty(name) ??
                                          throw new InvalidOperationException($"Missing serialized bool property '{name}'.");
            property.boolValue = value;
        }
    }
}
