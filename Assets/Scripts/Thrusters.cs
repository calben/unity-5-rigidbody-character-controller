using UnityEngine;
using System.Collections;
using GamepadInput;

public class Thrusters : MonoBehaviour
{

  public float thrusterPower = 1.0f;
  public float velocityDampingSpeed = 0.02f;
  public float rotationRightingSpeed = 0.02f;

  NetworkView networkView;

  void Awake()
  {
    this.networkView = this.GetComponent<NetworkView>();
  }

  void FixedUpdate()
  {
    GamepadState state = this.GetComponent<RigidbodyNetworkedPlayerController>().gamepadState;
    Vector3 direction = new Vector3();
    if (state != null)
    {
      if (state.A)
      {
        Quaternion currentRotation = this.GetComponent<Rigidbody>().transform.rotation;
        this.GetComponent<Rigidbody>().transform.rotation = Quaternion.Slerp(currentRotation, Quaternion.Euler(currentRotation.x, 90, currentRotation.z), this.rotationRightingSpeed);
        this.GetComponent<Rigidbody>().velocity = Vector3.Lerp(this.GetComponent<Rigidbody>().velocity, Vector3.zero, this.velocityDampingSpeed);
        this.GetComponent<Rigidbody>().angularVelocity = Vector3.Lerp(this.GetComponent<Rigidbody>().angularVelocity, Vector3.zero, this.velocityDampingSpeed);
      }
      if (state.LeftShoulder)
      {
        direction = this.GetComponent<Rigidbody>().transform.up;
      }
      else if (state.RightShoulder)
      {
        direction = this.GetComponent<Rigidbody>().transform.up * -1;
      }
      else
      {
        return;
      }
    }
    if (Network.isServer || Network.isClient)
    {
      networkView.RPC("ActivateThrusters", RPCMode.AllBuffered, direction);
    }
    else
    {
      this.ActivateThrusters(direction);
    }
  }

  [RPC]
  public void ActivateThrusters(Vector3 direction)
  {
    this.gameObject.GetComponent<Rigidbody>().AddForce(direction * this.gameObject.GetComponent<Rigidbody>().mass * thrusterPower, ForceMode.Impulse);
  }

}
