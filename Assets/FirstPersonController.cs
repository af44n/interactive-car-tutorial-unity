using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float jumpHeight = 1.0f;
    public float gravity = -35f;
    public float mouseSensitivity = 0.1f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    public Transform playerCamera;
    private float xRotation = 0f;

    [Header("Vehicle Interaction")]
    public float interactionDistance = 4.0f;
    private CarController nearbyCar;
    private CarController currentCar;
    public bool isDriving = false;

    [Header("Item Pickup")]
    public float pickupDistance = 3.5f;
    private PickupItem heldItem = null;
    private PickupItem nearbyItem = null;
    private Transform holdPoint;

    [Header("UI Canvas Elements")]
    private GameObject hudCanvasObj;
    private GameObject menuCanvasObj;
    private Slider sensitivitySlider;
    private Text sensitivityValueText;
    private Text promptText;
    private bool isMenuOpen = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0, 0.9f, 0);
        controller.stepOffset = 0.3f;

        if (playerCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null) playerCamera = mainCam.transform;
        }

        if (playerCamera != null)
        {
            playerCamera.SetParent(transform);
            playerCamera.localPosition = new Vector3(0, 1.6f, 0);
            playerCamera.localRotation = Quaternion.identity;
        }

        // Create hold point in front of camera
        GameObject hp = new GameObject("HoldPoint");
        hp.transform.SetParent(playerCamera);
        hp.transform.localPosition = new Vector3(0f, -0.2f, 0.6f);
        hp.transform.localRotation = Quaternion.identity;
        holdPoint = hp.transform;

        CreateHUDAndMenuUI();
        SetMenuState(false);
    }

    void Update()
    {
        // Toggle Settings Menu
        if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame))
        {
            SetMenuState(!isMenuOpen);
        }

        if (isMenuOpen) return;

        // Vehicle Entry / Exit Toggle with F Key
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (isDriving)
            {
                ExitCar();
            }
            else if (nearbyCar != null)
            {
                EnterCar(nearbyCar);
            }
        }

        // Item Pickup / Drop with E Key
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            var tut = FindObjectOfType<TireChangeTutorial>();
            bool tutorialConsumingE = (tut != null && tut.IsPlayerNearPlacementMarker(transform.position));

            if (tutorialConsumingE && heldItem != null)
            {
                // If tutorial uses E to place/use held item, clear heldItem link so tutorial handles placement
                heldItem = null;
            }
            else if (nearbyItem != null)
            {
                // If already holding another item, drop old item first
                if (heldItem != null)
                {
                    heldItem.Drop(playerCamera.forward, 1.5f);
                    heldItem = null;
                }
                // Pick up the new nearby item
                nearbyItem.PickUp(holdPoint);
                heldItem = nearbyItem;
                nearbyItem = null;
            }
            else if (heldItem != null)
            {
                // Drop held item when not near any interactive target
                heldItem.Drop(playerCamera.forward, 2f);
                heldItem = null;
            }
        }

        // Handle driving state & prompt
        if (isDriving)
        {
            if (promptText != null)
            {
                promptText.gameObject.SetActive(true);
                promptText.text = "PRESS [F] TO EXIT VEHICLE";
            }
            return;
        }

        // Check for nearby car and items when walking
        CheckNearbyCar();
        CheckNearbyItems();

        // Mouse look
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

            if (!float.IsNaN(mouseDelta.x) && !float.IsNaN(mouseDelta.y) && !float.IsNaN(xRotation))
            {
                xRotation -= mouseDelta.y;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                if (playerCamera != null)
                {
                    playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                }
                transform.Rotate(Vector3.up * mouseDelta.x);
            }
        }

        // Check ground state
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = 0f;
        float z = 0f;
        bool isSprinting = false;
        bool isJumping = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.wKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;

            isSprinting = isGrounded && Keyboard.current.leftShiftKey.isPressed;
            isJumping = Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        Vector3 move = transform.right * x + transform.forward * z;
        if (move.magnitude > 1f) move.Normalize();

        float speed = isSprinting ? sprintSpeed : walkSpeed;
        controller.Move(move * speed * Time.deltaTime);

        // Jump
        if (isJumping && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void CheckNearbyCar()
    {
        CarController car = FindObjectOfType<CarController>();
        var tut = FindObjectOfType<TireChangeTutorial>();
        bool inTutorial = (tut != null && tut.currentStep != TireChangeTutorial.TutorialStep.Completed);

        if (car != null)
        {
            float dist = Vector3.Distance(transform.position, car.transform.position);
            if (dist <= interactionDistance)
            {
                nearbyCar = car;
                if (promptText != null && !inTutorial)
                {
                    promptText.gameObject.SetActive(true);
                    promptText.text = "PRESS [F] TO ENTER VEHICLE";
                }
                return;
            }
        }

        nearbyCar = null;
        if (promptText != null && !isDriving && !inTutorial)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    private void CheckNearbyItems()
    {
        // If holding something, update prompt and skip scanning
        if (heldItem != null)
        {
            if (promptText != null)
            {
                promptText.gameObject.SetActive(true);
                promptText.text = $"[E] DROP {heldItem.itemName.ToUpper()}";
            }
            nearbyItem = null;
            return;
        }

        PickupItem closest = null;
        float closestDist = pickupDistance;

        foreach (var item in FindObjectsByType<PickupItem>(FindObjectsSortMode.None))
        {
            if (item.isHeld || item.isPlaced) continue;
            float dist = Vector3.Distance(transform.position, item.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = item;
            }
        }

        nearbyItem = closest;

        if (nearbyItem != null)
        {
            if (promptText != null)
            {
                promptText.gameObject.SetActive(true);
                promptText.text = $"[E] PICK UP {nearbyItem.itemName.ToUpper()}";
            }
        }
        else if (nearbyCar == null)
        {
            if (promptText != null)
                promptText.gameObject.SetActive(false);
        }
    }

    private void EnterCar(CarController car)
    {
        isDriving = true;
        currentCar = car;

        if (playerCamera != null)
        {
            playerCamera.SetParent(null);
        }

        controller.enabled = false;
        
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        car.EnterVehicle(playerCamera);
    }

    private void ExitCar()
    {
        if (currentCar == null) return;

        currentCar.ExitVehicle();

        Vector3 exitPos = currentCar.transform.position - currentCar.transform.right * 2.2f + Vector3.up * 0.2f;
        transform.position = exitPos;

        controller.enabled = true;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = true;
        }

        if (playerCamera != null)
        {
            playerCamera.SetParent(transform);
            playerCamera.localPosition = new Vector3(0, 1.6f, 0);
            playerCamera.localRotation = Quaternion.identity;
        }

        isDriving = false;
        currentCar = null;
    }

    public void SetMenuState(bool open)
    {
        isMenuOpen = open;
        if (menuCanvasObj != null) menuCanvasObj.SetActive(open);

        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    private void CreateHUDAndMenuUI()
    {
        EventSystem es = FindObjectOfType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }
        else
        {
            StandaloneInputModule oldModule = es.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Destroy(oldModule);
            }
            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        // --- ALWAYS ACTIVE HUD CANVAS FOR PROMPTS ---
        hudCanvasObj = new GameObject("HUDCanvas");
        Canvas hudCanvas = hudCanvasObj.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvasObj.AddComponent<CanvasScaler>();
        hudCanvasObj.AddComponent<GraphicRaycaster>();

        // Prompt Panel Background Box (Centered near bottom)
        GameObject promptBox = new GameObject("PromptBox");
        promptBox.transform.SetParent(hudCanvasObj.transform, false);
        Image promptBg = promptBox.AddComponent<Image>();
        promptBg.color = new Color(0, 0, 0, 0.7f);
        RectTransform promptBoxRect = promptBox.GetComponent<RectTransform>();
        promptBoxRect.anchoredPosition = new Vector2(0, -220);
        promptBoxRect.sizeDelta = new Vector2(400, 50);

        // Prompt Text
        GameObject promptObj = new GameObject("PromptText");
        promptObj.transform.SetParent(promptBox.transform, false);
        promptText = promptObj.AddComponent<Text>();
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 20;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = Color.yellow;
        promptText.text = "";
        RectTransform promptTextRect = promptObj.GetComponent<RectTransform>();
        promptTextRect.anchorMin = Vector2.zero;
        promptTextRect.anchorMax = Vector2.one;
        promptTextRect.sizeDelta = Vector2.zero;

        promptText.gameObject.SetActive(false);


        // --- PAUSE / SETTINGS CANVAS (TOGGLED) ---
        menuCanvasObj = new GameObject("PauseSettingsCanvas");
        Canvas canvas = menuCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvasObj.AddComponent<CanvasScaler>();
        menuCanvasObj.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(menuCanvasObj.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.85f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400, 250);
        panelRect.anchoredPosition = Vector2.zero;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.text = "SETTINGS";
        titleText.fontSize = 24;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 80);
        titleRect.sizeDelta = new Vector2(350, 40);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(panel.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.text = "Mouse Sensitivity";
        labelText.fontSize = 16;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = Color.white;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(-60, 20);
        labelRect.sizeDelta = new Vector2(200, 30);

        GameObject valObj = new GameObject("ValText");
        valObj.transform.SetParent(panel.transform, false);
        sensitivityValueText = valObj.AddComponent<Text>();
        sensitivityValueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sensitivityValueText.text = mouseSensitivity.ToString("F2");
        sensitivityValueText.fontSize = 16;
        sensitivityValueText.alignment = TextAnchor.MiddleRight;
        sensitivityValueText.color = Color.yellow;
        RectTransform valRect = valObj.GetComponent<RectTransform>();
        valRect.anchoredPosition = new Vector2(100, 20);
        valRect.sizeDelta = new Vector2(80, 30);

        GameObject sliderObj = new GameObject("SensitivitySlider");
        sliderObj.transform.SetParent(panel.transform, false);
        sensitivitySlider = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(0, -20);
        sliderRect.sizeDelta = new Vector2(300, 20);

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero;
        faRect.anchorMax = Vector2.one;
        faRect.sizeDelta = new Vector2(-10, 0);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 1f, 1f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform haRect = handleArea.AddComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.sizeDelta = new Vector2(-20, 0);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);

        sensitivitySlider.targetGraphic = handleImg;
        sensitivitySlider.fillRect = fillRect;
        sensitivitySlider.handleRect = handleRect;
        sensitivitySlider.minValue = 0.01f;
        sensitivitySlider.maxValue = 0.5f;
        sensitivitySlider.value = mouseSensitivity;

        sensitivitySlider.onValueChanged.AddListener((val) => {
            mouseSensitivity = val;
            if (sensitivityValueText != null) sensitivityValueText.text = val.ToString("F2");
        });

        GameObject btnObj = new GameObject("ResumeButton");
        btnObj.transform.SetParent(panel.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -75);
        btnRect.sizeDelta = new Vector2(140, 35);

        btn.onClick.AddListener(() => {
            SetMenuState(false);
        });

        GameObject btnTxtObj = new GameObject("BtnText");
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        Text btnText = btnTxtObj.AddComponent<Text>();
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.text = "RESUME";
        btnText.fontSize = 16;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        RectTransform btnTxtRect = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero;
        btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.sizeDelta = Vector2.zero;
    }
}
