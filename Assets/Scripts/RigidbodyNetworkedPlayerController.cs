using UnityEngine;
using System.Collections;
using GamepadInput;

public enum PlayerControllerState { walking, running, aiming };

public class RigidbodyNetworkedPlayerController : MonoBehaviour
{

  public PlayerControllerState playerState;

  public float baseSpeed = 2.0f;
  public float runSpeedMultiplier = 1.5f;
  public float aimSpeedMultiplier = 0.2f;
  public float horizontalAimingSpeed = 400f;
  public float verticalAimingSpeed = 400f;

  public float jumpHeight = 2.0f;
  public float jumpCooldown;
  float jumpCooldownTmp;

  public Vector3 relCameraPos = new Vector3(-2.0f, 1.0f, 0.0f);
  public float relCameraPosMag = 2.0f;
  public Vector3 pivotOffset = new Vector3(0.0f, 1.0f, 0.0f);
  public Vector3 camOffset = new Vector3(0.0f, 0.7f, -3.0f);
  public Vector3 aimPivotOffset = new Vector3(0.0f, 1.7f, -0.3f);
  public Vector3 aimCamOffset = new Vector3(0.8f, 0.0f, -1.0f);
  public Vector3 runPivotOffset = new Vector3(0.0f, 1.0f, 0.0f);
  public Vector3 runCamOffset = new Vector3(0.0f, 0.7f, -3.0f);
  public float runFOV = 100f;
  public float aimFOV = 60f;
  public float FOV = 80f;

  [HideInInspector]
  public float smoothingTurn = 2.0f;
  [HideInInspector]
  public float smoothingAim = 5.0f;

  public Vector2 controllerMoveDirection;
  public Vector2 controllerLookDirection;
  Vector3 moveDirection;

  public bool debug;

  GamePad.Index padIndex;
  [HideInInspector]
  public GamepadState gamepadState;

  bool isThisMachinesPlayer = false;

  Camera myCamera;
  Vector3 smoothPivotOffset;
  Vector3 smoothCamOffset;
  Vector3 targetPivotOffset;
  Vector3 targetCamOffset;
  float defaultFOV;
  float targetFOV;

  void Awake()
  {
    if (debug)
    {
      Debug.Log(this.ToString() + " awake.");
    }
    this.padIndex = GamePad.Index.Any;
    if (Network.isClient || Network.isServer)
    {
      if (this.GetComponent<NetworkView>().isMine)
      {
        this.isThisMachinesPlayer = true;
      }
    }
    else
    {
      this.isThisMachinesPlayer = true;
    }
    if (this.isThisMachinesPlayer)
    {
      if (debug)
      {
        Debug.Log("This machine owns player " + this.ToString());
      }
      GrabCamera(Camera.main);
    }
  }

  void GrabCamera(Camera cam)
  {
    this.myCamera = cam;
    relCameraPos = this.myCamera.transform.position - this.transform.position;
    relCameraPosMag = relCameraPos.magnitude - 0.5f;
    smoothPivotOffset = pivotOffset;
    smoothCamOffset = camOffset;
    Debug.Log("Camera has been reset by player");
    if (debug)
    {
      Debug.Log("Player grabbed camera " + cam.ToString());
    }
  }

