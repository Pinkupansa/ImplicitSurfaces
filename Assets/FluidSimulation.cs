
using UnityEngine;

public class FluidSimulation : MonoBehaviour
{
    [SerializeField] float boxSize;
    [SerializeField] int nbParticles; 
    [SerializeField] float springStrength, springLength, gravity, damping;
    

    [SerializeField] GameObject particlePrefab;
    Vector3[] particlePreviousPositions; 
    Vector3[] particlePositions;

    GameObject[] particles;

    void UpdatePosition(int i, Vector3 acceleration)
    {
    
        Vector3 storedPosition = particlePositions[i];
        particlePositions[i] = 2*particlePositions[i] - particlePreviousPositions[i] + Time.deltaTime*Time.deltaTime*acceleration;
        particlePositions[i] = new Vector3(Mathf.Clamp(particlePositions[i].x, -boxSize + transform.position.x, boxSize+ transform.position.x),Mathf.Clamp(particlePositions[i].y, -boxSize+ transform.position.y, boxSize+ transform.position.y), Mathf.Clamp(particlePositions[i].z, -boxSize+ transform.position.z, boxSize+ transform.position.z));
        particlePreviousPositions[i] = storedPosition;
    }

    Vector3 ComputeAcceleration(int i){
        Vector3 acc = Vector3.zero;
        for(int j = 0; j < nbParticles; j++){
            if(j == i) continue;
            Vector3 dir = particlePositions[i] - particlePositions[j];
            acc += springStrength*(dir.normalized*springLength - dir)/(dir.sqrMagnitude + 0.1f);
        }

        acc += damping*(particlePreviousPositions[i] - particlePositions[i])/Time.deltaTime; 
        acc += -gravity*Vector3.up;
        return acc;
    }
    void Step()
    {
        for(int i = 0; i < nbParticles; i++){ 
            Vector3 acceleration = ComputeAcceleration(i);
            UpdatePosition(i, acceleration);
        }
    }
    void Awake()
    {
        particlePositions = new Vector3[nbParticles];
        particlePreviousPositions = new Vector3[nbParticles];
        particles = new GameObject[nbParticles];
        for(int i = 0; i < nbParticles; i++){
            particlePositions[i] = new Vector3(Random.Range(-boxSize, boxSize), Random.Range(-boxSize, boxSize), Random.Range(-boxSize, boxSize)) + transform.position;
            particlePreviousPositions[i] = particlePositions[i];
            particles[i] = Instantiate(particlePrefab, particlePositions[i], Quaternion.identity);
            particles[i].transform.SetParent(transform);
        }
    }

    void UpdateGraphics(){
        for(int i = 0; i < nbParticles; i++){
            particles[i].transform.position = particlePositions[i];
        }
    }
    void Update()
    {
        Step();
        UpdateGraphics();
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, Vector3.one * boxSize * 2);
    }
}

