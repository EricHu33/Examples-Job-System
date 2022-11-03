using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

public class NoiseMotionMainThread : MonoBehaviour
{
    public int WorldEdgeSize;
    private Transform[] m_cubes;
    private Vector3[] m_originalPositions;

    void OnEnable()
    {
        m_cubes = new Transform[WorldEdgeSize * WorldEdgeSize * WorldEdgeSize];
        m_originalPositions = new Vector3[m_cubes.Length];

        var index = 0;
        for (int x = 0; x < WorldEdgeSize; x++)
        {
            for (int y = 0; y < WorldEdgeSize; y++)
            {
                for (int z = 0; z < WorldEdgeSize; z++)
                {
                    m_cubes[index] = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                    m_cubes[index].position = new Vector3(x, y, z) * 5f - new Vector3(WorldEdgeSize * 5f * 0.5f, WorldEdgeSize * 5f * 0.5f, 0);
                    m_originalPositions[index] = m_cubes[index].position;
                    index++;
                }
            }
        }
    }

    void Update()
    {
        for (int i = 0; i < m_cubes.Length; i++)
        {
            var p = m_originalPositions[i];
            var sinx = Mathf.Sin(Time.time + Perlin.Noise(0.3f * p.x + Time.time));
            var siny = Mathf.Cos(Time.time + Perlin.Noise(0.5f * p.y + Time.time));
            var sinz = Mathf.Sin(Time.time + Perlin.Noise(0.7f * p.z - Time.time));
            m_cubes[i].position = p + new Vector3(sinx, siny, sinz);
        }
    }

    void OnDisable()
    {
        for (int i = 0; i < m_cubes.Length; i++)
        {
            Destroy(m_cubes[i].gameObject);
        }
    }
}
