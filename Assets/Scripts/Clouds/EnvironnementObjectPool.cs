using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
public class EnvironnementObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public List<GameObject> prefabs;
        public int size;
    }

    public List<Pool> Pools;
    private Dictionary<string, Queue<GameObject>> PoolDictionary;

    private CloudSpawner cloudSpawner;
    private RockSpawner rockSpawner;

    private void Awake()
    {
        PoolDictionary = new Dictionary<string, Queue<GameObject>>();
        cloudSpawner = GetComponent<CloudSpawner>();
        rockSpawner = GetComponent<RockSpawner>();

        cloudSpawner.InitializeSpawner();
        rockSpawner.InitializeSpawner();

        foreach (Pool pool in Pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                Random rand = new Random();
                int index = rand.Next(0, pool.prefabs.Count);
                GameObject instance = null;

                if (pool.tag == "Clouds")
                    instance = InstantiateCloud(pool, index);
                else if (pool.tag == "Rocks")
                    instance = InstantiateRocks(pool, index);

                if (instance != null)
                    objectPool.Enqueue(instance);
            }

            PoolDictionary.Add(pool.tag, objectPool);
        }
    }

    private GameObject InstantiateCloud(Pool pool, int index)
    {
        GameObject cloud = Instantiate(pool.prefabs[index], cloudSpawner.GetRandomSpawnPosition(), transform.rotation);
        cloud.GetComponent<CloudAgentSetter>().Initialize();
        cloud.transform.eulerAngles = new Vector3(90f, 0f, 0f);
        cloud.SetActive(true);
        return cloud;
    }
    
    private GameObject InstantiateRocks(Pool pool, int index)
    {
        GameObject rock = Instantiate(pool.prefabs[index], rockSpawner.GetRandomSpawnPosition(), transform.rotation);
        rock.transform.localScale = ReturnRandomScale();
        rock.transform.eulerAngles = new Vector3(0f, 0f, 0f);
        rock.SetActive(true);
        return rock;
    }

    private void Update()
    {
        UpdateClouds( PoolDictionary["Clouds"]);
        UpdateRocks( PoolDictionary["Rocks"]);
    }

    private void UpdateClouds(Queue<GameObject> pool)
    {
        foreach (GameObject cloud in pool)
        {
            if (Vector3.Distance(transform.position, cloud.transform.position) > cloudSpawner.outerRadius)
            {
                UpdatePoolObject("Clouds", cloud, cloudSpawner.GetRandomSpawnPosition(), new Vector3(90f, 0f, 0f));
            }
        }
    }
    private void UpdateRocks(Queue<GameObject> pool)
    {
        foreach (GameObject rock in pool)
        {
            if (Vector3.Distance(transform.position, rock.transform.position) > rockSpawner.outerRadius)
            {
                rock.transform.localScale = ReturnRandomScale();
                UpdatePoolObject("Rocks", rock, rockSpawner.GetRandomSpawnPosition(), new Vector3(0f, 0f, 0f));
            }
        }
    }

    private GameObject UpdatePoolObject(string tag, GameObject poolObject, Vector3 position, Vector3 rotation)
    {
        if (!PoolDictionary.ContainsKey(tag))
            return null;

        poolObject.SetActive(true);
        poolObject.transform.position = position;
        poolObject.transform.eulerAngles = rotation;

        return poolObject;
    }

    private GameObject SpawnFromPool(string tag, Vector3 position)
    {
        if (!PoolDictionary.ContainsKey(tag))
            return null;
        
        GameObject objectToSpawn = PoolDictionary[tag].Dequeue();
        
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.eulerAngles = new Vector3(90f, 0f, 0f);;
        
        PoolDictionary[tag].Enqueue(objectToSpawn);

        return objectToSpawn;
    }

    private Vector3 ReturnRandomScale()
    {
        Random randx = new Random();
        float x = randx.Next(100, 150) / 100f;
        Random randy = new Random();
        float y = randy.Next(100, 150) / 100f;
        Random randz = new Random();
        float z = randz.Next(100, 150) / 100f;

        return new Vector3(x, y, z);
    }
    
}
