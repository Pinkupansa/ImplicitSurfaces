using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ImplicitSurface : MonoBehaviour
{
    [SerializeField] ImplicitSurfaceData data; 
    [SerializeField] bool debugDraw = false;
    [SerializeField] Material material;
    void Start(){
        SkeletonData[] skeletons = GetComponentsInChildren<Skeleton>().Select(x => x.GetData()).ToArray();
        Debug.Log(skeletons.Length); 
        data.SetSkeletons(skeletons);

    }

    void Update(){
        
        Mesh mesh = MarchingCubes.MarchingCubesCPU(data, transform.position);
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    void OnDrawGizmos()
    {
        if(!debugDraw) return;
        for(int z = -data.gridSize/2; z < data.gridSize/2f; z++){

            for(int x = -data.gridSize/2; x < data.gridSize/2f; x++){
                Vector3 startPoint = transform.position + data.gridStep*new Vector3(x, -data.gridSize/2, z);
                Vector3 endPoint = startPoint + new Vector3(0, data.gridSize, 0)*data.gridStep;
                Gizmos.DrawLine(startPoint, endPoint);
            }
            for(int y = -data.gridSize/2; y < data.gridSize/2f; y++){
                Vector3 startPoint = transform.position + data.gridStep*new Vector3(-data.gridSize/2, y, z);
                Vector3 endPoint = startPoint + new Vector3(data.gridSize,0 , 0)*data.gridStep;
                Gizmos.DrawLine(startPoint, endPoint);
            }
        }
        for(int x = -data.gridSize/2; x < data.gridSize/2f; x++){

            for(int y = -data.gridSize/2; y < data.gridSize/2f; y++){
                Vector3 startPoint = transform.position + data.gridStep*new Vector3(x, y, -data.gridSize/2);
                Vector3 endPoint = startPoint + new Vector3(0 , 0, data.gridSize)*data.gridStep;
                Gizmos.DrawLine(startPoint, endPoint);
            }
        }
    }
}

[System.Serializable]
public class ImplicitSurfaceData{

    public int gridSize;
    public float gridStep;

    SkeletonData[] skeletons; 
    
    //add combination rules, adding potentials by default 

    public void SetSkeletons(SkeletonData[] _skeletons){
        skeletons = _skeletons;
    }
    public SkeletonData GetSkeleton(int i){
        return skeletons[i];
    }
    public int GetNumberOfSkeletons(){
        return skeletons.Length;
    }
    public float EvaluatePot(Vector3 point){
        float sumPot = 0;
        foreach(SkeletonData s in skeletons){
            float d = Vector3.Distance(point, s.position)/s.scale;

            float pot = ISUtilities.EvaluateQuarticMetaballPotential(d*d); 
            sumPot += pot;
        }
        return sumPot;
    }
}

