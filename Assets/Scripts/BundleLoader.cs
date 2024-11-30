using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class BundleLoader : MonoBehaviour
{//fetch the bundle from the server with ip(#1)
    public static BundleLoader Instance { get; private set; }
    public Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
    public bool IsLoaded { get; private set; } = false;
    private void Awake()
    {
        // Singleton pattern to ensure only one instance of BundleLoader exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(DownloadAndCache());
    }

    IEnumerator DownloadAndCache()
    {
        while (!Caching.ready)
            yield return null;

        Debug.Log("Starting download...");
        using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle("http://74.208.214.17:3001/Android/thebundle"))//#1
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Download failed: " + www.error);
            }
            else
            {
                Debug.Log("Download successful...");
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                var names = bundle.GetAllAssetNames();  // get all the names of the assets
                Debug.Log("Loading asset...");
                foreach (var name in names)
                {
                    var loadAsset = bundle.LoadAssetAsync<GameObject>(name);
                    yield return loadAsset;
                    GameObject prefab = loadAsset.asset as GameObject;
                    if (prefab != null)
                    {
                        string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(name); // Gets file name without extension.
                        string[] parts = nameWithoutExtension.Split('/'); // Splits path into segments.
                        string simpleName = parts[parts.Length - 1]; 
                        Prefabs.Add(simpleName, prefab);  // store the prefabs in the dictionary
                        
                        Debug.Log("Loaded prefab:"+prefab);
                    }
                }
                IsLoaded = true;

                bundle.Unload(false);
            }
        }
    }
}
