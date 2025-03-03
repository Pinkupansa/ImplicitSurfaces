using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public static class MarchingCubes 
{

    static float EvaluatePoint(Vector3Int coord, float gridStep, Vector3 basePoint, ImplicitSurfaceData surface, float[,,] gridPotentials, bool[,,] evaluatedPoints){
        if(!evaluatedPoints[coord.x, coord.y, coord.z]){
            gridPotentials[coord.x, coord.y, coord.z] = surface.EvaluatePotGrad((Vector3) coord*gridStep + basePoint).Item1;
            evaluatedPoints[coord.x, coord.y, coord.z] = true;
        }
        return gridPotentials[coord.x, coord.y, coord.z];
    }
    static float[] EvaluateCube(Vector3Int baseCornerCoord, float gridStep, Vector3 basePoint, ImplicitSurfaceData surface, float[,,] gridPotentials, bool[,,] evaluatedPoints){

        float[] cornerPotentials = new float[8];
        for(int i = 0; i < 8; i++){
            Vector3Int point = baseCornerCoord + MCUtilities.vertexOffsets[i];
            cornerPotentials[i] = EvaluatePoint(point, gridStep, basePoint, surface, gridPotentials, evaluatedPoints);
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

    public static Mesh MarchingCubesCPU(ImplicitSurfaceData surface, Vector3 centerPoint){

		float gridStep = surface.gridStep;
		int gridSize = surface.gridSize;

        Mesh mesh = new Mesh();

        //vertices of the mesh will be on edges

        //table to keep track of the global edge indices in the mesh vertex array
        Dictionary<Edge, int> edgeIndices = new Dictionary<Edge, int>(); 
        
        //positions of the center of edges which will serve as vertices
        List<Vector3> edgesPositions = new List<Vector3>();

        float[,,] gridPotentials = new float[gridSize, gridSize, gridSize];
        bool[,,] evaluatedPoints = new bool[gridSize, gridSize, gridSize];
        //a list of edge indices to read in triplets to obtain the mesh triangles
        List<int> triangles = new List<int>();
        Vector3 basePoint = centerPoint - gridSize/2f * gridStep * Vector3.one;

        for(int x = 0; x < gridSize-1; x++){ //stop iterations at -1 because EvaluateCube looks at +1
            for(int y = 0; y < gridSize-1; y++){ 
                for(int z = 0; z < gridSize-1; z++){
                
                    Vector3Int currentCoord = new Vector3Int(x, y ,z); 
					int cubeCase = IdentifyCubeCase(EvaluateCube(currentCoord, gridStep, basePoint, surface, gridPotentials, evaluatedPoints));
                    //the local indices of the edges forming the triangles
                    int[] localTriangles = MCUtilities.triTable[cubeCase];

                    for(int i = 0; i < localTriangles.Length - 1; i++){ 
                        
                        //calculate the global coords of the edge extremities 
                        int[] edgeExtLocalIndices = MCUtilities.edgeTable[localTriangles[i]];
                        Vector3Int ext1GridCoord = currentCoord + MCUtilities.vertexOffsets[edgeExtLocalIndices[0]];
                        Vector3Int ext2GridCoord = currentCoord + MCUtilities.vertexOffsets[edgeExtLocalIndices[1]];
                        Edge e = new Edge(ext1GridCoord, ext2GridCoord);

                        if(!edgeIndices.ContainsKey(e)){
                            //add this edge as a vertex
                            edgeIndices.Add(e, edgeIndices.Count);
                            edgesPositions.Add((Vector3)(ext1GridCoord + ext2GridCoord)/2f * gridStep + basePoint);
                        }

                        //add the next vertex index of the triangle array
                        triangles.Add(edgeIndices[e]); 
                    }

			    } 
            } 
        }

        mesh.vertices = edgesPositions.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals(); 

        return mesh; 
    }
}

public class Edge{
    Vector3Int u;
    Vector3Int v;

    public Edge(Vector3Int a, Vector3Int b){
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
       return u.GetHashCode() + v.GetHashCode(); 
    }
}