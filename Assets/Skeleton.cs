using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEditor;
using UnityEngine;


public class Skeleton : MonoBehaviour{

    [SerializeField] bool debugDraw = false;
    [SerializeField] SkeletonData data;

    public SkeletonData GetData(){
        return data;
    }
    void Update(){
        data.SetPos(transform.position);
    }

    void OnDrawGizmos(){
        //Simply draw the radius of influence
        if(!debugDraw) return; 


        float r = ISUtilities.DichotomicSearch(data);
        if (r < 0) debugDraw = false;

        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(data.position, r*data.scale);
    }

}


[Serializable]
public class SkeletonData
{
    public float isoValue;
    public float scale; 
    public ISUtilities.POTENTIAL_FUNCTION potentialFunction;
    public Vector3 position{get; private set;}
    public void SetPos(Vector3 _pos){
        position = _pos;
    }
    
}
