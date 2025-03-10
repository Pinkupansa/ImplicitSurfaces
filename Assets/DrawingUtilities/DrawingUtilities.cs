using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using NUnit.Framework.Constraints;
public class DrawingUtilities : MonoBehaviour{

    public static DrawingUtilities instance; 

    [SerializeField] Material defaultMaterial;
    public Color currentColor;
    public Color highlightedColor;

    Dictionary<string, GameObject> drawings = new Dictionary<string, GameObject>(); 
    Dictionary<string, TMP_Text> labels = new Dictionary<string, TMP_Text>();

    [SerializeField] GameObject textPrefab;
    
    Dictionary<string, Color> highlightedObjectsBaseColor = new Dictionary<string, Color>();

    void Awake(){
        if(instance == null) instance = this;
    }
    public void DrawPrimitive(PrimitiveType type, Vector3 position, Vector3 scale, string name = ""){
        if(name.Length == 0){
            name = drawings.Count.ToString();
        }
        GameObject prim = GameObject.CreatePrimitive(type);
        drawings.Add(name, prim);
        prim.transform.localScale = scale;
        prim.transform.position = position;
        prim.GetComponent<MeshRenderer>().sharedMaterial = defaultMaterial;
        prim.GetComponent<MeshRenderer>().materials[0].color = currentColor;
        
    }

    public void Clear(){
        Dictionary<string, GameObject> drawingsCopy = drawings.ToDictionary(entry => entry.Key, entry => entry.Value);
        foreach(string name in drawingsCopy.Keys){
            DestroyDrawing(name);
        }
    }
    public void DestroyDrawing(string name){
        if(!drawings.ContainsKey(name)) return;
        Destroy(drawings[name]);
        drawings.Remove(name);

        if(!labels.ContainsKey(name)) return;
        Destroy(labels[name]);
        labels.Remove(name);

        if(!highlightedObjectsBaseColor.ContainsKey(name)) return;
        highlightedObjectsBaseColor.Remove(name);
    }
    public void ScaleObject(string name, float amount){
        
        if(!drawings.ContainsKey(name)) return;
        GameObject drawing = drawings[name];

        drawings[name].transform.localScale *= amount;
    }

    public void DisplaceObject(string name, Vector3 pos){
        if(!drawings.ContainsKey(name)) return;
        drawings[name].transform.position = pos;
        if(!labels.ContainsKey(name)) return;
        labels[name].gameObject.transform.position = drawings[name].transform.position + Vector3.up*drawings[name].transform.localScale.y;
    }
    
    public void DrawLine(Vector3 pos1, Vector3 pos2, float thickness, string name = ""){
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = (pos1 + pos2)/2f;
        cube.transform.right = pos2 - pos1; 
        cube.transform.localScale = new Vector3(Vector3.Distance(pos1, pos2), thickness, thickness);

        drawings.Add(name, cube);
        cube.GetComponent<MeshRenderer>().sharedMaterial = defaultMaterial;
        cube.GetComponent<MeshRenderer>().materials[0].color = currentColor;
    }

    public void LabelObject(string name, string label){
        if(!drawings.ContainsKey(name)) return;
        if(labels.ContainsKey(name)) return;
        GameObject drawing = drawings[name];

        GameObject text = Instantiate(textPrefab);
        text.transform.position = drawing.transform.position + Vector3.up*drawing.transform.localScale.y;
        text.GetComponent<TMP_Text>().text = label; 
        labels.Add(name, text.GetComponent<TMP_Text>());
        text.transform.localScale *= drawing.transform.localScale.x*2;
    }

    public void DrawLine(string name1, string name2, float thickness, string name = ""){
        GameObject drawing1 = drawings[name1];
        GameObject drawing2 = drawings[name2];
        if(drawing1 == null || drawing2 == null) return;

        DrawLine(drawing1.transform.position, drawing2.transform.position, thickness, name);
    }

    public void DrawTriangle(Vector3 pos1, Vector3 pos2, Vector3 pos3, string name = ""){
        if(name.Length == 0){
            name = drawings.Count.ToString();
        }

        Mesh triangle = new Mesh();
        triangle.vertices = new Vector3[3]{pos1, pos2, pos3};
        triangle.triangles = new int[]{0, 1, 2};

        GameObject gameObject = new GameObject();
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        MeshFilter filter = gameObject.AddComponent<MeshFilter>();

        filter.mesh = triangle;

        renderer.sharedMaterial = defaultMaterial;
        renderer.materials[0].color = currentColor;
    }

    public void ModifyColor(string name, Color color){

        if(!drawings.ContainsKey(name)) return;
        GameObject drawing = drawings[name];
        drawing.GetComponent<MeshRenderer>().materials[0].color = color;
    }

    public void HighlightObject(string name){ 
        if(!drawings.ContainsKey(name)) return;
        GameObject drawing = drawings[name];

        Color baseColor = drawing.GetComponent<MeshRenderer>().materials[0].color;

        highlightedObjectsBaseColor.Add(name, baseColor);
        ModifyColor(name, highlightedColor); 
    }

    public void ClearHighlighting(){
        foreach(KeyValuePair<string, Color> kvp in highlightedObjectsBaseColor){
            ModifyColor(kvp.Key, kvp.Value);
        }
        highlightedObjectsBaseColor = new Dictionary<string, Color>();
    }
}