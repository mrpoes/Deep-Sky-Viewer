using UnityEngine;

public class TelescopeController : MonoBehaviour
{
    [Header("Cameras")]
    public Camera playerCamera;
    public Camera telescopeCamera;


    [Header("Player Components")]
    public MonoBehaviour playerMovement;
    public PlayerInteract playerInteract;
    public MouseLook mouseLook;

    [Header("Telescope")]
    public Transform luneta;
    public Transform tube;
    public float tubeRotationSpeed = 2f;
    public float minAltitude = -90f; //-5f
    public float maxAltitude = 20f; //70f

    [Header("Eyepiece")]
    public Material eyepieceMaterial;
    public GameObject eyepieceMask;

    [Header("FOV")]
    public Transform operatingCameraAnchor;
    public float zoomFOV = 25f;
    public float normalFOV = 60f;
    public float transitionSpeed = 4f;

    private float t = 0f;
    private float targetT = 0f;
    private bool wasInTelescope = false;
    private int originalCullingMask;
    private float currentAltitude = 5f; //45f

    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;

    public enum PlayerState { Walking, Operating, Telescope }
    public PlayerState state;

    void Start()
    {
        originalCullingMask = playerCamera.cullingMask;
        originalCameraLocalPos = playerCamera.transform.localPosition;
        originalCameraLocalRot = playerCamera.transform.localRotation;
        eyepieceMask.SetActive(false);
        telescopeCamera.gameObject.SetActive(false);
        tube.localRotation = Quaternion.Euler(currentAltitude, 0f, 0f);
    }

    void Update()
    {
        HandleInput();
        HandleTelescopeRotation();
        HandleTransition();
        HandleStateChanges();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (state == PlayerState.Operating) ExitTelescope();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (state == PlayerState.Telescope) ExitEyepiece();
            else if (state == PlayerState.Operating) EnterEyepiece();
        }
    }

    void HandleTelescopeRotation()
    {
        if (state != PlayerState.Operating) return;

        float mouseX = Input.GetAxis("Mouse X") * tubeRotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * tubeRotationSpeed;

        luneta.Rotate(Vector3.up, mouseX, Space.World);

        currentAltitude = Mathf.Clamp(currentAltitude - mouseY, -50, 20);
        print(mouseY);
        print(currentAltitude);
        tube.localRotation = Quaternion.Euler(-currentAltitude, 0f, 0f);

        // lock player camera to anchor
        playerCamera.transform.position = operatingCameraAnchor.position;
        playerCamera.transform.rotation = operatingCameraAnchor.rotation;
    }
    void HandleTransition()
    {
        t = Mathf.Lerp(t, targetT, Time.deltaTime * transitionSpeed);
        float eased = Mathf.SmoothStep(0f, 1f, t);

        playerCamera.fieldOfView = Mathf.Lerp(normalFOV, zoomFOV, eased);
        telescopeCamera.fieldOfView = Mathf.Lerp(normalFOV, zoomFOV, eased);

        if (eyepieceMaterial != null)
            eyepieceMaterial.SetFloat("_Radius", Mathf.Lerp(1f, 0.4f, eased));
    }

    void HandleStateChanges()
    {
        bool inTelescope = state == PlayerState.Telescope;

        if (inTelescope && !wasInTelescope)
        {
            playerInteract.interactText.enabled = false;
            playerMovement.enabled = false;
            playerInteract.enabled = false;
            mouseLook.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            eyepieceMask.SetActive(true);
            playerCamera.cullingMask = 0;
            telescopeCamera.gameObject.SetActive(true);
            wasInTelescope = true;
        }

        // only fully exit when returning to Walking, not Operating
        if (state == PlayerState.Walking && wasInTelescope)
        {
            playerMovement.enabled = true;
            playerInteract.enabled = true;
            mouseLook.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            eyepieceMask.SetActive(false);
            playerCamera.cullingMask = originalCullingMask;
            telescopeCamera.gameObject.SetActive(false);
            wasInTelescope = false;
        }
    }

    public void EnterTelescope()
    {
        state = PlayerState.Operating;
        playerMovement.enabled = false;
        playerInteract.enabled = false;
        mouseLook.enabled = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void EnterEyepiece()
    {
        state = PlayerState.Telescope;
        targetT = 1f;
    }

    void ExitEyepiece()
    {
        state = PlayerState.Operating;
        targetT = 0f;
        eyepieceMask.SetActive(false);
        playerCamera.cullingMask = originalCullingMask;
        telescopeCamera.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        wasInTelescope = false; // allow HandleStateChanges to fire again on re-entry
    }

    public void ExitTelescope()
    {
        state = PlayerState.Walking;
        targetT = 0f;
        t = 0f;
        playerCamera.transform.localPosition = originalCameraLocalPos;
        playerCamera.transform.localRotation = originalCameraLocalRot;
        playerMovement.enabled = true;
        playerInteract.enabled = true;
        mouseLook.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}