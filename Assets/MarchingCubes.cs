using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;

public static class MarchingCubes 
{
	
    static float[] EvaluateCube(Vector3 baseCorner, float gridStep, ImplicitSurfaceData surface){

        float[] cornerPotentials = new float[8];
        for(int i = 0; i < 8; i++){
            Vector3 point = baseCorner + (Vector3)MCUtilities.vertexOffsets[i] * gridStep;
            cornerPotentials[i] = surface.EvaluatePotGrad(point).Item1;
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

    public static Mesh MarchingCubesCPU(ImplicitSurfaceData surface, Vector3 basePoint){

		float gridStep = surface.gridStep;
		int gridSize = surface.halfGridSize;

        Mesh mesh = new Mesh();

        //vertices of the mesh will be on edges

        //table to keep track of the global edge indices in the mesh vertex array
        Dictionary<Edge, int> edgeIndices = new Dictionary<Edge, int>(); 
        
        //positions of the center of edges which will serve as vertices
        List<Vector3> edgesPositions = new List<Vector3>();

        //a list of edge indices to read in triplets to obtain the mesh triangles
        List<int> triangles = new List<int>(); 

        for(int x = -gridSize; x < gridSize; x++){
            for(int y = -gridSize; y < gridSize; y++){ 
                for(int z = -gridSize; z < gridSize; z++){
                
                    Vector3Int currentCoord = new Vector3Int(x, y ,z); 
                    Vector3 currentPoint = (Vector3)currentCoord*gridStep + basePoint;
					int cubeCase = IdentifyCubeCase(EvaluateCube(currentPoint, gridStep, surface));
				 	Debug.Log(cubeCase);
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