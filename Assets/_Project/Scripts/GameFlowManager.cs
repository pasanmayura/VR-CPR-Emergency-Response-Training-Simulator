using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameFlowManager : MonoBehaviour
{
    public enum TrainingStage
    {
        Start,
        Tutorial,
        Emergency,
        CheckResponsiveness,
        CallForHelp,
        CPRPosition,
        CPR,
        AED,
        Assessment,
        Complete
    }

    public TrainingStage CurrentStage { get; private set; } = TrainingStage.Start;

    [Header("Start button")]
    [Min(0.5f)]
    public float startButtonDistance = 1.5f;

    [Tooltip("XR Camera Offset used as the spatial position reference for the button.")]
    public Transform cameraOffset;

    public UnityEvent onTrainingStarted = new UnityEvent();

    private Canvas startCanvas;

    private void Start()
    {
        CreateStartButton();
    }

    private void Update()
    {
        if (CurrentStage != TrainingStage.Start)
            return;

        bool startPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            startPressed =
                Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        startPressed =
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter);
#endif

        if (startPressed)
            StartTraining();
    }

    public void SetStage(TrainingStage newStage)
    {
        CurrentStage = newStage;

        Debug.Log("Training Stage: " + CurrentStage);
    }

    public void StartTraining()
    {
        if (CurrentStage != TrainingStage.Start)
            return;

        SetStage(TrainingStage.Tutorial);

        if (startCanvas != null)
            startCanvas.gameObject.SetActive(false);

        onTrainingStarted.Invoke();
    }

    private void CreateStartButton()
    {
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError(
                "GameFlowManager: a MainCamera is required to place the start button.",
                this);
            return;
        }

        GameObject canvasObject = new GameObject("Start Spatial UI");
        canvasObject.transform.SetParent(transform, false);

        startCanvas = canvasObject.AddComponent<Canvas>();
        startCanvas.renderMode = RenderMode.WorldSpace;
        startCanvas.worldCamera = playerCamera;
        startCanvas.sortingOrder = 10;

        EnsureXRUIInputModule();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        RectTransform canvasTransform = startCanvas.GetComponent<RectTransform>();
        canvasTransform.sizeDelta = new Vector2(800f, 400f);
        canvasTransform.localScale = Vector3.one * 0.0015f;

        Transform cameraTransform = playerCamera.transform;
        Transform positionReference = ResolveCameraOffset(cameraTransform);

        canvasTransform.position =
            positionReference.position +
            cameraTransform.forward * Mathf.Max(0.5f, startButtonDistance);
        canvasTransform.rotation =
            Quaternion.LookRotation(
                canvasTransform.position - positionReference.position);

        GameObject buttonObject = new GameObject("Start Button");
        buttonObject.transform.SetParent(canvasTransform, false);

        RectTransform buttonTransform =
            buttonObject.AddComponent<RectTransform>();
        buttonTransform.sizeDelta = new Vector2(520f, 160f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.1f, 0.55f, 0.9f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(StartTraining);

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(buttonTransform, false);

        RectTransform textTransform =
            textObject.AddComponent<RectTransform>();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.offsetMin = Vector2.zero;
        textTransform.offsetMax = Vector2.zero;

        Text label = textObject.AddComponent<Text>();
        label.text = "START TRAINING";
        label.alignment = TextAnchor.MiddleCenter;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 48;
        label.fontStyle = FontStyle.Bold;
        label.color = Color.white;
    }

    private void EnsureXRUIInputModule()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<XRUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<XRUIInputModule>();
    }

    private Transform ResolveCameraOffset(Transform cameraTransform)
    {
        if (cameraOffset != null)
            return cameraOffset;

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == "Camera Offset")
            {
                cameraOffset = candidate;
                return candidate;
            }
        }

        Debug.LogWarning(
            "GameFlowManager: Camera Offset was not found. " +
            "Using Main Camera position for the start button.",
            this);

        return cameraTransform;
    }

    private void OnDestroy()
    {
        if (startCanvas != null)
            Destroy(startCanvas.gameObject);
    }
}