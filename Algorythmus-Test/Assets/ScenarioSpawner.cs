using UnityEngine;

public class ScenarioSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public int obstacleCount;
    public Vector2 areaSize;
    public float clearCenterSize;

    public void SpawnNewPattern()
    {
        foreach (var o in GameObject.FindGameObjectsWithTag("Obstacle"))
        {
            Destroy(o);
        }

        for (int i = 0; i < obstacleCount; i++)
        {
            Vector3 pos;
            int attempts = 0;

            do
            {
                pos = new Vector3(
                    Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                    0,
                    Random.Range(-areaSize.y / 2f, areaSize.y / 2f)
                );
                attempts++;
                if (attempts > 100) break;
            } while (Mathf.Abs(pos.x) < clearCenterSize / 2f && Mathf.Abs(pos.z) < clearCenterSize / 2f);

            GameObject o = Instantiate(obstaclePrefab, pos, Quaternion.identity);
            o.tag = "Obstacle";
            o.layer = LayerMask.NameToLayer("Obstacles");
        }
    }
}