using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;

[BurstCompile]
struct BackgroundNoiseParallelForTRSJob : IJobParallelFor
{
    public float ElapsedTime;
    [ReadOnly]
    public NativeArray<Vector3> OriginPositions;
    [WriteOnly]
    public NativeArray<Matrix4x4> TRSArray;

    public void Execute(int index)
    {
        var p = OriginPositions[index];
        var sinx = Mathf.Sin(1f * ElapsedTime + Perlin.Noise(0.3f * p.x + ElapsedTime));
        var siny = Mathf.Cos(1f * ElapsedTime + Perlin.Noise(0.5f * p.y + ElapsedTime));
        var sinz = Mathf.Sin(1f * ElapsedTime + Perlin.Noise(0.7f * p.z - ElapsedTime));
        var offset = 2f * new Vector3(sinx, siny, sinz);
        TRSArray[index] = Matrix4x4.TRS(p + offset, Quaternion.identity, Vector3.one);
    }
}

public class ParallelForJobInstancingDemo : MonoBehaviour
{

    public Material InstancedMaterial;
    public Mesh InstancedMesh;
    public int WorldEdgeSize;
    private JobHandle m_jobHandle;
    private NativeArray<Vector3> m_nativeOffsets;
    private NativeArray<Vector3> m_nativePositions;
    private Matrix4x4[] m_managedTRS;
    private NativeArray<Matrix4x4> m_nativeTRS;

    void OnEnable()
    {
        var totalCount = WorldEdgeSize * WorldEdgeSize * WorldEdgeSize;
        m_nativePositions = new NativeArray<Vector3>(totalCount, Allocator.Persistent);
        m_nativeTRS = new NativeArray<Matrix4x4>(totalCount, Allocator.Persistent);
        m_managedTRS = new Matrix4x4[totalCount];

        var index = 0;
        for (int x = 0; x < WorldEdgeSize; x++)
        {
            for (int y = 0; y < WorldEdgeSize; y++)
            {
                for (int z = 0; z < WorldEdgeSize; z++)
                {
                    var pos = new Vector3(x, y, z) * 5f - new Vector3(WorldEdgeSize * 5f * 0.5f, WorldEdgeSize * 5f * 0.5f, 0);
                    m_nativePositions[index] = pos;
                    m_nativeTRS[index] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
                    index++;
                }
            }
        }
    }

    void Update()
    {
        var noiseJob = new BackgroundNoiseParallelForTRSJob
        {
            ElapsedTime = Time.time,
            OriginPositions = m_nativePositions,
            TRSArray = m_nativeTRS,
        };
        m_jobHandle = noiseJob.Schedule(WorldEdgeSize * WorldEdgeSize * WorldEdgeSize, 1, m_jobHandle);
    }

    void LateUpdate()
    {
        m_jobHandle.Complete();
        m_nativeTRS.CopyTo(m_managedTRS);

        Graphics.DrawMeshInstanced(InstancedMesh, 0, InstancedMaterial, m_managedTRS, m_managedTRS.Length);
    }

    void OnDisable()
    {
        m_nativeTRS.Dispose();
        m_nativePositions.Dispose();
    }
}