using UnityEngine;
using TMPro;
using static TelescopeController;

public class PlayerInteract : MonoBehaviour
{
    public TelescopeController telescopeController;

    public float interactDistance = 5f;
    public Camera playerCamera;
    public TextMeshProUGUI interactText;
    public PlayerState state;
    
    void Update()
    {
        if (playerCamera == null) return;
        if (telescopeController.state != TelescopeController.PlayerState.Walking)
        {
            interactText.enabled = false;
            return;
        }
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactDistance, Color.red);

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            if (hit.collider.CompareTag("Telescope"))
            {
                if (!interactText.enabled) interactText.enabled = true;

                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log("Using telescope");
                    telescopeController.EnterTelescope();
                }

                return;
            }
        }

        interactText.enabled = false;
    }
}