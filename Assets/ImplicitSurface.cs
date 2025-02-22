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
    void Start(){
        SkeletonData[] skeletons = GetComponentsInChildren<Skeleton>().Select(x => x.GetData()).ToArray(); 
        data.SetSkeletons(skeletons);

        Mesh mesh = MarchingCubes.MarchingCubesCPU(data, transform.position);
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update(){
        
        Mesh mesh = MarchingCubes.MarchingCubesCPU(data, transform.position);
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void OnDrawGizmos()
    {
        for(int z = -data.halfGridSize; z < data.halfGridSize; z++){

            for(int x = -data.halfGridSize; x < data.halfGridSize; x++){
                Vector3 startPoint = transform.position + data.gridStep*new Vector3(x, -data.halfGridSize, z);
                Vector3 endPoint = startPoint + new Vector3(0, 2*data.halfGridSize - 1, 0)*data.gridStep;
                Gizmos.DrawLine(startPoint, endPoint);
            }
            for(int y = -data.halfGridSize; y < data.halfGridSize; y++){
                Vector3 startPoint = transform.position + data.gridStep*new Vector3(-data.halfGridSize, y, z);
                Vector3 endPoint = startPoint + new Vector3(2*data.halfGridSize - 1,0 , 0)*data.gridStep;
                Gizmos.DrawLine(startPoint, endPoint);
            }
        }
        for(int x = -data.halfGridSize; x < data.halfGridSize; x++){

            for(int y = -data.halfGridSize; y < data.halfGridSize; y++){
                Vector3 startPoint = transform.position + data.gridStep*new Vector3(x, y, -data.halfGridSize);
                Vector3 endPoint = startPoint + new Vector3(0 , 0, 2*data.halfGridSize - 1)*data.gridStep;
                Gizmos.DrawLine(startPoint, endPoint);
            }
        }
    }
}

[System.Serializable]
public class ImplicitSurfaceData{

    public int halfGridSize;
    public float gridStep;

    SkeletonData[] skeletons; 
    
    //add combination rules, adding potentials by default 

    public void SetSkeletons(SkeletonData[] _skeletons){
        skeletons = _skeletons;
    }
    public (float, float) EvaluatePotGrad(Vector3 point){
        float sumPot = 0;
        float sumGrad = 0; 
        foreach(SkeletonData s in skeletons){
           (float, float) potGrad = ISUtilities.EvaluatePotentialAndGradient(s, point); 
           sumPot += potGrad.Item1;
           sumGrad += potGrad.Item2;
        }
        return (sumPot, sumGrad);
    }
}

