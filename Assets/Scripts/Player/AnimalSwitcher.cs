using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Seamless animal species switcher for the Sundarbans Guardian ecosystem:
    /// - Royal Bengal Tiger (Apex Predator - Land)
    /// - Chitra Spotted Deer (Agile Herbivore - Land)
    /// - Gangetic Dolphin (River Apex Mammal - Aquatic)
    /// - Hilsha Shad (Migratory Fish - River / Marine)
    /// Keyboard shortcuts: '1' Tiger, '2' Deer, '3' Dolphin, '4' Hilsha, 'Tab' cycles next species.
    /// Automatically updates CameraFollow target, species preset, and GameManager.
    /// </summary>
    public class AnimalSwitcher : MonoBehaviour
    {
        [Header("Animal References")]
        public GameObject tigerObject;
        public GameObject deerObject;
        public GameObject dolphinObject;
        public GameObject hilshaObject;

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

        public void FindAnimals()
        {
            if (tigerObject == null) tigerObject = GameObject.Find("Tiger");
            if (deerObject == null) deerObject = GameObject.Find("Deer");
            if (dolphinObject == null) dolphinObject = GameObject.Find("Dolphin");
            if (hilshaObject == null) hilshaObject = GameObject.Find("Hilsha");

            if (tigerObject == null || deerObject == null || dolphinObject == null || hilshaObject == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var go in all)
                {
                    if (go.hideFlags != HideFlags.None || !go.scene.isLoaded) continue;
                    if (tigerObject == null && go.name == "Tiger") tigerObject = go;
                    if (deerObject == null && go.name == "Deer") deerObject = go;
                    if (dolphinObject == null && go.name == "Dolphin") dolphinObject = go;
                    if (hilshaObject == null && go.name == "Hilsha") hilshaObject = go;
                }
            }
        }

        private void Start()
        {
            FindAnimals();

            if (gameManager != null && !string.IsNullOrEmpty(gameManager.currentSpecies))
            {
                string sp = gameManager.currentSpecies.ToLower();
                if (sp.Contains("deer")) { SwitchToDeer(); return; }
                if (sp.Contains("dolphin")) { SwitchToDolphin(); return; }
                if (sp.Contains("hilsha")) { SwitchToHilsha(); return; }
            }

            SwitchToTiger();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) SwitchToTiger();
                    else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) SwitchToDeer();
                    else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) SwitchToDolphin();
                    else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) SwitchToHilsha();
                    else if (kb.tabKey.wasPressedThisFrame) CycleNextSpecies();
                }
            }
            catch (System.Exception) { }
#endif
#if !ENABLE_INPUT_SYSTEM
            try
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SwitchToTiger();
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SwitchToDeer();
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SwitchToDolphin();
                else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SwitchToHilsha();
                else if (Input.GetKeyDown(KeyCode.Tab)) CycleNextSpecies();
            }
            catch (System.Exception) { }
#endif
        }

        public void CycleNextSpecies()
        {
            if (activeSpecies == "Tiger") SwitchToDeer();
            else if (activeSpecies == "Deer") SwitchToDolphin();
            else if (activeSpecies == "Dolphin") SwitchToHilsha();
            else SwitchToTiger();
        }

        private void SetOnlyActive(GameObject activeGo)
        {
            if (tigerObject != null) tigerObject.SetActive(tigerObject == activeGo);
            if (deerObject != null) deerObject.SetActive(deerObject == activeGo);
            if (dolphinObject != null) dolphinObject.SetActive(dolphinObject == activeGo);
            if (hilshaObject != null) hilshaObject.SetActive(hilshaObject == activeGo);
        }

        public void SwitchToTiger()
        {
            if (CompetitiveRoundManager.Instance != null)
            {
                CompetitiveRoundManager.Instance.LaunchRound(0);
                activeSpecies = "Tiger";
                return;
            }

            FindAnimals();
            activeSpecies = "Tiger";
            SetOnlyActive(tigerObject);

            if (tigerObject != null)
            {
                var lac = tigerObject.GetComponent<LandAnimalController>();
                if (lac != null) lac.enabled = true;
                UpdateCameraAndManager(tigerObject, "Tiger", "tiger");
            }

            Debug.Log("<color=orange>[AnimalSwitcher] Switched active animal to Royal Bengal Tiger</color>");
        }

        public void SwitchToDeer()
        {
            if (CompetitiveRoundManager.Instance != null)
            {
                CompetitiveRoundManager.Instance.LaunchRound(1);
                activeSpecies = "Deer";
                return;
            }

            FindAnimals();
            activeSpecies = "Deer";
            SetOnlyActive(deerObject);

            if (deerObject != null)
            {
                var lac = deerObject.GetComponent<LandAnimalController>();
                if (lac != null) lac.enabled = true;
                UpdateCameraAndManager(deerObject, "Deer", "deer");
            }

            Debug.Log("<color=yellow>[AnimalSwitcher] Switched active animal to Chitra Spotted Deer</color>");
        }

        public void SwitchToDolphin()
        {
            if (CompetitiveRoundManager.Instance != null)
            {
                CompetitiveRoundManager.Instance.LaunchRound(2);
                activeSpecies = "Dolphin";
                return;
            }

            FindAnimals();
            activeSpecies = "Dolphin";
            SetOnlyActive(dolphinObject);

            if (dolphinObject != null)
            {
                var dsc = dolphinObject.GetComponent<DolphinSwimController>();
                if (dsc != null) dsc.enabled = true;
                UpdateCameraAndManager(dolphinObject, "Dolphin", "gangetic_dolphin");
            }

            Debug.Log("<color=cyan>[AnimalSwitcher] Switched active animal to Gangetic Dolphin</color>");
        }

        public void SwitchToHilsha()
        {
            if (CompetitiveRoundManager.Instance != null)
            {
                CompetitiveRoundManager.Instance.LaunchRound(3);
                activeSpecies = "Hilsha";
                return;
            }

            FindAnimals();
            activeSpecies = "Hilsha";
            SetOnlyActive(hilshaObject);

            if (hilshaObject != null)
            {
                var hmc = hilshaObject.GetComponent<HilshaMigrationController>();
                if (hmc != null) hmc.enabled = true;
                UpdateCameraAndManager(hilshaObject, "Hilsha", "hilsha");
            }

            Debug.Log("<color=silver>[AnimalSwitcher] Switched active animal to Hilsha Shad</color>");
        }

        private void UpdateCameraAndManager(GameObject animalGo, string speciesPreset, string managerSpeciesId)
        {
            if (cameraFollow == null)
            {
                var cam = Camera.main;
                if (cam != null) cameraFollow = cam.GetComponent<CameraFollow>();
            }

            if (cameraFollow != null && animalGo != null)
            {
                cameraFollow.target = animalGo.transform;
                cameraFollow.ApplySpeciesPreset(speciesPreset);
            }

            if (gameManager != null && animalGo != null)
            {
                gameManager.currentSpecies = managerSpeciesId;
                gameManager.playerObject = animalGo;
            }
        }
    }
}
