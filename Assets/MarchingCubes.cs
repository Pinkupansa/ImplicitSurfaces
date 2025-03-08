using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public static class MarchingCubes 
{
    static float ISOPOTENTIAL = 0.1f;

    static float gridStep;
    static int gridSize; 
    static Vector3 basePoint; 
    static ImplicitSurfaceData surface; 
    static float[,,] gridPotentials; 
    static bool[,,] evaluatedPoints;
    static Dictionary<Edge, int> edgeIndices;
    static List<Vector3> edgePositions;
    static List<int> triangles;

    
    static float EvaluatePoint(GridCoord coord){
        if(!evaluatedPoints[coord.x, coord.y, coord.z]){
            gridPotentials[coord.x, coord.y, coord.z] = surface.EvaluatePot((Vector3)coord*gridStep + basePoint) - ISOPOTENTIAL;
            evaluatedPoints[coord.x, coord.y, coord.z] = true;
        }
        return gridPotentials[coord.x, coord.y, coord.z];
    }
    static float[] EvaluateCube(GridCoord baseCornerCoord){

        float[] cornerPotentials = new float[8];
        for(int i = 0; i < 8; i++){
            GridCoord point = new GridCoord(baseCornerCoord.x + MCUtilities.vertexOffsets[i].x, baseCornerCoord.y + MCUtilities.vertexOffsets[i].y, baseCornerCoord.z + MCUtilities.vertexOffsets[i].z);
            cornerPotentials[i] = EvaluatePoint(point);
        }
        return cornerPotentials;
    }
    
    static int IdentifyCubeCase(float[] potentials){

        int cubeCase = 0;
        for(int i = 7; i >= 0; i--){
           cubeCase *= 2;
           cubeCase += potentials[i] > 0 ? 1 : 0;
        }

        return cubeCase;
    }

    static Vector3 InterpolateMeshVertexPosition(GridCoord gc1, GridCoord gc2){

        //evaluate potentials (if not already done) and interpolate border placement
        float pot1 = EvaluatePoint(gc1);
        float pot2 = EvaluatePoint(gc2);
        return Vector3.Lerp(gc1, gc2, Mathf.Abs(pot1/(pot1-pot2))) * gridStep + basePoint;
    }

    static void AddTriangles(int[] localTriangles, GridCoord currentCoord){

        for(int i = localTriangles.Length - 2; i >= 0; i--){ 
            
            //calculate the global coords of the edge extremities 
            int[] edgeExtLocalIndices = MCUtilities.edgeTable[localTriangles[i]];
            GridCoord ext1GridCoord = currentCoord + MCUtilities.vertexOffsets[edgeExtLocalIndices[0]];
            GridCoord ext2GridCoord = currentCoord + MCUtilities.vertexOffsets[edgeExtLocalIndices[1]];
            Edge e = new Edge(ext1GridCoord, ext2GridCoord);

            if(!edgeIndices.ContainsKey(e)){
                //add this edge as a vertex
                edgeIndices.Add(e, edgeIndices.Count);
                edgePositions.Add(InterpolateMeshVertexPosition(ext1GridCoord, ext2GridCoord));
            }

            //add the next vertex index of the triangle array
            triangles.Add(edgeIndices[e]); 
        }
    }
    public static Mesh MarchingCubesCPU(ImplicitSurfaceData _surface, Vector3 centerPoint){
        surface = _surface;
        gridStep = surface.gridStep;
        gridSize = surface.gridSize;

        Mesh mesh = new Mesh();

        //vertices of the mesh will be on edges

        //table to keep track of the global edge indices in the mesh vertex array
        edgeIndices = new Dictionary<Edge, int>(); 
        
        //positions of the center of edges which will serve as vertices
        edgePositions = new List<Vector3>();

        gridPotentials = new float[gridSize, gridSize, gridSize];
        evaluatedPoints = new bool[gridSize, gridSize, gridSize];
        //a list of edge indices to read in triplets to obtain the mesh triangles
        triangles = new List<int>();
        basePoint = centerPoint - gridSize/2f * gridStep * Vector3.one;

        for(int x = 0; x < gridSize-1; x++){ //stop iterations at -1 because EvaluateCube looks at +1
            for(int y = 0; y < gridSize-1; y++){ 
                for(int z = 0; z < gridSize-1; z++){
                
                    GridCoord currentCoord = new GridCoord(x, y ,z); 
                    int cubeCase = IdentifyCubeCase(EvaluateCube(currentCoord));
                    //the local indices of the edges forming the triangles
                    int[] localTriangles = MCUtilities.triTable[cubeCase];

                    AddTriangles(localTriangles, currentCoord);
                }
            } 
        }

            mesh.vertices = edgePositions.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals(); 

            return mesh; 
        }
    }

public class Edge{
    GridCoord u;
    GridCoord v;

    public Edge(GridCoord a, GridCoord b){
        u = a;
        v = b;
    }

    public override bool Equals(object obj)
    {
        if(!(obj is Edge)){
            return false;
        }
        Edge edge = (Edge)obj;

        return (u == edge.u && v == edge.v) || (u == edge.v && v == edge.u);
    }

    public override int GetHashCode()
    {
       return u.GetHashCode() * v.GetHashCode(); 
    }
}


// Define a struct with better equality comparison
public struct GridCoord : IEquatable<GridCoord> {
    public int x, y, z;
    
    public GridCoord(int _x, int _y, int _z) {
        x = _x; y = _y; z = _z;
    }
    
    public override bool Equals(object obj) => 
        obj is GridCoord other && Equals(other);
    
    public bool Equals(GridCoord other) => 
        x == other.x && y == other.y && z == other.z;
    
    public override int GetHashCode() => 
        HashCode.Combine(x, y, z);
    
    public static GridCoord operator +(GridCoord a, GridCoord b) => new GridCoord(a.x+b.x, a.y+b.y, a.z + b.z);
    public static bool operator == (GridCoord a, GridCoord b) => a.Equals(b);
    public static bool operator != (GridCoord a, GridCoord b) => !a.Equals(b);
    public static implicit operator Vector3(GridCoord coord){
        return new Vector3(coord.x, coord.y, coord.z);
    }
    
    // Explicit conversion from Vector3 to GridCoord - requires explicit cast
    public static explicit operator GridCoord(Vector3 vec)
    {
        return new GridCoord((int)vec.x, (int)vec.y, (int)vec.z);
    }

}