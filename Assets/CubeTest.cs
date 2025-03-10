using UnityEngine;

public class CubeTest : MonoBehaviour
{
    
    void Start()
    {

        for(int i = 0; i < 8; i++){
            string name = "Vertex " + i.ToString();
            DrawingUtilities.instance.DrawPrimitive(PrimitiveType.Sphere, MCUtilities.vertexOffsets[i], 0.1f*Vector3.one, name);
            DrawingUtilities.instance.LabelObject(name, i.ToString());
        }
        DrawingUtilities.instance.currentColor = Color.green;
        
        for(int i = 0; i < 12; i ++){ 
            string name = "Edge " + i.ToString();
            int[] vertices = MCUtilities.edgeTable[i];
            string name1 = "Vertex " + vertices[0].ToString();
            string name2 = "Vertex " + vertices[1].ToString();

            DrawingUtilities.instance.DrawLine(name1, name2, 0.03f, name);

            DrawingUtilities.instance.currentColor = Color.red;
            DrawingUtilities.instance.DrawPrimitive(PrimitiveType.Sphere, MCUtilities.edgeOffsets[i], 0.09f*Vector3.one, name + " center");
            DrawingUtilities.instance.LabelObject(name + " center", i.ToString() + " " + MCUtilities.localIndexInNeighbours[i][0].ToString() + " " + MCUtilities.localIndexInNeighbours[i][1].ToString() + " " + MCUtilities.localIndexInNeighbours[i][2].ToString());

            DrawingUtilities.instance.currentColor = Color.red; 
            DrawingUtilities.instance.DrawLine(MCUtilities.edgeOffsets[i], MCUtilities.edgeOffsets[i] + (Vector3)MCUtilities.neighboursWithCommonEdge[i][0] * 0.25f, 0.01f, name + " l1");
            DrawingUtilities.instance.currentColor = Color.green; 
            DrawingUtilities.instance.DrawLine(MCUtilities.edgeOffsets[i], MCUtilities.edgeOffsets[i] + (Vector3)MCUtilities.neighboursWithCommonEdge[i][1] * 0.25f, 0.01f, name + " l2");
            DrawingUtilities.instance.currentColor = Color.blue; 
            DrawingUtilities.instance.DrawLine(MCUtilities.edgeOffsets[i], MCUtilities.edgeOffsets[i] + (Vector3)MCUtilities.neighboursWithCommonEdge[i][2] * 0.25f, 0.01f, name + " l3");
            DrawingUtilities.instance.currentColor = Color.green;
        }

        //DrawCubeCase(134);
        
    }

    void DrawCubeCase(int i){
        DrawingUtilities.instance.highlightedColor = Color.yellow;
        for(int k=0; k < 8; k++){ 
            int positive = (i>>k)&1; 
            if(positive == 1){
                DrawingUtilities.instance.HighlightObject("Vertex " + k);
            }
        }

        int[] triangles = MCUtilities.triTable[i];
        int j = 0;
        DrawingUtilities.instance.currentColor = Color.red;
        while(triangles[j] != -1){
            Vector3 pos1 = MCUtilities.edgeOffsets[triangles[j]];
            Vector3 pos2 = MCUtilities.edgeOffsets[triangles[j+1]];
            Vector3 pos3 = MCUtilities.edgeOffsets[triangles[j+2]];

            DrawingUtilities.instance.DrawTriangle(pos1, pos2, pos3);
            j += 3;
        }
    }
    
}
