using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
    }

    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDict;
    private Dictionary<string, Pool> poolConfigs;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        poolDict = new Dictionary<string, Queue<GameObject>>();
        poolConfigs = new Dictionary<string, Pool>();

        foreach (var pool in pools)
        {
            if (pool.prefab == null) continue;
            Queue<GameObject> queue = new Queue<GameObject>();
            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }
            poolDict[pool.tag] = queue;
            poolConfigs[pool.tag] = pool;
        }
    }

    public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDict.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPool] No pool with tag '{tag}'");
            return null;
        }

        Queue<GameObject> queue = poolDict[tag];
        GameObject obj;
        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else
        {
            if (poolConfigs.ContainsKey(tag) && poolConfigs[tag].prefab != null)
            {
                obj = Instantiate(poolConfigs[tag].prefab, transform);
            }
            else
            {
                return null;
            }
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        return obj;
    }

    public void Despawn(string tag, GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        if (poolDict.ContainsKey(tag))
        {
            poolDict[tag].Enqueue(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    public void DespawnAfterDelay(string tag, GameObject obj, float delay)
    {
        StartCoroutine(DespawnCoroutine(tag, obj, delay));
    }

    System.Collections.IEnumerator DespawnCoroutine(string tag, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
            Despawn(tag, obj);
    }
}
