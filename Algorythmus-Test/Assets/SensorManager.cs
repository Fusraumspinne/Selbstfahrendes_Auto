using UnityEngine;

public class SensorManager : MonoBehaviour
{
    public Transform[] sensors;  
    public float maxRange;
    public LayerMask obstacleMask;

    public float[] ReadSensors()
    {
        int n = sensors.Length;
        float[] dists = new float[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 origin = sensors[i].position;
            Vector3 dir = sensors[i].forward;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, maxRange, obstacleMask))
            {
                dists[i] = hit.distance / maxRange;
            }
            else
            {
                dists[i] = 1f;
            }
        }
        return dists;
    }

    private void OnDrawGizmos()
    {
        if (sensors == null) return;
        Gizmos.color = Color.red;
        foreach (var s in sensors)
        {
            if (s == null) continue;
            Gizmos.DrawRay(s.position, s.forward * maxRange);
        }
    }
}