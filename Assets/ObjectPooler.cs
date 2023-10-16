using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance; // Singleton instance for easy access.

    public GameObject objectToPool;      // Prefab to pool.
    public int initialPoolSize = 1000;     // Initial size of the pool.

    private List<GameObject> pooledObjects;
    public Transform landParrent;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        pooledObjects = new List<GameObject>();
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject obj = Instantiate(objectToPool);
            obj.SetActive(false);
            pooledObjects.Add(obj);
        }
    }

    public GameObject GetPooledObject()
    {
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (!pooledObjects[i].activeInHierarchy)
            {
                return pooledObjects[i];
            }
        }

        // If there's no available object in the pool, create a new one, add to pool and return.
        GameObject newObj = Instantiate(objectToPool);
        newObj.SetActive(false);
        pooledObjects.Add(newObj);
        newObj.transform.Rotate(0,180,0);
        return newObj;
    }

    public void ReturnObjectToPool(GameObject obj)
    {
        LandController landController = obj.GetComponent<LandController>();
        landController.ResetLand();
        landController.textMesh.text = "";
        if (landController.meshCollider != null)
        {
            Destroy(landController.meshCollider);
        }

        obj.SetActive(false);
    }
}