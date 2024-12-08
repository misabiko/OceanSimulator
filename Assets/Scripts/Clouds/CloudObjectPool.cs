using System;
using System.Collections.Generic;
using UnityEngine;

public class CloudObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public List<Pool> Pools;
    public Dictionary<string, Queue<GameObject>> PoolDictionary;

    private CloudSpawner cloudSpawner;

    private void Awake()
    {
        cloudSpawner = GetComponent<CloudSpawner>();
    }

    private void Start()
    {
        cloudSpawner.InitializeSpawner();

        PoolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in Pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject cloud = Instantiate(pool.prefab, cloudSpawner.GetRandomSpawnPosition(), transform.rotation);
                cloud.transform.eulerAngles = new Vector3(90f, 0f, 0f);
                cloud.SetActive(true);
                objectPool.Enqueue(cloud);
            }

            PoolDictionary.Add(pool.tag, objectPool);
        }
    }

    private void Update()
    {
        Queue<GameObject> pool = PoolDictionary["Clouds"];
        
        foreach (GameObject cloud in pool)
        {
            if(Vector3.Distance(transform.position,cloud.transform.position) > cloudSpawner.outerRadius)
                UpdateCloud("Clouds", cloud, cloudSpawner.GetRandomSpawnPosition());
        }
    }

    private GameObject UpdateCloud(string tag, GameObject cloud, Vector3 position)
    {
        if (!PoolDictionary.ContainsKey(tag))
            return null;

        cloud.SetActive(true);
        cloud.transform.position = position;
        cloud.transform.eulerAngles = new Vector3(90f, 0f, 0f);;

        return cloud;
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

}
