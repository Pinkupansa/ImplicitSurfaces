using System.Collections.Generic;
using System.IO.Compression;
using NUnit.Framework;
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
    static int[,,,] edgeIndices;
    static List<Vector3> edgePositions;
    static List<int> triangles;
    static bool[,,] visitedCubes; 

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

            int edgeIndex = -1;
            if(!FindOrAddEdgeMeshIndex(currentCoord, localTriangles[i], out edgeIndex)){
                GridCoord ext1GridCoord = currentCoord + MCUtilities.vertexOffsets[edgeExtLocalIndices[0]];
                GridCoord ext2GridCoord = currentCoord + MCUtilities.vertexOffsets[edgeExtLocalIndices[1]];
                edgePositions.Add(InterpolateMeshVertexPosition(ext1GridCoord, ext2GridCoord));
            }
            //add the next vertex index of the triangle array
            triangles.Add(edgeIndex - 1); 
        }
    }
     

    static bool FindOrAddEdgeMeshIndex(GridCoord cube, int edgeLocalIndex, out int edgeIndex){ 
        for(int i = 0; i < 3; i++){
            GridCoord neighbour = cube + MCUtilities.neighboursWithCommonEdge[edgeLocalIndex][i];
            if(!IsInsideGrid(neighbour) || !visitedCubes[neighbour.x, neighbour.y, neighbour.z]) continue; 

            int index = edgeIndices[neighbour.x, neighbour.y, neighbour.z, MCUtilities.localIndexInNeighbours[edgeLocalIndex][i]];
            if(index != 0){
                edgeIndex = index;
                return true;
            }
        }
        //edge index has not been found 
        int newIndex = edgePositions.Count + 1;
        edgeIndices[cube.x, cube.y, cube.z, edgeLocalIndex] = newIndex;
        edgeIndex = newIndex;
        return false; 
    }

    public static Mesh MarchingCubesCPU(ImplicitSurfaceData _surface, Vector3 centerPoint){
        surface = _surface;
        gridStep = surface.gridStep;
        gridSize = surface.gridSize;

        Mesh mesh = new Mesh();

        
        //vertices of the mesh will be on edges

        //table to keep track of the global edge indices in the mesh vertex array
        edgeIndices = new int[gridSize, gridSize, gridSize, 12];
        
        //positions of the center of edges which will serve as vertices
        edgePositions = new List<Vector3>();

        gridPotentials = new float[gridSize, gridSize, gridSize];
        evaluatedPoints = new bool[gridSize, gridSize, gridSize];

        //a list of edge indices to read in triplets to obtain the mesh triangles
        triangles = new List<int>();
        basePoint = centerPoint - gridSize/2f * gridStep * Vector3.one;
        visitedCubes = new bool[gridSize, gridSize, gridSize];

        for(int i = 0; i < surface.GetNumberOfSkeletons(); i++){
            GenerateConnectedComponent(FindComponentStartingCube(PositionToGridCoord(surface.GetSkeleton(i).position)));
        }

        mesh.vertices = edgePositions.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals(); 

        return mesh; 
    }
    
    static GridCoord PositionToGridCoord(Vector3 position){
        Vector3 v = (position - basePoint)/gridStep; 
        return new GridCoord(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
    }
    static bool IsFullyInsideGrid(GridCoord coord){
        return coord.x > 0 && coord.x <= gridSize - 2 && coord.y > 0 && coord.y <= gridSize - 2 && coord.z > 0 && coord.z <= gridSize - 2;
    }
    static bool IsInsideGrid(GridCoord coord){

        return coord.x >= 0 && coord.x <= gridSize - 1 && coord.y >= 0 && coord.y <= gridSize - 1 && coord.z >= 0 && coord.z <= gridSize - 1;
    }
    static GridCoord FindComponentStartingCube(GridCoord insidePoint){ 
        
        for(int i = 1; i < gridSize; i++){
            GridCoord trialPointX = new GridCoord(insidePoint.x + i, insidePoint.y, insidePoint.z);
            if(IsInsideGrid(trialPointX) && EvaluatePoint(trialPointX) < 0){
                return new GridCoord(insidePoint.x + i - 1, insidePoint.y, insidePoint.z);
            }
        }
        return new GridCoord(0, 0, 0);
    }
    static void GenerateConnectedComponent(GridCoord startingCube){
        if(!IsFullyInsideGrid(startingCube)) return;
        Queue<GridCoord> cubeQueue = new Queue<GridCoord>(); 
        cubeQueue.Enqueue(startingCube);

        
        while(cubeQueue.Count > 0){
            GridCoord currentCoord = cubeQueue.Dequeue();
            if(!visitedCubes[currentCoord.x, currentCoord.y, currentCoord.z]){
                int[] localTriangles = MCUtilities.triTable[IdentifyCubeCase(EvaluateCube(currentCoord))];
                if(localTriangles[0] != -1){
                    //todo : Add triangles and Enqueue neighbours 
                    AddTriangles(localTriangles, currentCoord);
                    for(int x = -1; x <= 1; x ++)
                        for(int y = -1; y <= 1; y ++)
                            for(int z = -1;  z <= 1; z++){
                                if(x == 0 && y == 0 && z == 0) continue;
                                GridCoord newCoord = new GridCoord(currentCoord.x + x, currentCoord.y + y, currentCoord.z + z);
                                if(IsFullyInsideGrid(newCoord) && !visitedCubes[newCoord.x, newCoord.y, newCoord.z]){
                                    cubeQueue.Enqueue(newCoord);
                                }
                            }
                }
                visitedCubes[currentCoord.x, currentCoord.y, currentCoord.z] = true;
            }
        }
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
       return u.GetHashCode() + v.GetHashCode();
    }
}
