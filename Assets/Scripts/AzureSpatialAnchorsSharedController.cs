using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using Microsoft.Azure.SpatialAnchors.Unity.Samples;

public class AzureSpatialAnchorsSharedController : MonoBehaviour
{
    public static AzureSpatialAnchorsSharedController Instance = null;

    public GameObject AnchoredObjectPrefab = null;

#if !UNITY_EDITOR
    public AnchorExchanger anchorExchanger = new AnchorExchanger();
#endif

    protected GameObject spawnedObject = null;

    protected AzureSpatialAnchorsDemoWrapper CloudManager { get; private set; }

    protected CloudSpatialAnchor currentCloudAnchor;

    private readonly List<GameObject> otherSpawnedObjects = new List<GameObject>();

    private readonly Queue<Action> dispatchQueue = new Queue<Action>();

    public string BaseSharingUrl = "";
    public long anchorNumber = 0;

    private string _anchorKeyToFind;
    private int anchorsLocated = 0;
    private int anchorsExpected = 0;
    private long? _anchorNumberToFind;

    //Awake is always called before any Start functions
    void Awake()
    {
        //Check if instance already exists
        if (Instance == null)

            //if not, set instance to this
            Instance = this;

        //If instance already exists and it's not this:
        else if (Instance != this)

            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);

