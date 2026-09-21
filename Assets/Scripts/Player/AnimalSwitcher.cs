using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Lightweight switcher allowing seamless switching between playing as the Royal Bengal Tiger
    /// and the Chitra Spotted Deer in both Edit/Play Mode.
    /// Keyboard shortcuts: '1' for Tiger, '2' for Deer, 'Tab' to toggle.
    /// Automatically updates CameraFollow target, species preset, and GameManager.
    /// </summary>
    public class AnimalSwitcher : MonoBehaviour
    {
        [Header("Animal References")]
        public GameObject tigerObject;
        public GameObject deerObject;

        [Header("Current Active Species")]
        public string activeSpecies = "Tiger";

        private CameraFollow cameraFollow;
        private GameManager gameManager;

        private void Awake()
        {
            FindAnimals();

            var cam = Camera.main;
            if (cam != null) cameraFollow = cam.GetComponent<CameraFollow>();
            gameManager = Object.FindAnyObjectByType<GameManager>();
        }

        private void FindAnimals()
        {
            if (tigerObject == null) tigerObject = GameObject.Find("Tiger");
            if (deerObject == null) deerObject = GameObject.Find("Deer");

            if (tigerObject == null || deerObject == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var go in all)
                {
                    if (go.hideFlags != HideFlags.None) continue;
                    if (tigerObject == null && go.name == "Tiger" && go.scene.isLoaded) tigerObject = go;
                    if (deerObject == null && go.name == "Deer" && go.scene.isLoaded) deerObject = go;
                }
            }
        }

        private void Start()
        {
            FindAnimals();

            if (gameManager != null && !string.IsNullOrEmpty(gameManager.currentSpecies))
            {
                if (gameManager.currentSpecies.ToLower().Contains("deer"))
                {
                    SwitchToDeer();
                    return;
                }
            }

            SwitchToTiger();
        }

        private void Update()
        {
            // Desktop key shortcuts (Safe for both Input System package and legacy Input)
#if ENABLE_INPUT_SYSTEM
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                    {
                        SwitchToTiger();
                    }
                    else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                    {
                        SwitchToDeer();
                    }
                    else if (kb.tabKey.wasPressedThisFrame)
                    {
                        if (activeSpecies == "Tiger") SwitchToDeer();
                        else SwitchToTiger();
                    }
                }
            }
            catch (System.Exception) { }
#endif
#if !ENABLE_INPUT_SYSTEM
            try
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                {
                    SwitchToTiger();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                {
                    SwitchToDeer();
                }
                else if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (activeSpecies == "Tiger") SwitchToDeer();
                    else SwitchToTiger();
                }
            }
            catch (System.Exception) { }
#endif
        }

        public void SwitchToTiger()
        {
            FindAnimals();
            activeSpecies = "Tiger";

            if (deerObject != null) deerObject.SetActive(false);
            if (tigerObject != null)
            {
                tigerObject.SetActive(true);
                var lac = tigerObject.GetComponent<LandAnimalController>();
                if (lac != null) lac.enabled = true;
            }

            if (cameraFollow == null)
            {
                var cam = Camera.main;
                if (cam != null) cameraFollow = cam.GetComponent<CameraFollow>();
            }

            if (cameraFollow != null && tigerObject != null)
            {
                cameraFollow.target = tigerObject.transform;
                cameraFollow.ApplySpeciesPreset("Tiger");
            }

            if (gameManager != null && tigerObject != null)
            {
                gameManager.currentSpecies = "tiger";
                gameManager.playerObject = tigerObject;
            }

            Debug.Log("<color=orange>[AnimalSwitcher] Switched active animal to Royal Bengal Tiger</color>");
        }

        public void SwitchToDeer()
        {
            FindAnimals();
            activeSpecies = "Deer";

            if (tigerObject != null) tigerObject.SetActive(false);
            if (deerObject != null)
            {
                deerObject.SetActive(true);
                var lac = deerObject.GetComponent<LandAnimalController>();
                if (lac != null) lac.enabled = true;
            }

            if (cameraFollow == null)
            {
                var cam = Camera.main;
                if (cam != null) cameraFollow = cam.GetComponent<CameraFollow>();
            }

            if (cameraFollow != null && deerObject != null)
            {
                cameraFollow.target = deerObject.transform;
                cameraFollow.ApplySpeciesPreset("Deer");
            }

            if (gameManager != null && deerObject != null)
            {
                gameManager.currentSpecies = "deer";
                gameManager.playerObject = deerObject;
            }

            Debug.Log("<color=yellow>[AnimalSwitcher] Switched active animal to Chitra Spotted Deer</color>");
        }
    }
}
