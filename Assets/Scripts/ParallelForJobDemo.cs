using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;

[BurstCompile]
struct BackgroundNoiseParallelForJob : IJobParallelFor
{
    public float ElapsedTime;
    public float DeltaTime;
    [WriteOnly]
    public NativeArray<Vector3> Offsets;
    [ReadOnly]
    public NativeArray<Vector3> OriginPositions;

    public void Execute(int index)
    {
        var p = OriginPositions[index];
        var sinx = Mathf.Sin(1f * ElapsedTime + Perlin.Noise(0.3f * p.x + ElapsedTime));
        var siny = Mathf.Cos(1f * ElapsedTime + Perlin.Noise(0.5f * p.y + ElapsedTime));
        var sinz = Mathf.Sin(1f * ElapsedTime + Perlin.Noise(0.7f * p.z - ElapsedTime));
        Offsets[index] = 2f * new Vector3(sinx, siny, sinz);
    }
}

public class ParallelForJobDemo : MonoBehaviour
{
    public int WorldEdgeSize;
    private Transform[] m_cubes;
    private JobHandle m_jobHandle;
    private NativeArray<Vector3> m_nativeOffsets;
    private NativeArray<Vector3> m_nativePositions;

    void OnEnable()
    {
        m_cubes = new Transform[WorldEdgeSize * WorldEdgeSize * WorldEdgeSize];
        m_nativePositions = new NativeArray<Vector3>(m_cubes.Length, Allocator.Persistent);
        m_nativeOffsets = new NativeArray<Vector3>(m_cubes.Length, Allocator.Persistent);

        var index = 0;
        for (int x = 0; x < WorldEdgeSize; x++)
        {
            for (int y = 0; y < WorldEdgeSize; y++)
            {
                for (int z = 0; z < WorldEdgeSize; z++)
                {
                    m_cubes[index] = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                    m_cubes[index].position = new Vector3(x, y, z) * 5f - new Vector3(WorldEdgeSize * 5f * 0.5f, WorldEdgeSize * 5f * 0.5f, 0);
                    m_nativePositions[index] = m_cubes[index].position;
                    index++;
                }
            }
        }
    }

    void Update()
    {
        var noiseJob = new BackgroundNoiseParallelForJob
        {
            ElapsedTime = Time.time,
            DeltaTime = Time.deltaTime,
            Offsets = m_nativeOffsets,
            OriginPositions = m_nativePositions,
        };
        m_jobHandle = noiseJob.Schedule(m_cubes.Length, 1, m_jobHandle);
    }

    void LateUpdate()
    {
        m_jobHandle.Complete();
        for (int i = 0; i < m_nativeOffsets.Length; i++)
        {
            m_cubes[i].position = m_nativePositions[i] + m_nativeOffsets[i];
        }
    }

    void OnDisable()
    {
        m_nativeOffsets.Dispose();
        m_nativePositions.Dispose();
        for (int i = 0; i < m_cubes.Length; i++)
        {
            Destroy(m_cubes[i].gameObject);
        }
    }
}