        //Sets this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);
    }


    public void Start()
    {
        this.CloudManager = AzureSpatialAnchorsDemoWrapper.Instance;

        if (this.CloudManager == null)
        {
            Debug.Log("AzureSpatialAnchorsDemoWrapper doesn't exist in the scene. Make sure it has been added.");
            return;
        }

        Uri result;
        if (!Uri.TryCreate(this.BaseSharingUrl, UriKind.Absolute, out result))
        {
            Debug.Log("BaseSharingUrl, on the AzureSpatialAnchors object in your scene, is not a valid url");
            return;
        }
        else
        {
            this.BaseSharingUrl = $"{result.Scheme}://{result.Host}/api/anchors";
        }

        Debug.Log(this.BaseSharingUrl);

#if !UNITY_EDITOR
        anchorExchanger.WatchKeys(this.BaseSharingUrl);
#endif

        this.CloudManager.OnSessionUpdated += this.CloudManager_SessionUpdated;
        this.CloudManager.OnAnchorLocated += this.CloudManager_OnAnchorLocated;
        this.CloudManager.OnLocateAnchorsCompleted += this.CloudManager_OnLocateAnchorsCompleted;
        this.CloudManager.OnLogDebug += CloudManager_OnLogDebug;
        this.CloudManager.OnSessionError += CloudManager_OnSessionError;

    }

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    public void Update()
    {
        lock (this.dispatchQueue)
        {
            if (this.dispatchQueue.Count > 0)
            {
                this.dispatchQueue.Dequeue()();
            }
        }
    }

    public async void InitializeLocateFlowDemo()
    {
        //long anchorNumber = 1;
        _anchorNumberToFind = anchorNumber;

#if !UNITY_EDITOR
         _anchorKeyToFind = await anchorExchanger.RetrieveAnchorKey(_anchorNumberToFind.Value);
#endif
        if (_anchorKeyToFind == null)
        {
            Debug.Log("Anchor Number Not Found!");
        }
        else
        {
            // Not Implemented Yet
        }

    }

    public void CreateSession()
    {
        InitializeLocateFlowDemo();

        this.CloudManager.ResetSessionStatusIndicators();
        this.currentCloudAnchor = null;
    }

    public void ConfigSessionForQuery()
    {
        this.anchorsLocated = 0;
        List<string> anchorsToFind = new List<string>();

        bool currentAppState = true;

        if (currentAppState)
        {
#if !UNITY_EDITOR
            anchorsToFind.AddRange(anchorExchanger.AnchorKeys);
#endif
        }
        {
            this.anchorsExpected = anchorsToFind.Count;
            this.CloudManager.SetAnchorIdsToLocate(anchorsToFind);
        }
    }

    public void QueryAnchor()
    {
        this.CloudManager.EnableProcessing = true;
        this.CloudManager.CreateWatcher();
    }

    public void StopSessionForQuery()
    {
        this.CloudManager.EnableProcessing = false;
        this.CloudManager.ResetSession();

        this.currentCloudAnchor = null;
    }

    private void CloudManager_OnAnchorLocated(object sender, AnchorLocatedEventArgs args)
    {
        Debug.LogFormat("Anchor recognized as a possible anchor {0} {1}", args.Identifier, args.Status);
        if (args.Status == LocateAnchorStatus.Located)
        {
            //this.OnCloudAnchorLocated(args);

            CloudSpatialAnchor nextCsa = args.Anchor;
            this.currentCloudAnchor = args.Anchor;


            this.QueueOnUpdate(new Action(() =>
            {
                this.anchorsLocated++;
                this.currentCloudAnchor = nextCsa;
                Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
                anchorPose = this.currentCloudAnchor.GetAnchorPose();
#endif
                // HoloLens: The position will be set based on the unityARUserAnchor that was located.

                GameObject nextObject = this.SpawnNewAnchoredObject(anchorPose.position, anchorPose.rotation, this.currentCloudAnchor);
                //this.AttachTextMesh(nextObject, _anchorNumberToFind);
                this.otherSpawnedObjects.Add(nextObject);

                //if (this.anchorsLocated >= this.anchorsExpected)
                //{
                //    //this.currentAppState = AppState.DemoStepStopSessionForQuery;
                //}
            }));
        }
    }

    private void CloudManager_OnLocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs args)
    {
        //Not Implemented Yet
        //this.OnCloudLocateAnchorsCompleted(args);

        Debug.Log("Locate pass complete");
    }

    private void CloudManager_SessionUpdated(object sender, SessionUpdatedEventArgs args)
    {
        //Not implemented yet
        //this.OnCloudSessionUpdated();
    }

    private void CloudManager_OnSessionError(object sender, SessionErrorEventArgs args)
    {
        //this.isErrorActive = true;
        //this.feedbackBox.text = string.Format("Error: {0}", args.ErrorMessage);
        Debug.Log(args.ErrorMessage);
    }

    private void CloudManager_OnLogDebug(object sender, OnLogDebugEventArgs args)
    {
        Debug.Log(args.Message);
    }

    /// <summary>
    /// Queues the specified <see cref="Action"/> on update.
    /// </summary>
    /// <param name="updateAction">The update action.</param>
    protected void QueueOnUpdate(Action updateAction)
    {
        lock (this.dispatchQueue)
        {
            this.dispatchQueue.Enqueue(updateAction);
        }
    }


    /// <summary>
    /// Spawns a new anchored object.
    /// </summary>
    /// <param name="worldPos">The world position.</param>
    /// <param name="worldRot">The world rotation.</param>
    /// <returns><see cref="GameObject"/>.</returns>
    protected virtual GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot)
    {
        GameObject newGameObject = GameObject.Instantiate(this.AnchoredObjectPrefab, worldPos, worldRot);
        newGameObject.AddARAnchor();

        return newGameObject;
    }

    /// <summary>
    /// Spawns a new object.
    /// </summary>
    /// <param name="worldPos">The world position.</param>
    /// <param name="worldRot">The world rotation.</param>
    /// <param name="cloudSpatialAnchor">The cloud spatial anchor.</param>
    /// <returns><see cref="GameObject"/>.</returns>
    protected virtual GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot, CloudSpatialAnchor cloudSpatialAnchor)
    {
        GameObject newGameObject = this.SpawnNewAnchoredObject(worldPos, worldRot);

#if WINDOWS_UWP || UNITY_WSA
        // On HoloLens, if we do not have a cloudAnchor already, we will have already positioned the
        // object based on the passed in worldPos/worldRot and attached a new world anchor,
        // so we are ready to commit the anchor to the cloud if requested.
        // If we do have a cloudAnchor, we will use it's pointer to setup the world anchor,
        // which will position the object automatically.
        if (cloudSpatialAnchor != null)
        {
            newGameObject.GetComponent<UnityEngine.XR.WSA.WorldAnchor>().SetNativeSpatialAnchorPtr(cloudSpatialAnchor.LocalAnchor);
        }
#endif

        return newGameObject;
    }
}
