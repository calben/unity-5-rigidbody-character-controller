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
  public float velocityDampingSpeed = 0.02f;

  public float jumpHeight = 2.0f;
  public float jumpCooldown;
  float jumpCooldownTmp;

  public float relCameraPosMag = 1.5f;
  public Vector3 pivotOffset = new Vector3(1.5f, 0.0f, -2.0f);
  public Vector3 camOffset = new Vector3(1f, 3.5f, -6f);
  public Vector3 aimPivotOffset = new Vector3(1.0f, 0.0f, 0.0f);
  public Vector3 aimCamOffset = new Vector3(1f, 3f, -5.0f);
  public Vector3 runPivotOffset = new Vector3(1f, 0.0f, -2.0f);
  public Vector3 runCamOffset = new Vector3(1f, 3.5f, -6f);
  public float runFOV = 80f;
  public float aimFOV = 40f;
  public float FOV = 60f;

  [HideInInspector]
  public float smoothingTurn = 2.0f;
  [HideInInspector]
  public float smoothingAim = 5.0f;

  public Vector2 controllerMoveDirection;
  public Vector2 controllerLookDirection;
  Vector3 moveDirection;

  public bool debug = true;

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

  public bool showCrosshair = false;
  public Texture2D verticalTexture;
  public Texture2D horizontalTexture;

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
    //angleV = Mathf.Clamp(angleV, 80, 80);
    //angleH = Mathf.Clamp(angleH, -80, 80);
    Quaternion aimRotation = Quaternion.Euler(-angleV, angleH, 0);
    if (this.GetComponent<Rigidbody>().velocity.magnitude > 0.2f)
    {
      aimRotation = Quaternion.Slerp(aimRotation, Quaternion.Euler(this.transform.forward), this.velocityDampingSpeed);
    }
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
        if (debug)
        {
          Debug.Log("Collision detected");
        }
        targetCamOffset.z = tempOffset.z;
        break;
      }
    }
    #endregion

    smoothPivotOffset = Vector3.Lerp(smoothPivotOffset, targetPivotOffset, 10f * Time.deltaTime);
    smoothCamOffset = Vector3.Lerp(smoothCamOffset, targetCamOffset, 10f * Time.deltaTime);

    this.myCamera.transform.position = this.transform.position + camYRotation * smoothPivotOffset + aimRotation * smoothCamOffset;
  }


  Texture2D temp;
  public float spread;
  public float minSpread = 20.0f;
  public float maxSpread = 40.0f;
  void DrawCrossHair()
  {
    GUIStyle verticalT = new GUIStyle();
    GUIStyle horizontalT = new GUIStyle();
    verticalT.normal.background = verticalTexture;
    horizontalT.normal.background = horizontalTexture;
    spread = Mathf.Clamp(spread, minSpread, maxSpread);
    Vector2 pivot = new Vector2(Screen.width / 2, Screen.height / 2);
    GUI.Box(new Rect((Screen.width - 2) / 2, (Screen.height - spread) / 2 - 14, 2, 14), temp, horizontalT);
    GUIUtility.RotateAroundPivot(45, pivot);
    GUI.Box(new Rect((Screen.width + spread) / 2, (Screen.height - 2) / 2, 14, 2), temp, verticalT);
    GUIUtility.RotateAroundPivot(0, pivot);
    GUI.Box(new Rect((Screen.width - 2) / 2, (Screen.height + spread) / 2, 2, 14), temp, horizontalT);
  }

  void OnGUI()
  {
    if (this.playerState == PlayerControllerState.aiming)
    {
      this.DrawCrossHair();
    }
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

    AdjustCamera();

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
    if (this.moveDirection.magnitude > 0.05f)
    {
      this.GetComponent<Rigidbody>().velocity = Vector3.Lerp(this.GetComponent<Rigidbody>().velocity, this.moveDirection, this.velocityDampingSpeed);
      this.transform.forward = Vector3.Lerp(this.transform.forward, this.GetComponent<Rigidbody>().velocity.normalized, this.velocityDampingSpeed);
      this.GetComponent<Rigidbody>().angularVelocity = Vector3.Lerp(this.GetComponent<Rigidbody>().angularVelocity, Vector3.zero, this.velocityDampingSpeed);
    }
    #endregion
  }

  public bool isGrounded()
  {
    return Physics.Raycast(this.transform.position, Vector3.down, (this.GetComponent<CapsuleCollider>().height / 2.0f) + 0.1f);
  }
}
