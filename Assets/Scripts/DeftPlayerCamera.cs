using UnityEngine;
using System.Collections;
using Image = UnityEngine.UI.Image;

public class DeftPlayerCamera : MonoBehaviour
{
    public GameObject player;

    public Vector3 pivotOffset = new Vector3(0.0f, 1.0f, 0.0f);
    public Vector3 camOffset = new Vector3(0.0f, 0.7f, -3.0f);

    public float smooth = 10f;

    public Vector3 aimPivotOffset = new Vector3(0.0f, 1.7f, -0.3f);
    public Vector3 aimCamOffset = new Vector3(0.8f, 0.0f, -1.0f);

    public float horizontalAimingSpeed = 400f;
    public float verticalAimingSpeed = 400f;
    public float maxVerticalAngle = 30f;
    public float flyMaxVerticalAngle = 60f;
    public float minVerticalAngle = -60f;

    public float mouseSensitivity = 0.3f;

    public float sprintFOV = 100f;

    public DeftPlayerController player_controller;
    float angleH = 0;
    float angleV = 0;
    Transform cam;

    Vector3 relCameraPos;
    float relCameraPosMag;

    Vector3 smoothPivotOffset;
    Vector3 smoothCamOffset;
    Vector3 targetPivotOffset;
    Vector3 targetCamOffset;

    float defaultFOV;
    float targetFOV;

    void Awake()
    {
    }

    public void Reset()
    {
        cam = transform;
        player_controller = player.GetComponent<DeftPlayerController>();
        relCameraPos = transform.position - player.transform.position;
        relCameraPosMag = relCameraPos.magnitude - 0.5f;
        smoothPivotOffset = pivotOffset;
        smoothCamOffset = camOffset;
        defaultFOV = cam.GetComponent<Camera>().fieldOfView;
        Debug.Log("Camera has been reset by player");
    }

    void LateUpdate()
    {
        if (this.player == null)
        {
            return;
        }
        angleH += player_controller.controllerLookDirection.x * horizontalAimingSpeed * Time.deltaTime;
        angleV += player_controller.controllerLookDirection.y * verticalAimingSpeed * Time.deltaTime;
        angleV = Mathf.Clamp(angleV, minVerticalAngle, maxVerticalAngle);


        Quaternion aimRotation = Quaternion.Euler(-angleV, angleH, 0);
        Quaternion camYRotation = Quaternion.Euler(0, angleH, 0);
        cam.rotation = aimRotation;

        if (player_controller.state == PlayerState.aiming)
        {
            targetPivotOffset = aimPivotOffset;
            targetCamOffset = aimCamOffset;
        }
        else
        {
            targetPivotOffset = pivotOffset;
            targetCamOffset = camOffset;
        }

        if (player_controller.state == PlayerState.sprinting)
        {
            targetFOV = sprintFOV;
        }
        else
        {
            targetFOV = defaultFOV;
        }
        cam.GetComponent<Camera>().fieldOfView = Mathf.Lerp(cam.GetComponent<Camera>().fieldOfView, targetFOV, Time.deltaTime);

        // Test for collision
        Vector3 baseTempPosition = player.transform.position + camYRotation * targetPivotOffset;
        Vector3 tempOffset = targetCamOffset;
        for (float zOffset = targetCamOffset.z; zOffset < 0; zOffset += 0.5f)
        {
            tempOffset.z = zOffset;
            if (DoubleViewingPosCheck(baseTempPosition + aimRotation * tempOffset))
            {
                targetCamOffset.z = tempOffset.z;
                break;
            }
        }

        smoothPivotOffset = Vector3.Lerp(smoothPivotOffset, targetPivotOffset, smooth * Time.deltaTime);
        smoothCamOffset = Vector3.Lerp(smoothCamOffset, targetCamOffset, smooth * Time.deltaTime);

        cam.position = player.transform.position + camYRotation * smoothPivotOffset + aimRotation * smoothCamOffset;

    }

    // concave objects doesn't detect hit from outside, so cast in both directions
    bool DoubleViewingPosCheck(Vector3 checkPos)
    {
        return ViewingPosCheck(checkPos) && ReverseViewingPosCheck(checkPos);
    }

    bool ViewingPosCheck(Vector3 checkPos)
    {
        RaycastHit hit;

        // If a raycast from the check position to the player hits something...
        if (Physics.Raycast(checkPos, player.transform.position - checkPos, out hit, relCameraPosMag))
        {
            // ... if it is not the player...
            if (hit.transform != player && !hit.transform.GetComponent<Collider>().isTrigger)
            {
                // This position isn't appropriate.
                return false;
            }
        }
        // If we haven't hit anything or we've hit the player, this is an appropriate position.
        return true;
    }

    bool ReverseViewingPosCheck(Vector3 checkPos)
    {
        RaycastHit hit;

        if (Physics.Raycast(player.transform.position, checkPos - player.transform.position, out hit, relCameraPosMag))
        {
            if (hit.transform != transform && !hit.transform.GetComponent<Collider>().isTrigger)
            {
                return false;
            }
        }
        return true;
    }

}
