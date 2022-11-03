using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;

[BurstCompile]
struct HighlightSelectionJob : IJobParallelForDefer
{
    [ReadOnly]
    public NativeList<int> Indexes;
    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<Color> Colors;
    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<Vector3> Scales;

    public void Execute(int i)
    {
        Scales[Indexes[i]] = Vector3.one * 1.5f;
        Colors[Indexes[i]] = Color.green;
    }
}

[BurstCompile]
struct RestoreUnselectionJob : IJobParallelForDefer
{
    [ReadOnly]
    public NativeList<int> Indexes;
    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<Color> Colors;
    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<Vector3> Scales;

    public void Execute(int i)
    {
        Scales[Indexes[i]] = Vector3.one;
        Colors[Indexes[i]] = Color.white;
    }
}

[BurstCompile]
struct CheckBoundsFilterJob : IJobParallelForFilter
{
    public Bounds Bounds;
    public bool CheckIsInside;
    [ReadOnly]
    public NativeArray<Vector3> Offsets;
    [ReadOnly]
    public NativeArray<Vector3> Positions;

    bool IJobParallelForFilter.Execute(int index)
    {
        var pos = Offsets[index] + Positions[index];
        return (CheckIsInside == Bounds.Contains(pos));
    }
}

public class ParallelForFilterJobDemo : MonoBehaviour
{
    public BoxCollider SelectionCollider;
    public int WorldEdgeSize;
    private Transform[] Cubes;
    private JobHandle m_jobHandle;
    private NativeArray<Vector3> m_nativeOffsets;
    private NativeArray<Vector3> m_nativePositions;
    private NativeArray<Color> m_nativeColors;
    private NativeArray<Vector3> m_nativeScales;
    private MaterialPropertyBlock m_matPropBlock;

    void OnEnable()
    {
        if (!SelectionCollider.gameObject.activeSelf)
        {
            SelectionCollider.gameObject.SetActive(true);
        }
        m_matPropBlock = new MaterialPropertyBlock();
        Cubes = new Transform[WorldEdgeSize * WorldEdgeSize * WorldEdgeSize];
        m_nativePositions = new NativeArray<Vector3>(Cubes.Length, Allocator.Persistent);
        m_nativeOffsets = new NativeArray<Vector3>(Cubes.Length, Allocator.Persistent);
        m_nativeColors = new NativeArray<Color>(Cubes.Length, Allocator.Persistent);
        m_nativeScales = new NativeArray<Vector3>(Cubes.Length, Allocator.Persistent);

        var index = 0;
        for (int x = 0; x < WorldEdgeSize; x++)
        {
            for (int y = 0; y < WorldEdgeSize; y++)
            {
                for (int z = 0; z < WorldEdgeSize; z++)
                {
                    Cubes[index] = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                    Cubes[index].position = new Vector3(x, y, z) * 5f - new Vector3(WorldEdgeSize * 5f * 0.5f, WorldEdgeSize * 5f * 0.5f, 0);
                    m_nativePositions[index] = Cubes[index].position;
                    m_nativeColors[index] = Color.white;
                    m_nativeScales[index] = Cubes[index].localScale;
                    index++;
                }
            }
        }
    }

    private NativeList<int> m_deferredWithinBoundsList;
    private NativeList<int> m_deferredOutsideBoundsList;

    void Update()
    {
        m_deferredWithinBoundsList = new NativeList<int>(Allocator.TempJob);
        m_deferredOutsideBoundsList = new NativeList<int>(Allocator.TempJob);

        var noiseJob = new BackgroundNoiseParallelForJob
        {
            ElapsedTime = Time.time,
            DeltaTime = Time.deltaTime,
            Offsets = m_nativeOffsets,
            OriginPositions = m_nativePositions,
        };

        var findWithinBoundsJob = new CheckBoundsFilterJob
        {
            CheckIsInside = true,
            Bounds = SelectionCollider.bounds,
            Positions = m_nativePositions,
            Offsets = m_nativeOffsets,
        };

        var findOutsideBoundsJob = new CheckBoundsFilterJob
        {
            CheckIsInside = false,
            Bounds = SelectionCollider.bounds,
            Positions = m_nativePositions,
            Offsets = m_nativeOffsets,
        };

        var highlightSelectionJob = new HighlightSelectionJob
        {
            Indexes = m_deferredWithinBoundsList,
            Colors = m_nativeColors,
            Scales = m_nativeScales,
        };

        var restoreUnselectionJob = new RestoreUnselectionJob
        {
            Indexes = m_deferredOutsideBoundsList,
            Colors = m_nativeColors,
            Scales = m_nativeScales,
        };


        m_jobHandle = noiseJob.Schedule(Cubes.Length, 1, m_jobHandle);
        m_jobHandle = findWithinBoundsJob.ScheduleAppend(m_deferredWithinBoundsList, Cubes.Length, 1, m_jobHandle);
        m_jobHandle = findOutsideBoundsJob.ScheduleAppend(m_deferredOutsideBoundsList, Cubes.Length, 1, m_jobHandle);
        m_jobHandle = restoreUnselectionJob.ScheduleByRef(m_deferredOutsideBoundsList, 1, m_jobHandle);
        m_jobHandle = highlightSelectionJob.ScheduleByRef(m_deferredWithinBoundsList, 1, m_jobHandle);
    }

    void LateUpdate()
    {
        m_jobHandle.Complete();
        m_deferredWithinBoundsList.Dispose();
        m_deferredOutsideBoundsList.Dispose();
        for (int i = 0; i < m_nativeOffsets.Length; i++)
        {
            Cubes[i].position = m_nativePositions[i] + m_nativeOffsets[i];
            Cubes[i].GetComponent<Renderer>().GetPropertyBlock(m_matPropBlock);
            m_matPropBlock.SetColor("_Color", m_nativeColors[i]);
            Cubes[i].GetComponent<Renderer>().SetPropertyBlock(m_matPropBlock);
            Cubes[i].localScale = m_nativeScales[i];
        }
    }

    void OnDisable()
    {
        if (SelectionCollider.gameObject.activeSelf)
        {
            SelectionCollider.gameObject.SetActive(false);
        }

        if (m_deferredWithinBoundsList.IsCreated)
            m_deferredWithinBoundsList.Dispose();
        if (m_deferredOutsideBoundsList.IsCreated)
            m_deferredOutsideBoundsList.Dispose();
        m_nativeOffsets.Dispose();
        m_nativePositions.Dispose();
        m_nativeColors.Dispose();
        m_nativeScales.Dispose();
        for (int i = 0; i < Cubes.Length; i++)
        {
            Destroy(Cubes[i].gameObject);
        }
    }
}