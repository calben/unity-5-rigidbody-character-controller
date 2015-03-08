using UnityEngine;
using System.Collections;

public class DeftNetworkPlayer : MonoBehaviour
{

  public double m_InterpolationBackTime = 0.1;
  public double m_ExtrapolationLimit = 0.5;

  internal struct State
  {
    internal double timestamp;
    internal Vector3 pos;
    internal Vector3 velocity;
    internal Quaternion rot;
    internal Vector3 angularVelocity;
  }

  State[] m_BufferedState = new State[20];
  int m_TimestampCount;

  void Awake()
  {
    foreach (NetworkView n in GetComponents<NetworkView>())
      n.observed = this;
  }

  void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
  {
    if (stream.isWriting)
    {
      Vector3 pos = GetComponent<Rigidbody>().position;
      Quaternion rot = GetComponent<Rigidbody>().rotation;
      Vector3 velocity = GetComponent<Rigidbody>().velocity;
      Vector3 angularVelocity = GetComponent<Rigidbody>().angularVelocity;

      stream.Serialize(ref pos);
      stream.Serialize(ref velocity);
      stream.Serialize(ref rot);
      stream.Serialize(ref angularVelocity);
    }
    else
    {
      Vector3 pos = Vector3.zero;
      Vector3 velocity = Vector3.zero;
      Quaternion rot = Quaternion.identity;
      Vector3 angularVelocity = Vector3.zero;
      stream.Serialize(ref pos);
      stream.Serialize(ref velocity);
      stream.Serialize(ref rot);
      stream.Serialize(ref angularVelocity);

      for (int i = m_BufferedState.Length - 1; i >= 1; i--)
      {
        m_BufferedState[i] = m_BufferedState[i - 1];
      }

      State state;
      state.timestamp = info.timestamp;
      state.pos = pos;
      state.velocity = velocity;
      state.rot = rot;
      state.angularVelocity = angularVelocity;
      m_BufferedState[0] = state;

      m_TimestampCount = Mathf.Min(m_TimestampCount + 1, m_BufferedState.Length);

      for (int i = 0; i < m_TimestampCount - 1; i++)
        if (m_BufferedState[i].timestamp < m_BufferedState[i + 1].timestamp)
          Debug.Log("State inconsistent");
    }
  }

  void Update()
  {
    double interpolationTime = Network.time - m_InterpolationBackTime;
    if (m_BufferedState[0].timestamp > interpolationTime)
    {
      for (int i = 0; i < m_TimestampCount; i++)
      {
        if (m_BufferedState[i].timestamp <= interpolationTime || i == m_TimestampCount - 1)
        {
          State rhs = m_BufferedState[Mathf.Max(i - 1, 0)];
          State lhs = m_BufferedState[i];
          double length = rhs.timestamp - lhs.timestamp;
          float t = 0.0f;
          if (length > 0.0001f)
            t = (float)((interpolationTime - lhs.timestamp) / length);
          transform.localPosition = Vector3.Lerp(lhs.pos, rhs.pos, t);
          transform.localRotation = Quaternion.Slerp(lhs.rot, rhs.rot, t);
          return;
        }
      }
    }
    else
    {
      State latest = m_BufferedState[0];

      float extrapolationLength = (float)(interpolationTime - latest.timestamp);
      if (extrapolationLength < m_ExtrapolationLimit)
      {
        float axisLength = extrapolationLength * latest.angularVelocity.magnitude * Mathf.Rad2Deg;
        Quaternion angularRotation = Quaternion.AngleAxis(axisLength, latest.angularVelocity);

        GetComponent<Rigidbody>().position = latest.pos + latest.velocity * extrapolationLength;
        GetComponent<Rigidbody>().rotation = angularRotation * latest.rot;
        GetComponent<Rigidbody>().velocity = latest.velocity;
        GetComponent<Rigidbody>().angularVelocity = latest.angularVelocity;
      }
    }
  }

}