  float angleH;
  float angleV;
  void AdjustCamera()
  {
    angleH += this.controllerLookDirection.x * this.horizontalAimingSpeed * Time.deltaTime;
    angleV += this.controllerLookDirection.y * this.verticalAimingSpeed * Time.deltaTime;
    angleV = Mathf.Clamp(angleV, -80, 80);
    Quaternion aimRotation = Quaternion.Euler(-angleV, angleH, 0);
    Quaternion camYRotation = Quaternion.Euler(0, angleH, 0);
    this.myCamera.transform.rotation = aimRotation;
    if (this.playerState == PlayerControllerState.aiming)
    {
      targetPivotOffset = aimPivotOffset;
      targetCamOffset = aimCamOffset;
      targetFOV = aimFOV;
    }
    else if (this.playerState == PlayerControllerState.running)
    {
      targetPivotOffset = runPivotOffset;
      targetCamOffset = runCamOffset;
      targetFOV = runFOV;
    }
    else
    {
      targetPivotOffset = pivotOffset;
      targetCamOffset = camOffset;
      targetFOV = FOV;
    }
    this.myCamera.fieldOfView = Mathf.Lerp(this.myCamera.fieldOfView, targetFOV, Time.deltaTime);

    #region Collisions
    Vector3 baseTempPosition = this.transform.position + camYRotation * targetPivotOffset;
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
    #endregion

    smoothPivotOffset = Vector3.Lerp(smoothPivotOffset, targetPivotOffset, 10f * Time.deltaTime);
    smoothCamOffset = Vector3.Lerp(smoothCamOffset, targetCamOffset, 10f * Time.deltaTime);

    this.myCamera.transform.position = this.transform.position + camYRotation * smoothPivotOffset + aimRotation * smoothCamOffset;
  }

  bool DoubleViewingPosCheck(Vector3 checkPos)
  {
    return ViewingPosCheck(checkPos) && ReverseViewingPosCheck(checkPos);
  }

  bool ViewingPosCheck(Vector3 checkPos)
  {
    RaycastHit hit;
    if (Physics.Raycast(checkPos, this.transform.position - checkPos, out hit, relCameraPosMag))
    {
      if (hit.transform != this && !hit.transform.GetComponent<Collider>().isTrigger)
      {
        return false;
      }
    }
    return true;
  }

  bool ReverseViewingPosCheck(Vector3 checkPos)
  {
    RaycastHit hit;
    if (Physics.Raycast(this.transform.position, checkPos - this.transform.position, out hit, relCameraPosMag))
    {
      if (hit.transform != transform && !hit.transform.GetComponent<Collider>().isTrigger)
      {
        return false;
      }
    }
    return true;
  }

  void Update()
  {
    #region TimerMaintenance
    jumpCooldownTmp -= Time.deltaTime;
    #endregion

    #region GatherInput
    if (this.isThisMachinesPlayer)
    {
      this.gamepadState = GamePad.GetState(this.padIndex);
    }
    this.controllerMoveDirection = GamePad.GetAxis(GamePad.Axis.LeftStick, this.padIndex);
    this.controllerLookDirection = GamePad.GetAxis(GamePad.Axis.RightStick, this.padIndex);
    #endregion

    #region SettingPlayerState
    if (this.gamepadState.LeftTrigger > 0.20f)
    {
      this.playerState = PlayerControllerState.aiming;
    }
    else if (this.gamepadState.LeftStick)
    {
      this.playerState = PlayerControllerState.running;
    }
    else
    {
      this.playerState = PlayerControllerState.walking;
    }
    #endregion

    Vector3 forward = this.myCamera.transform.TransformDirection(Vector3.forward);
    forward = forward.normalized;
    this.moveDirection = this.controllerMoveDirection.y * forward + this.controllerMoveDirection.x * new Vector3(forward.z, 0, -forward.x);

    #region RunningActionByState
    forward = this.myCamera.transform.TransformDirection(Vector3.forward);
    forward = forward.normalized;
    this.moveDirection = this.controllerMoveDirection.y * forward + this.controllerMoveDirection.x * new Vector3(forward.z, 0, -forward.x);
    moveDirection *= this.baseSpeed;
    switch (this.playerState)
    {
      case PlayerControllerState.walking:
        break;
      case PlayerControllerState.running:
        moveDirection *= this.runSpeedMultiplier;
        break;
      case PlayerControllerState.aiming:
        moveDirection *= this.aimSpeedMultiplier;
        break;
    }
    #endregion

    #region Movement
    this.GetComponent<Rigidbody>().velocity = this.moveDirection;
    #endregion
  }

  public bool isGrounded()
  {
    return Physics.Raycast(this.transform.position, Vector3.down, (this.GetComponent<CapsuleCollider>().height / 2.0f) + 0.1f);
  }
}
