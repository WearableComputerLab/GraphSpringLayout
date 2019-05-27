using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// This system updates all entities in the scene with both a RotationSpeed_IJobChunk and Rotation component.

// ReSharper disable once InconsistentNaming
public class GraphLayoutSystem_JobChunk : JobComponentSystem
{
    EntityQuery m_Group;

    public float repulsion = 3f;
    public float _damping = 0.075f;
    public float forceScale = 10000;

    protected override void OnCreate()
    {
        // Cached access to a set of ComponentData based on a specific query
        m_Group = GetEntityQuery(typeof(Translation), typeof(NodeECS));
    }

    // Use the [BurstCompile] attribute to compile a job with Burst. You may see significant speed ups, so try it!
    //[BurstCompile]
    struct RotationSpeedJob : IJobChunk
    {
        public float DeltaTime;
        public ArchetypeChunkComponentType<Translation> TranslationType;
        public ArchetypeChunkComponentType<NodeECS> NodeECSType;
        public float repulsion;
        public float _damping;
        public float forceScale;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkTranslations = chunk.GetNativeArray(TranslationType);
            var chunkNodes = chunk.GetNativeArray(NodeECSType);
            for (var i = 0; i < chunk.Count; i++)
            {
                var n1 = chunkNodes[i];
                for (var j = i + 1; j < chunk.Count; j++)
                {
                    var n2 = chunkNodes[j];

                    float3 d = n1.pos - n2.pos;
                    float distance = math.length(d) + 0.001f;
                    float3 direction = math.normalize(d);

                    if (distance < 115)
                    {
                        var force = (direction * repulsion) / (distance * distance * 0.5f);
                        force *= forceScale;
                        n1.acc += force;
                        n2.acc -= force;
                    }
                }

            }

            for (var i = 0; i < chunk.Count; i++)
            {
                var n = chunkNodes[i];
                n.vel = (n.vel + n.acc * DeltaTime) * _damping;
                n.acc = new float3 { };

                n.pos += math.up() * DeltaTime;

                chunkTranslations[i] = new Translation { Value = n.pos };
            }
        }
    }

    // OnUpdate runs on the main thread.
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        // Explicitly declare:
        // - Read-Write access to Rotation
        // - Read-Only access to RotationSpeed_IJobChunk
        var translationType = GetArchetypeChunkComponentType<Translation>();
        var nodeECSType = GetArchetypeChunkComponentType<NodeECS>();

        var job = new RotationSpeedJob()
        {
            TranslationType = translationType,
            NodeECSType = nodeECSType,
            DeltaTime = Time.deltaTime,
            forceScale = forceScale,
            repulsion = repulsion,
            _damping = _damping
        };

        return job.Schedule(m_Group, inputDependencies);
    }
}
