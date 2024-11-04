using UnityEngine;

using System.Runtime.InteropServices;
using UnityEngine.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct Particle
{
    public Vector3 Position;
    public Vector3 PreviousPosition;
    public Vector3 Velocity;
    public float Radius;
}

public class ParticleSystemController : MonoBehaviour
{
    public ComputeShader computeShader;
    public int particleCount = 1000;
    public float cellSize = 1.0f;
    public int gridSize = 10;
    public GameObject particlePrefab;
    public float circleRadius = 100.0f;

    private Particle[] particles;
    private ComputeBuffer particleBuffer;
    private ComputeBuffer gridBuffer;
    private ComputeBuffer gridCountsBuffer;
    private GameObject[] particleObjects;

    void Start()
    {
        // Initialize particles and GameObjects
        particles = new Particle[particleCount];
        particleObjects = new GameObject[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            GameObject obj = Instantiate(particlePrefab, Random.insideUnitSphere * 10, Quaternion.identity);
            particles[i] = new Particle
            {
                Position = obj.transform.position,
                PreviousPosition = obj.transform.position,
                Velocity = Random.insideUnitSphere,
                Radius = 0.1f
            };
            particleObjects[i] = obj;
        }

        // Initialize compute buffers
        int particleSize = Marshal.SizeOf(typeof(Particle));
        particleBuffer = new ComputeBuffer(particleCount, particleSize);
        particleBuffer.SetData(particles);

        gridBuffer = new ComputeBuffer(gridSize * gridSize * 256, sizeof(int), ComputeBufferType.Raw);
        gridCountsBuffer = new ComputeBuffer(gridSize * gridSize, sizeof(int), ComputeBufferType.Raw);

        // Set compute shader parameters
        computeShader.SetBuffer(0, "particles", particleBuffer);
        computeShader.SetBuffer(0, "grid", gridBuffer);
        computeShader.SetBuffer(0, "gridCounts", gridCountsBuffer);
        computeShader.SetInt("gridSize", gridSize);
        computeShader.SetFloat("cellSize", cellSize);
        computeShader.SetFloat("circleRadius", circleRadius);
    }

    void Update()
    {
        // Clear grid counts
        int[] gridCounts = new int[gridSize * gridSize];
        gridCountsBuffer.SetData(gridCounts);

        // Set delta time
        computeShader.SetFloat("deltaTime", Time.deltaTime);

        // Dispatch compute shader
        int threadGroups = Mathf.CeilToInt(particleCount / 256.0f);
        computeShader.Dispatch(0, threadGroups, 1, 1);

        // Asynchronous readback
        AsyncGPUReadback.Request(particleBuffer, OnCompleteReadback);
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.LogError("GPU readback error");
            return;
        }

        // Retrieve updated particle data
        request.GetData<Particle>().CopyTo(particles);

        // Update particle GameObjects
        for (int i = 0; i < particleCount; i++)
        {
            Particle particle = particles[i];
            GameObject obj = particleObjects[i];
            obj.transform.position = particle.Position;
            ParticleController particleComponent = obj.GetComponent<ParticleController>();
            particleComponent.PreviousPosition = particle.PreviousPosition;
            particleComponent.Velocity = particle.Velocity;
        }
    }

    void OnDestroy()
    {
        // Release compute buffers
        particleBuffer.Release();
        gridBuffer.Release();
        gridCountsBuffer.Release();
    }
}