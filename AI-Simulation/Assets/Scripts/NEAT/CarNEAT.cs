using UnityEngine;

/// <summary>
/// Ein Car‐Agent, der mit einem NEAT‐Netzwerk gesteuert wird.
/// Zusätzlich hat es eine maximale Lebenszeit (z.B. 15 s), 
/// nach der das Auto zwangsweise „stirbt“, falls es nicht vorher kollidiert.
/// </summary>
public class CarNEAT : MonoBehaviour
{
    [Header("=== Komponenten ===")]
    [SerializeField] private Rigidbody rig;
    [SerializeField] private WheelCollider[] wheelColliders;
    [SerializeField] private Transform[] wheelMeshes;

    [Header("=== Fahrparameter ===")]
    [SerializeField] private float motorPower = 200f;
    [SerializeField] private float maxSteerAngle = 30f;

    [Header("=== Lebenszeitlimit (Sekunden) ===")]
    [SerializeField] private float maxLifetime = 15f;

    private NeuralNetworkNEAT net;
    private Transform target;
    private System.Action<CarNEAT, int> onDeath;
    private int genomeIndex;

    private bool initialized = false;
    private bool isDead = false;
    public bool IsDead => isDead;

    private float fitness = 0f;
    private float lifeTimer = 0f;

    /// <summary>
    /// Initialisiert den Car-Agent mit dem NEAT-Netzwerk, Ziel und Callback.
    /// </summary>
    public void Init(NeuralNetworkNEAT network, Transform target, System.Action<CarNEAT, int> onDeathCallback, int index)
    {
        this.net = network;
        this.target = target;
        this.onDeath = onDeathCallback;
        this.genomeIndex = index;
        initialized = true;
        lifeTimer = 0f;
        fitness = 0f;
    }

    private void Update()
    {
        if (!initialized || isDead) return;

        // 1) Timer hochzählen
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            // Zwangsweise „Tod“ nach maxLifetime
            Die();
            return;
        }

        // 2) Sensor‐Input: Winkel und Raycasts
        float distance = Vector3.Distance(transform.position, target.position);
        float angleToTarget = Vector3.SignedAngle(transform.forward,
            (target.position - transform.position).normalized, Vector3.up) / 180f;

        // 9 Raycast‐Distanzen
        float[] vision = new float[9];
        float[] angles = { -75, -50, -25, -10, 0, 10, 25, 50, 75 };
        for (int i = 0; i < angles.Length; i++)
            vision[i] = PerformRaycast(Quaternion.Euler(0, angles[i], 0) * transform.forward);

        // 3) Input‐Vektor (dim = numInputs = 11)
        float[] inputs = new float[11];
        inputs[0] = angleToTarget;
        for (int i = 0; i < vision.Length; i++) inputs[i + 1] = vision[i];

        // 4) FeedForward im NEAT-Netzwerk
        float[] outputs = net.FeedForward(inputs);
        float steer = Mathf.Clamp(outputs[0], -1f, 1f);
        float throttle = Mathf.Clamp(outputs[1], 0f, 1f);

        // 5) Wende Steuerung an
        ApplySteering(steer);
        ApplyThrottle(throttle);

        // 6) Fitness‐Berechnung: Je näher am Ziel, desto höher
        float distFactor = Mathf.Pow(0.5f, distance / 50f);
        fitness += distFactor * Time.deltaTime;
    }

    private float PerformRaycast(Vector3 dir)
    {
        RaycastHit hit;
        float maxDist = 30f;
        int mask = ~LayerMask.GetMask("Car");
        if (Physics.Raycast(transform.position, dir, out hit, maxDist, mask))
        {
            Debug.DrawRay(transform.position, dir * hit.distance, Color.red);
            return hit.distance / maxDist;
        }
        Debug.DrawRay(transform.position, dir * maxDist, Color.green);
        return 1f;
    }

    private void ApplyThrottle(float t)
    {
        float motor = t * motorPower;
        // Vorder- und Hinterachse antreiben
        foreach (var wc in wheelColliders)
            wc.motorTorque = motor;
    }

    private void ApplySteering(float s)
    {
        float angle = s * maxSteerAngle;
        // Nur vordere Räder lenken (Indexes 0 und 1)
        if (wheelColliders.Length >= 2)
        {
            wheelColliders[0].steerAngle = angle;
            wheelColliders[1].steerAngle = angle;
        }
    }

    private void FixedUpdate()
    {
        if (!initialized || isDead) return;

        // 7) Update der Rad-Meshes
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            Vector3 pos; Quaternion rot;
            wheelColliders[i].GetWorldPose(out pos, out rot);
            wheelMeshes[i].position = pos;
            wheelMeshes[i].rotation = rot;
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        if (isDead) return;

        // Kollision mit Wänden o.Ä. → ausfallen
        if (col.gameObject.CompareTag("Wall"))
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        // Setze Fitness im zugehörigen Genome
        ManagerNEAT manager = FindObjectOfType<ManagerNEAT>();
        if (manager != null && genomeIndex >= 0 && genomeIndex < manager.population.Count)
        {
            manager.population[genomeIndex].fitness = fitness;
            // Melde den Tod ans ManagerNEAT
            manager.OnAgentDeath(this, genomeIndex);
        }
        Destroy(gameObject);
    }
}
