using System.Collections;
using System.Reflection;
using GamepadInput;
using UnityEngine;


public enum PlayerState { idle, aiming, walking, running, sprinting, jumping };
public delegate void ButtonAction();

/// <summary>
/// 
/// The player state is checked with the following precedence:
///   1. Aiming
///   2. Running
///   3. Sprinting
///   4. Walking
/// 
/// </summary>
public class DeftPlayerController : MonoBehaviour
{
  public float speedWhileAim = 1.0f;
  public float speedWhileWalk = 2.0f;
  public float speedWhileRun = 4.0f;
  public float speedWhileSprint = 7.0f;

  public float jumpHeight = 5.0f;
  public float jumpCooldown = 1.0f;
  private float jumpCooldownTemp;

  public float smoothingTurn = 2.0f;
  public float smoothingAim = 5.0f;

  public Vector2 controllerLookDirection;
  public Vector2 controllerMoveDirection;

  public float smooth = 20f;

  public float playerHeight;
  public float playerWidth;

  public bool debug;
  public bool singlePlayer;

  public bool isGrounded;
  public PlayerState state;

  public bool inverted = false;
  float invertTimer = 0;

  public bool playerEnabled = true;

  GamePad.Index padIndex = GamePad.Index.One;

  float speed_current;
  Vector3 move_direction;
  Vector3 forward;
  Vector3 last_input;

  Animator animator;

  void Awake()
  {
    animator = this.GetComponent<Animator>();
    if (GetComponent<NetworkView>().isMine || singlePlayer)
    {
      Camera.main.GetComponent<DeftPlayerCamera>().player = this.gameObject;
      Camera.main.GetComponent<DeftPlayerCamera>().Reset();
    }
    Debug.Log("PLAYER IS AWAKE");
    this.playerHeight = this.GetComponent<CapsuleCollider>().height;
  }

  public GamepadState gamepadState;

  void Update()
  {
    if (GetComponent<NetworkView>().isMine || singlePlayer)
    {
      bool gamePadExists = true;
      this.gamepadState = GamePad.GetState(this.padIndex);
      if (this.gamepadState == null)
      {
        gamePadExists = false;
        this.gamepadState = new GamepadState();
      }

      invertTimer += Time.deltaTime;

      // invert y axis if down on dpad is pressed
      if (this.gamepadState.dPadAxis.y < 0 && invertTimer > 1)
      {
        if (inverted)
          inverted = false;
        else
          inverted = true;

        invertTimer = 0;
      }

      #region PlayerState

      if (playerEnabled && (this.gamepadState.LeftTrigger > 0.20f || Input.GetMouseButtonDown(1)))
      {
        this.state = PlayerState.aiming;
      }
      else if (playerEnabled && (this.gamepadState.A || Input.GetKeyDown(KeyCode.Space)))
      {
        this.state = PlayerState.jumping;
      }
      else if (playerEnabled && (this.gamepadState.LeftStick && this.gamepadState.RightStick))
      {
        this.state = PlayerState.sprinting;
      }
      else if (playerEnabled && (this.gamepadState.LeftStick))
      {
        this.state = PlayerState.running;
      }
      else if (playerEnabled && (this.gamepadState.LeftStickAxis.sqrMagnitude > 0.20f))
      {
        this.state = PlayerState.walking;
      }
      else
      {
        this.state = PlayerState.idle;
      }

      #endregion

      this.controllerMoveDirection = new Vector3(0, 0, 0);
      this.controllerLookDirection = new Vector3(0, 0, 0);
      if (gamePadExists && playerEnabled)
      {
        this.controllerMoveDirection = GamePad.GetAxis(GamePad.Axis.LeftStick, padIndex);
        this.controllerLookDirection = GamePad.GetAxis(GamePad.Axis.RightStick, padIndex);
        if (this.gamepadState.B)
        {
        //ht
        }
      }
      else
      {
        this.controllerMoveDirection = new Vector2(Input.GetAxis("Horizontal"), -Input.GetAxis("Vertical"));
        this.controllerLookDirection = new Vector2(Mathf.Clamp(Input.GetAxis("Mouse X"), -1, 1), Mathf.Clamp(Input.GetAxis("Mouse Y"), -1, 1));
      }

      if (inverted)
        this.controllerLookDirection.y *= -1;
    }
  }

  void FixedUpdate()
  {
    jumpCooldownTemp -= Time.deltaTime;
    Animate();
    forward = Camera.main.transform.TransformDirection(Vector3.forward);
    forward = forward.normalized;
    this.move_direction = this.controllerMoveDirection.y * forward + this.controllerMoveDirection.x * new Vector3(forward.z, 0, -forward.x);
    if (this.move_direction.x != 0 || this.move_direction.z != 0)
    {
      last_input = move_direction;
    }
    switch (this.state)
    {
      case PlayerState.aiming:
        {
          speed_current = speedWhileAim;
          break;
        }
      case PlayerState.jumping:
        {
          if (speed_current > 0 && jumpCooldownTemp < 0)
          {
            GetComponent<Rigidbody>().velocity += new Vector3(0, jumpHeight, 0);
            jumpCooldownTemp = jumpCooldown;
          }
          break;
        }
      case PlayerState.sprinting:
        {
          speed_current = speedWhileSprint;
          break;
        }
      case PlayerState.running:
        {
          speed_current = speedWhileRun;
          break;
        }
      default:
        {
          speed_current = speedWhileWalk;
          break;
        }
    }
    if (CalculateGrounded())
    {
      Vector3 last_input_without_y = new Vector3(last_input.x, 0, last_input.z);
      Vector3 forward_without_y = new Vector3(transform.forward.x, 0, transform.forward.z);
      transform.forward = Vector3.Lerp(forward_without_y, last_input_without_y, smooth * Time.deltaTime);
      Vector3 move_without_y = new Vector3(this.move_direction.x, 0, this.move_direction.z);
      this.GetComponent<Rigidbody>().velocity = new Vector3(move_direction.x * speed_current, GetComponent<Rigidbody>().velocity.y, move_direction.z * speed_current);
    }
    else
    {
      Vector3 forward_input = new Vector3(transform.forward.x, last_input.y, transform.forward.z);
      transform.forward = Vector3.Lerp(forward_input, last_input, smooth * Time.deltaTime);
    }

  }

  public void Animate()
  {

  }


  public bool CalculateGrounded()
  {
    return Physics.Raycast(transform.position, Vector3.down, (this.playerHeight / 2.0f) + 0.1f);
  }

}
