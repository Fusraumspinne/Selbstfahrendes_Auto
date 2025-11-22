using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Traget : MonoBehaviour
{
    public float xMin;
    public float xMax;
    public float zMin;
    public float zMax;

    public float speed;

    public float randomTimeMin;
    public float randomTimeMax;

    private float timeToChangeDirection;
    private Vector3 direction;

    public bool autoMove;
    public bool moveOnZOnly;

    void Start()
    {
        if (moveOnZOnly)
        {
            direction = Vector3.forward;
        }
        else
        {
            direction = GetRandomDirection();
            timeToChangeDirection = GetRandomTime();
        }
    }

    void Update()
    {
        if (!autoMove) return;

        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        if (moveOnZOnly)
        {
            MoveOnZAxis();
        }
        else
        {
            timeToChangeDirection -= Time.deltaTime;
            if (timeToChangeDirection <= 0)
            {
                direction = GetRandomDirection();
                timeToChangeDirection = GetRandomTime();
            }

            CheckBoundsAndClamp();
        }
    }

    Vector3 GetRandomDirection()
    {
        Vector3 randomDirection;
        do
        {
            randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        } while (randomDirection.magnitude == 0);

        return randomDirection;
    }

    float GetRandomTime()
    {
        return Random.Range(randomTimeMin, randomTimeMax);
    }

    void CheckBoundsAndClamp()
    {
        Vector3 position = transform.position;

        if (position.x < xMin)
        {
            position.x = xMin;
            direction.x = Mathf.Abs(direction.x);
        }
        else if (position.x > xMax)
        {
            position.x = xMax;
            direction.x = -Mathf.Abs(direction.x);
        }

        if (position.z < zMin)
        {
            position.z = zMin;
            direction.z = Mathf.Abs(direction.z);
        }
        else if (position.z > zMax)
        {
            position.z = zMax;
            direction.z = -Mathf.Abs(direction.z);
        }

        transform.position = position;
    }
    
     void MoveOnZAxis()
    {
        Vector3 position = transform.position;

        if (position.z < zMin)
        {
            position.z = zMin;
            direction = Vector3.forward;
        }
        else if (position.z > zMax)
        {
            position.z = zMax;
            direction = Vector3.back;
        }

        transform.position = position;
    }
}
