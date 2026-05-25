using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Zombie")]
    public GameObject zombiePrefab;

    [Header("Wave Settings")]
    public int currentWave = 1;

    public int zombiesToSpawn = 10;
    public int maxZombiesAlive = 3;

    public float spawnRate = 2f;

    [Header("Spawn Area")]
    public float spawnRadius = 5f;

    private int zombiesSpawned;
    private int zombiesKilled;

    private float nextSpawnTime;

    void Update()
    {
        GameObject[] zombies =
            GameObject.FindGameObjectsWithTag("Zombie");

        // Spawn zombie
        if (zombiesSpawned < zombiesToSpawn)
        {
            if (zombies.Length < maxZombiesAlive)
            {
                if (Time.time >= nextSpawnTime)
                {
                    SpawnZombie();

                    zombiesSpawned++;

                    nextSpawnTime =
                        Time.time + spawnRate;
                }
            }
        }

        // Cek wave selesai
        zombiesKilled =
            zombiesToSpawn - zombies.Length;

        if (zombiesKilled >= zombiesToSpawn)
        {
            NextWave();
        }
    }

    void SpawnZombie()
    {
        Vector3 randomPosition =
            transform.position +
            new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                0,
                Random.Range(-spawnRadius, spawnRadius)
            );

        Instantiate(
            zombiePrefab,
            randomPosition,
            Quaternion.identity
        );
    }

    void NextWave()
    {
        Debug.Log("Wave Complete!");

        currentWave++;

        zombiesToSpawn += 5;

        zombiesSpawned = 0;

        zombiesKilled = 0;
    }
}