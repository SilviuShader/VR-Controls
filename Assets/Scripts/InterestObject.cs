using System.Collections.Generic;
using UnityEngine;

public class InterestObject : MonoBehaviour
{
    [HideInInspector]
    public        float                Radius = 1.0f;

    public static List<InterestObject> InterestObjects = new();

    public static void Register(InterestObject obj)
    {
        Debug.Assert(
            !InterestObjects.Contains(obj),
            "Duplicate registration of interest object!", obj
        );
        InterestObjects.Add(obj);
    }

    public static void Unregister(InterestObject obj)
    {
        Debug.Assert(
            InterestObjects.Contains(obj),
            "Unregistration of unknown interest object!", obj
        );

        InterestObjects.Remove(obj);
    }

    private void OnEnable() =>
        Register(this);


    private void OnDisable() =>
        Unregister(this);

    private void Update() =>
        Radius = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
}
