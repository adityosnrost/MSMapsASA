using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;

public class AzureSpatialAnchorsMenuManager : InteractionReceiver
{
    // Start is called before the first frame update
    void Start()
    {
        // Not implemented yet
    }

    protected override void FocusEnter(GameObject targetObject, FocusEventData eventData)
    {
        Debug.Log(targetObject.name + " : InputDown");
    }

    //protected override void InputDown(GameObject obj, InputEventData eventData)
    //{
    //    Debug.Log(obj.name + " : InputDown");

    //    switch (obj.name)
    //    {
    //        case "StartSessionAnchorButton":
    //            //AzureSpatialAnchorsSharedController.Instance.CreateSession();
    //            break;

    //        case "ConfigSessionForQueryButton":
    //            //AzureSpatialAnchorsSharedController.Instance.ConfigSessionForQuery();
    //            break;

    //        case "QueryAnchorButton":
    //            //AzureSpatialAnchorsSharedController.Instance.QueryAnchor();
    //            break;

    //        case "StopQuerySessionButton":
    //            //AzureSpatialAnchorsSharedController.Instance.StopSessionForQuery();
    //            break;

    //        default:
    //            break;
    //    }

    //}
}
