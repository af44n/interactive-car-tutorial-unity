using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TireChangeTutorial : MonoBehaviour
{
    public enum TutorialStep
    {
        PickupJack = 0,
        PlaceJackUnderCar = 1,
        PickupWrench = 2,
        UnscrewLugNuts = 3,
        RemoveFlatTire = 4,
        PickupSpareTire = 5,
        MountSpareTire = 6,
        TightenNutsAndLowerJack = 7,
        Completed = 8
    }

    [Header("Current Progress")]
    public TutorialStep currentStep = TutorialStep.PickupJack;

    [Header("Car Jack Placement Tweaks (Adjust in Inspector!)")]
    public Vector3 jackLocalPositionOffset = new Vector3(-1.096f, 0.07f, 0.914f);
    public Vector3 jackRotationOffsetEuler = new Vector3(-84.592f, 90f, -84.963f);

    [Header("Scene Object References")]
    public GameObject carJackObj;
    public GameObject lugWrenchObj;
    public GameObject lugNutObj;
    public GameObject spareTireObj;
    public GameObject carObj;

    [Header("Car Markers")]
    public Transform jackPointMarker;
    public Transform wheelHubMarker;
    public GameObject detachedFlatTireObj;

    [Header("UI & Pointer")]
    private GameObject tutorialCanvasObj;
    private Text stepCounterText;
    private Text titleText;
    private Text instructionText;
    private GameObject arrowPointerObj;
    public GameObject ghostJackObj;

    private FirstPersonController playerController;
    private bool isAnimating = false;

    void Awake()
    {
        currentStep = TutorialStep.PickupJack;
    }

    void Start()
    {
        playerController = FindObjectOfType<FirstPersonController>();
        SetupSceneObjectsAndMarkers();
        CreateTutorialUI();
        Create3DArrowPointer();
        CreateGhostJackPreview();
        UpdateStepUI();
    }

    void OnValidate()
    {
        if (carObj == null) carObj = GameObject.Find("RMCar26");
        if (jackPointMarker == null)
        {
            var jp = GameObject.Find("JackPointMarker");
            if (jp != null) jackPointMarker = jp.transform;
        }

        if (carObj != null && jackPointMarker != null)
        {
            jackPointMarker.localPosition = jackLocalPositionOffset;
            jackPointMarker.localRotation = Quaternion.Euler(jackRotationOffsetEuler);

            if (ghostJackObj == null) ghostJackObj = GameObject.Find("GhostJackPreview");
            if (ghostJackObj != null)
            {
                ghostJackObj.transform.position = jackPointMarker.position;
                ghostJackObj.transform.rotation = jackPointMarker.rotation;
                ghostJackObj.SetActive(true);
            }

            if (carJackObj == null) carJackObj = GameObject.Find("Car Jack");
            if (carJackObj != null)
            {
                PickupItem pi = carJackObj.GetComponent<PickupItem>();
                if (pi != null && pi.isPlaced)
                {
                    carJackObj.transform.position = jackPointMarker.position;
                    carJackObj.transform.rotation = jackPointMarker.rotation;
                }
            }
        }
    }

    void SetupSceneObjectsAndMarkers()
    {
        if (carObj == null) carObj = GameObject.Find("RMCar26");
        if (carJackObj == null) carJackObj = GameObject.Find("Car Jack");
        if (lugWrenchObj == null) lugWrenchObj = GameObject.Find("Lug Wrench");
        if (lugNutObj == null) lugNutObj = GameObject.Find("Lug Nut");
        if (spareTireObj == null) spareTireObj = GameObject.Find("Spare Tire");

        if (carObj != null)
        {
            carObj.transform.position = Vector3.zero;
            carObj.transform.rotation = Quaternion.identity;

            if (jackPointMarker == null)
            {
                GameObject jp = GameObject.Find("JackPointMarker");
                if (jp == null)
                {
                    jp = new GameObject("JackPointMarker");
                    jp.transform.SetParent(carObj.transform);
                }
                jackPointMarker = jp.transform;
            }

            jackPointMarker.localPosition = jackLocalPositionOffset;
            jackPointMarker.localRotation = Quaternion.Euler(jackRotationOffsetEuler);

            Transform flWheelTransform = carObj.transform.Find("RMCar26_Main/RMCar26_WheelFrontLeft");
            if (flWheelTransform != null)
            {
                wheelHubMarker = flWheelTransform;
            }
            else
            {
                wheelHubMarker = carObj.transform;
            }
        }

        if (spareTireObj != null)
        {
            spareTireObj.transform.position = new Vector3(-3.8f, 0.15f, -0.8f);
            spareTireObj.transform.rotation = Quaternion.Euler(-90, 0, 0);
        }
    }

    void Update()
    {
        if (isAnimating) return;
        CheckProgressInput();
        UpdateArrowPointer();
    }

    void CheckProgressInput()
    {
        switch (currentStep)
        {
            case TutorialStep.PickupJack:
                if (carJackObj != null)
                {
                    PickupItem pi = carJackObj.GetComponent<PickupItem>();
                    if (pi != null && pi.isHeld)
                    {
                        AdvanceStep(TutorialStep.PlaceJackUnderCar);
                    }
                }
                break;

            case TutorialStep.PlaceJackUnderCar:
                if (jackPointMarker != null && playerController != null && !playerController.isDriving)
                {
                    float dist = Vector3.Distance(playerController.transform.position, jackPointMarker.position);
                    if (dist <= 2.8f && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        PickupItem pi = carJackObj != null ? carJackObj.GetComponent<PickupItem>() : null;
                        if (pi != null)
                        {
                            pi.Place(jackPointMarker.position, jackPointMarker.rotation);
                        }

                        StartCoroutine(AnimatePlaceJackAndLiftCar());
                    }
                }
                break;

            case TutorialStep.PickupWrench:
                if (lugWrenchObj != null)
                {
                    PickupItem pi = lugWrenchObj.GetComponent<PickupItem>();
                    if (pi != null && pi.isHeld)
                    {
                        AdvanceStep(TutorialStep.UnscrewLugNuts);
                    }
                }
                break;

            case TutorialStep.UnscrewLugNuts:
                if (wheelHubMarker != null && playerController != null && !playerController.isDriving)
                {
                    float dist = Vector3.Distance(playerController.transform.position, wheelHubMarker.position);
                    if (dist <= 2.8f && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        StartCoroutine(AnimateUnscrewLugNuts());
                    }
                }
                break;

            case TutorialStep.RemoveFlatTire:
                if (wheelHubMarker != null && playerController != null && !playerController.isDriving)
                {
                    float dist = Vector3.Distance(playerController.transform.position, wheelHubMarker.position);
                    if (dist <= 2.8f && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        StartCoroutine(AnimateRemoveFlatTire());
                    }
                }
                break;

            case TutorialStep.PickupSpareTire:
                if (spareTireObj != null)
                {
                    PickupItem pi = spareTireObj.GetComponent<PickupItem>();
                    if (pi != null && pi.isHeld)
                    {
                        AdvanceStep(TutorialStep.MountSpareTire);
                    }
                }
                break;

            case TutorialStep.MountSpareTire:
                if (wheelHubMarker != null && playerController != null && !playerController.isDriving)
                {
                    float dist = Vector3.Distance(playerController.transform.position, wheelHubMarker.position);
                    if (dist <= 2.8f && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        PickupItem pi = spareTireObj != null ? spareTireObj.GetComponent<PickupItem>() : null;
                        if (pi != null && pi.isHeld)
                        {
                            pi.Drop(Vector3.zero, 0f);
                        }
                        StartCoroutine(AnimateMountSpareTire());
                    }
                }
                break;

            case TutorialStep.TightenNutsAndLowerJack:
                if (jackPointMarker != null && playerController != null && !playerController.isDriving)
                {
                    float dist = Vector3.Distance(playerController.transform.position, jackPointMarker.position);
                    if (dist <= 2.8f && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        StartCoroutine(AnimateLowerCarAndFinish());
                    }
                }
                break;
        }
    }

    public bool IsPlayerNearPlacementMarker(Vector3 playerPos)
    {
        if (currentStep == TutorialStep.PlaceJackUnderCar && jackPointMarker != null)
        {
            return Vector3.Distance(playerPos, jackPointMarker.position) <= 2.8f;
        }
        if ((currentStep == TutorialStep.UnscrewLugNuts || currentStep == TutorialStep.RemoveFlatTire || currentStep == TutorialStep.MountSpareTire) && wheelHubMarker != null)
        {
            return Vector3.Distance(playerPos, wheelHubMarker.position) <= 2.8f;
        }
        if (currentStep == TutorialStep.TightenNutsAndLowerJack && jackPointMarker != null)
        {
            return Vector3.Distance(playerPos, jackPointMarker.position) <= 2.8f;
        }
        return false;
    }

    IEnumerator AnimatePlaceJackAndLiftCar()
    {
        isAnimating = true;
        if (ghostJackObj != null) ghostJackObj.SetActive(false);

        if (carJackObj != null && jackPointMarker != null)
        {
            carJackObj.transform.position = jackPointMarker.position;
            carJackObj.transform.rotation = jackPointMarker.rotation;
            PickupItem pi = carJackObj.GetComponent<PickupItem>();
            if (pi != null) pi.isPlaced = true;
        }

        // Lift car smoothly
        if (carObj != null)
        {
            Vector3 startPos = Vector3.zero;
            Vector3 targetPos = startPos + Vector3.up * 0.28f;
            Quaternion startRot = Quaternion.identity;
            Quaternion targetRot = startRot * Quaternion.Euler(0, 0, -3.5f);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                carObj.transform.position = Vector3.Lerp(startPos, targetPos, t);
                carObj.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
        }

        isAnimating = false;
        AdvanceStep(TutorialStep.PickupWrench);
    }

    IEnumerator AnimateUnscrewLugNuts()
    {
        isAnimating = true;

        // Stow/drop lug wrench onto floor so player's hands become completely free
        if (lugWrenchObj != null)
        {
            PickupItem pi = lugWrenchObj.GetComponent<PickupItem>();
            if (pi != null)
            {
                pi.Place(new Vector3(-2.2f, 0.05f, 1.2f), Quaternion.identity);
            }
        }

        if (lugNutObj != null && wheelHubMarker != null)
        {
            lugNutObj.transform.position = wheelHubMarker.position + carObj.transform.forward * 0.3f - Vector3.up * 0.35f;
            lugNutObj.transform.rotation = Quaternion.identity;
        }

        yield return new WaitForSeconds(0.5f);

        isAnimating = false;
        AdvanceStep(TutorialStep.RemoveFlatTire);
    }

    IEnumerator AnimateRemoveFlatTire()
    {
        isAnimating = true;

        Transform flWheelMesh = carObj.transform.Find("RMCar26_Main/RMCar26_WheelFrontLeft/RMCar26WheelFrontLeft");
        if (flWheelMesh != null)
        {
            flWheelMesh.gameObject.SetActive(false);

            if (detachedFlatTireObj == null)
            {
                detachedFlatTireObj = Instantiate(flWheelMesh.gameObject, wheelHubMarker.position - carObj.transform.right * 0.8f - Vector3.up * 0.25f, Quaternion.Euler(-90, 0, 0));
                detachedFlatTireObj.name = "Flat Tire (Removed)";
                detachedFlatTireObj.SetActive(true);
            }
        }

        yield return new WaitForSeconds(0.5f);

        isAnimating = false;
        AdvanceStep(TutorialStep.PickupSpareTire);
    }

    IEnumerator AnimateMountSpareTire()
    {
        isAnimating = true;

        if (spareTireObj != null)
        {
            Destroy(spareTireObj);
        }

        Transform flWheelMesh = carObj.transform.Find("RMCar26_Main/RMCar26_WheelFrontLeft/RMCar26WheelFrontLeft");
        if (flWheelMesh != null)
        {
            flWheelMesh.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(0.5f);

        isAnimating = false;
        AdvanceStep(TutorialStep.TightenNutsAndLowerJack);
    }

    IEnumerator AnimateLowerCarAndFinish()
    {
        isAnimating = true;

        if (lugNutObj != null && wheelHubMarker != null)
        {
            lugNutObj.transform.position = wheelHubMarker.position;
        }

        if (carObj != null)
        {
            Vector3 startPos = carObj.transform.position;
            Vector3 targetPos = new Vector3(startPos.x, 0f, startPos.z);
            Quaternion startRot = carObj.transform.rotation;
            Quaternion targetRot = Quaternion.identity;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                carObj.transform.position = Vector3.Lerp(startPos, targetPos, t);
                carObj.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
            carObj.transform.position = targetPos;
            carObj.transform.rotation = targetRot;
        }

        if (carJackObj != null)
        {
            PickupItem pi = carJackObj.GetComponent<PickupItem>();
            if (pi != null) pi.isPlaced = false;
        }

        isAnimating = false;
        AdvanceStep(TutorialStep.Completed);
    }

    public void AdvanceStep(TutorialStep nextStep)
    {
        currentStep = nextStep;
        UpdateStepUI();
    }

    void UpdateStepUI()
    {
        if (instructionText == null || stepCounterText == null) return;

        if (currentStep == TutorialStep.Completed)
        {
            stepCounterText.text = "SUCCESS!";
            titleText.text = "TUTORIAL COMPLETED!";
            instructionText.text = "Awesome job! You successfully changed the car tire!";
            if (arrowPointerObj != null) arrowPointerObj.SetActive(false);
            if (ghostJackObj != null) ghostJackObj.SetActive(false);
        }
        else
        {
            int stepNum = ((int)currentStep) + 1;
            stepCounterText.text = $"STEP {stepNum} OF 8";

            switch (currentStep)
            {
                case TutorialStep.PickupJack:
                    titleText.text = "GET THE CAR JACK";
                    instructionText.text = "Walk to the Car Jack on the floor and press [E] to pick it up.";
                    if (ghostJackObj != null) ghostJackObj.SetActive(false);
                    break;

                case TutorialStep.PlaceJackUnderCar:
                    titleText.text = "POSITION THE CAR JACK";
                    instructionText.text = "Carry the Jack to the cyan ghost marker under the front-left side skirt and press [E].";
                    if (ghostJackObj != null) ghostJackObj.SetActive(true);
                    break;

                case TutorialStep.PickupWrench:
                    titleText.text = "GET THE LUG WRENCH";
                    instructionText.text = "Pick up the 4-Way Lug Wrench [E] from the floor.";
                    if (ghostJackObj != null) ghostJackObj.SetActive(false);
                    break;

                case TutorialStep.UnscrewLugNuts:
                    titleText.text = "UNSCREW LUG NUTS";
                    instructionText.text = "Approach the front-left wheel with your Lug Wrench and press [E] to unbolt the lug nuts.";
                    break;

                case TutorialStep.RemoveFlatTire:
                    titleText.text = "REMOVE FLAT TIRE";
                    instructionText.text = "Walk up to the front-left wheel and press [E] to take off the flat tire.";
                    break;

                case TutorialStep.PickupSpareTire:
                    titleText.text = "PICK UP SPARE TIRE";
                    instructionText.text = "Pick up the fresh Spare Tire [E] from the ground.";
                    break;

                case TutorialStep.MountSpareTire:
                    titleText.text = "MOUNT SPARE TIRE";
                    instructionText.text = "Bring the Spare Tire to the bare wheel hub and press [E] to mount it.";
                    break;

                case TutorialStep.TightenNutsAndLowerJack:
                    titleText.text = "TIGHTEN NUTS & LOWER JACK";
                    instructionText.text = "Press [E] at the front-left side skirt to tighten the lug nuts and lower the Car Jack.";
                    break;
            }
        }
    }

    void Create3DArrowPointer()
    {
        arrowPointerObj = new GameObject("Tutorial3DArrowPointer");
        
        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.transform.SetParent(arrowPointerObj.transform, false);
        cone.transform.localScale = new Vector3(0.12f, 0.25f, 0.12f);
        cone.transform.localRotation = Quaternion.Euler(180, 0, 0);

        Renderer r = cone.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            r.material.SetColor("_BaseColor", Color.yellow);
            r.material.SetColor("_EmissionColor", Color.yellow * 2.0f);
            r.material.EnableKeyword("_EMISSION");
        }

        Collider c = cone.GetComponent<Collider>();
        if (c != null) Destroy(c);
    }

    void CreateGhostJackPreview()
    {
        if (carJackObj == null || jackPointMarker == null) return;

        if (ghostJackObj == null)
        {
            ghostJackObj = Instantiate(carJackObj, jackPointMarker.position, jackPointMarker.rotation);
            ghostJackObj.name = "GhostJackPreview";

            var rb = ghostJackObj.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            var pi = ghostJackObj.GetComponent<PickupItem>();
            if (pi != null) Destroy(pi);

            foreach (var c in ghostJackObj.GetComponentsInChildren<Collider>())
            {
                Destroy(c);
            }

            Material ghostMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ghostMat.SetFloat("_Surface", 1);
            ghostMat.SetColor("_BaseColor", new Color(0.2f, 0.9f, 1.0f, 0.45f));
            ghostMat.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 1.0f) * 0.8f);
            ghostMat.EnableKeyword("_EMISSION");

            foreach (var r in ghostJackObj.GetComponentsInChildren<Renderer>())
            {
                r.material = ghostMat;
            }
        }

        ghostJackObj.transform.position = jackPointMarker.position;
        ghostJackObj.transform.rotation = jackPointMarker.rotation;
        ghostJackObj.SetActive(false);
    }

    void UpdateArrowPointer()
    {
        if (arrowPointerObj == null || currentStep == TutorialStep.Completed) return;

        Vector3 targetPos = Vector3.zero;

        switch (currentStep)
        {
            case TutorialStep.PickupJack:
                if (carJackObj != null) targetPos = carJackObj.transform.position + Vector3.up * 0.6f;
                break;

            case TutorialStep.PlaceJackUnderCar:
                if (jackPointMarker != null) targetPos = jackPointMarker.position + Vector3.up * 0.8f;
                break;

            case TutorialStep.PickupWrench:
                if (lugWrenchObj != null) targetPos = lugWrenchObj.transform.position + Vector3.up * 0.6f;
                break;

            case TutorialStep.UnscrewLugNuts:
            case TutorialStep.RemoveFlatTire:
            case TutorialStep.MountSpareTire:
                if (wheelHubMarker != null) targetPos = wheelHubMarker.position + Vector3.up * 0.8f;
                break;

            case TutorialStep.PickupSpareTire:
                if (spareTireObj != null) targetPos = spareTireObj.transform.position + Vector3.up * 0.6f;
                break;

            case TutorialStep.TightenNutsAndLowerJack:
                if (jackPointMarker != null) targetPos = jackPointMarker.position + Vector3.up * 0.8f;
                break;
        }

        if (targetPos != Vector3.zero)
        {
            arrowPointerObj.SetActive(true);
            float bob = Mathf.Sin(Time.time * 5f) * 0.12f;
            arrowPointerObj.transform.position = targetPos + Vector3.up * bob;
            arrowPointerObj.transform.Rotate(Vector3.up * 90f * Time.deltaTime);
        }
        else
        {
            arrowPointerObj.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (carObj != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 worldPos = carObj.transform.TransformPoint(jackLocalPositionOffset);
            Quaternion worldRot = carObj.transform.rotation * Quaternion.Euler(jackRotationOffsetEuler);
            Gizmos.matrix = Matrix4x4.TRS(worldPos, worldRot, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.5f, 0.2f, 0.4f));
        }
    }

    void CreateTutorialUI()
    {
        tutorialCanvasObj = new GameObject("TutorialCanvas");
        Canvas canvas = tutorialCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tutorialCanvasObj.AddComponent<CanvasScaler>();
        tutorialCanvasObj.AddComponent<GraphicRaycaster>();

        GameObject banner = new GameObject("TutorialBanner");
        banner.transform.SetParent(tutorialCanvasObj.transform, false);
        Image bgImg = banner.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        RectTransform bannerRect = banner.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 1f);
        bannerRect.anchorMax = new Vector2(0.5f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.anchoredPosition = new Vector2(0, -25);
        bannerRect.sizeDelta = new Vector2(700, 100);

        GameObject stepObj = new GameObject("StepCounterText");
        stepObj.transform.SetParent(banner.transform, false);
        stepCounterText = stepObj.AddComponent<Text>();
        stepCounterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stepCounterText.fontSize = 14;
        stepCounterText.fontStyle = FontStyle.Bold;
        stepCounterText.alignment = TextAnchor.MiddleCenter;
        stepCounterText.color = new Color(0.2f, 0.8f, 1f);
        RectTransform stepRect = stepObj.GetComponent<RectTransform>();
        stepRect.anchoredPosition = new Vector2(0, 32);
        stepRect.sizeDelta = new Vector2(300, 25);

        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(banner.transform, false);
        titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 22;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.yellow;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 8);
        titleRect.sizeDelta = new Vector2(650, 30);

        GameObject instObj = new GameObject("InstructionText");
        instObj.transform.SetParent(banner.transform, false);
        instructionText = instObj.AddComponent<Text>();
        instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionText.fontSize = 16;
        instructionText.alignment = TextAnchor.MiddleCenter;
        instructionText.color = Color.white;
        RectTransform instRect = instObj.GetComponent<RectTransform>();
        instRect.anchoredPosition = new Vector2(0, -22);
        instRect.sizeDelta = new Vector2(660, 35);
    }
}
