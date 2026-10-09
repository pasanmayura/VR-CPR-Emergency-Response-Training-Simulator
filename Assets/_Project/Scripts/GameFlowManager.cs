using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(AudioSource))]
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
    public bool TrainingStarted { get; private set; }

    [Header("Start button")]
    [Min(0.5f)]
    public float startButtonDistance = 1.5f;

    [Tooltip("XR Camera Offset used as the spatial position reference for the button.")]
    public Transform cameraOffset;

    [Header("Guide model")]
    [Tooltip("Optional override for the guide model. If empty, Resources/guider is loaded.")]
    public GameObject guiderPrefab;

    [Tooltip("Optional point inside the training room where the guide begins.")]
    public Transform guiderStartPoint;

    [Min(0.1f)]
    public float guiderMoveDuration = 1.5f;

    [Min(0.5f)]
    public float guiderDistanceFromPlayer = 1.5f;

    [Header("Guide audio")]
    [Range(0f, 1f)]
    public float guiderArrivalVolume = 1f;

    [Header("Guide departure")]
    [Min(0.1f)]
    public float guiderWalkDistance = 1f;

    [Min(0.1f)]
    public float guiderWalkSpeed = 0.5f;

    [Min(0.1f)]
    public float guiderRotationDuration = 0.5f;

    public UnityEvent onTrainingStarted = new UnityEvent();

    private Canvas startCanvas;
    private GameObject guiderInstance;
    private bool guiderWasInstantiated;
    private AudioSource audioSource;
    private AudioClip guiderArrivalClip;
    private LocomotionSystem locomotionSystem;
    private LocomotionProvider[] locomotionProviders;
    private bool[] locomotionProviderStates;
    private XROrigin xrOrigin;
    private Transform playerCameraTransform;
    private Vector3 lockedCameraOffsetLocalPosition;
    private Vector3 lockedCameraLocalPosition;
    private Vector3 lockedOriginPosition;
    private bool playerMovementEnabled;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin != null)
            lockedOriginPosition = xrOrigin.transform.position;

        playerCameraTransform = Camera.main != null
            ? Camera.main.transform
            : null;
        cameraOffset = ResolveCameraOffset(playerCameraTransform);
        if (cameraOffset != null)
            lockedCameraOffsetLocalPosition = cameraOffset.localPosition;
        if (playerCameraTransform != null)
            lockedCameraLocalPosition = playerCameraTransform.localPosition;

        locomotionSystem = FindObjectOfType<LocomotionSystem>();
        locomotionProviders = FindObjectsOfType<LocomotionProvider>(true);
        locomotionProviderStates = new bool[locomotionProviders.Length];
        for (int i = 0; i < locomotionProviders.Length; i++)
            locomotionProviderStates[i] = locomotionProviders[i].enabled;

        SetPlayerMovementEnabled(false);

        if (locomotionSystem == null)
        {
            Debug.LogError(
                "GameFlowManager: could not find the XR Locomotion System. " +
                "Player movement cannot be locked during the guide audio.",
                this);
        }

        guiderArrivalClip =
            Resources.Load<AudioClip>("guider/ControlGuider");

        if (guiderArrivalClip == null)
        {
            Debug.LogError(
                "GameFlowManager: the guide arrival audio could not be loaded. " +
                "Place ControlGuider.mp3 in Assets/_Project/Resources/guider.",
                this);
        }

        CreateStartButton();
    }

    private void Update()
    {
        if (CurrentStage != TrainingStage.Start)
            return;

        SetPlayerMovementEnabled(false);

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

        TrainingStarted = true;
        SetStage(TrainingStage.Tutorial);
        SetPlayerMovementEnabled(false);

        if (startCanvas != null)
            startCanvas.gameObject.SetActive(false);

        ShowGuider();
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

    private void ShowGuider()
    {
        if (guiderInstance != null)
            return;

        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError(
                "GameFlowManager: a MainCamera is required to place the guide model.",
                this);
            return;
        }

        Transform cameraTransform = playerCamera.transform;
        Transform positionReference = ResolveCameraOffset(cameraTransform);

        guiderInstance = FindSceneGuider();
        if (guiderInstance == null)
        {
            GameObject guiderAsset = guiderPrefab;
            if (guiderAsset == null)
                guiderAsset = Resources.Load<GameObject>("guider");

            if (guiderAsset == null)
            {
                Debug.LogError(
                    "GameFlowManager: could not find the guide model. " +
                    "Place a model named guider in the scene, assign " +
                    "guiderPrefab, or place guider.fbx in a Resources folder.",
                    this);
                return;
            }

            Vector3 startPosition = guiderStartPoint != null
                ? guiderStartPoint.position
                : transform.position;
            guiderInstance = Instantiate(guiderAsset, startPosition, Quaternion.identity);
            guiderWasInstantiated = true;
        }

        ApplyGuiderTextures();

        Vector3 playerPosition =
            positionReference.position +
            cameraTransform.forward * Mathf.Max(0.5f, guiderDistanceFromPlayer);
        Vector3 targetPosition = guiderInstance.transform.position;
        targetPosition.z = playerPosition.z;

        StartCoroutine(MoveGuider(guiderInstance.transform, targetPosition));
    }

    private void ApplyGuiderTextures()
    {
        Texture2D diffuseTexture =
            Resources.Load<Texture2D>("guider/rp_mei_posed_001_dif_2k");
        Texture2D normalTexture =
            Resources.Load<Texture2D>("guider/rp_mei_posed_001_norm_2k");

        if (diffuseTexture == null)
        {
            Debug.LogError(
                "GameFlowManager: the guide diffuse texture could not be loaded.",
                this);
            return;
        }

        Renderer[] renderers = guiderInstance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                if (material == null)
                    continue;

                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", diffuseTexture);
                if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", diffuseTexture);

                if (normalTexture == null)
                    continue;

                if (material.HasProperty("_BumpMap"))
                {
                    material.SetTexture("_BumpMap", normalTexture);
                    material.EnableKeyword("_NORMALMAP");
                }
                if (material.HasProperty("_NormalMap"))
                    material.SetTexture("_NormalMap", normalTexture);
            }
        }
    }

    private GameObject FindSceneGuider()
    {
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == "guider")
                return candidate.gameObject;
        }

        return null;
    }

    private System.Collections.IEnumerator MoveGuider(
        Transform guiderTransform,
        Vector3 targetPosition)
    {
        Vector3 startPosition = guiderTransform.position;
        float elapsed = 0f;

        while (elapsed < guiderMoveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / guiderMoveDuration);
            progress = progress * progress * (3f - 2f * progress);
            guiderTransform.position =
                Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }

        guiderTransform.position = targetPosition;
        StartCoroutine(PlayArrivalSequence(guiderTransform));
    }

    private System.Collections.IEnumerator PlayArrivalSequence(
        Transform guiderTransform)
    {
        if (guiderArrivalClip == null)
        {
            SetPlayerMovementEnabled(true);
            yield break;
        }

        audioSource.PlayOneShot(guiderArrivalClip, guiderArrivalVolume);
        yield return new WaitUntil(() => !audioSource.isPlaying);
        SetPlayerMovementEnabled(true);

        Quaternion startRotation = guiderTransform.rotation;
        Quaternion targetRotation = Quaternion.Euler(-90f, 90f, 0f);
        float rotationElapsed = 0f;

        while (rotationElapsed < guiderRotationDuration)
        {
            rotationElapsed += Time.deltaTime;
            float rotationProgress =
                Mathf.Clamp01(rotationElapsed / guiderRotationDuration);
            rotationProgress =
                rotationProgress * rotationProgress *
                (3f - 2f * rotationProgress);
            guiderTransform.rotation =
                Quaternion.Slerp(startRotation, targetRotation, rotationProgress);
            yield return null;
        }

        guiderTransform.rotation = targetRotation;

        Vector3 walkStart = guiderTransform.position;
        Vector3 walkTarget =
            walkStart + Vector3.right * guiderWalkDistance;
        float walkDuration =
            guiderWalkDistance / Mathf.Max(0.1f, guiderWalkSpeed);
        float walkElapsed = 0f;

        while (walkElapsed < walkDuration)
        {
            walkElapsed += Time.deltaTime;
            float walkProgress = Mathf.Clamp01(walkElapsed / walkDuration);
            walkProgress =
                walkProgress * walkProgress * (3f - 2f * walkProgress);
            guiderTransform.position =
                Vector3.Lerp(walkStart, walkTarget, walkProgress);
            yield return null;
        }

        guiderTransform.position = walkTarget;
    }

    private void SetPlayerMovementEnabled(bool enabled)
    {
        playerMovementEnabled = enabled;

        if (locomotionSystem != null)
            locomotionSystem.enabled = enabled;

        if (locomotionProviders == null)
            return;

        for (int i = 0; i < locomotionProviders.Length; i++)
        {
            if (locomotionProviders[i] == null)
                continue;

            locomotionProviders[i].enabled =
                enabled ? locomotionProviderStates[i] : false;
        }
    }

    private void LateUpdate()
    {
        if (playerMovementEnabled || xrOrigin == null)
            return;

        Vector3 currentPosition = xrOrigin.transform.position;
        currentPosition.x = lockedOriginPosition.x;
        currentPosition.z = lockedOriginPosition.z;
        xrOrigin.transform.position = currentPosition;

        if (cameraOffset != null)
        {
            Vector3 cameraOffsetPosition = cameraOffset.localPosition;
            cameraOffsetPosition.x = lockedCameraOffsetLocalPosition.x;
            cameraOffsetPosition.z = lockedCameraOffsetLocalPosition.z;
            cameraOffset.localPosition = cameraOffsetPosition;
        }

        if (playerCameraTransform != null)
        {
            Vector3 cameraPosition = playerCameraTransform.localPosition;
            cameraPosition.x = lockedCameraLocalPosition.x;
            cameraPosition.z = lockedCameraLocalPosition.z;
            playerCameraTransform.localPosition = cameraPosition;
        }
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

        if (guiderWasInstantiated && guiderInstance != null)
            Destroy(guiderInstance);
    }
}