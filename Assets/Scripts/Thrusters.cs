using UnityEngine;
using System.Collections;

public class Thrusters : MonoBehaviour {

  public float thrusterPower;

  [RPC]
  public void Activate()
  {
    this.gameObject.GetComponent<Rigidbody>().AddForce(this.gameObject.transform.forward * this.gameObject.GetComponent<Rigidbody>().mass * thrusterPower, ForceMode.Impulse);
  }

}
