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
    JobHandle m_lastjob;

    public float repulsion = 3f;
    public float _damping = 0.075f;
    public float forceScale = 10000;
    private NativeArray<NodeECS> scratchStorage;
    private NativeArray<float3> forcesBuffer;

    //[NativeDisableParallelForRestriction]
    //private BufferFromEntity<Forces> entityForces;

    protected override void OnCreate()
    {
        // Cached access to a set of ComponentData based on a specific query
        m_Group = GetEntityQuery(typeof(Translation), typeof(NodeECS));
        // scratchStorage = new NativeArray<NodeECS>(0, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        RequireForUpdate(m_Group);
    }

    [BurstCompile]
    struct GatherNodesJob : IJobForEachWithEntity<NodeECS>
    {
        public NativeArray<NodeECS> scratch;

        public void Execute(Entity entity, int index, [ReadOnly] ref NodeECS c0)
        {
            scratch[index] = c0;
        }
    }

    // Use the [BurstCompile] attribute to compile a job with Burst. You may see significant speed ups, so try it!
    [BurstCompile]
    struct RotationSpeedJob : IJobForEachWithEntity<NodeECS>
    {
        public float DeltaTime;
        public float repulsion;
        public float forceScale;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<NodeECS> scratch;
        public NativeArray<float3> forceBuffer;

        public void Execute(Entity entity, int index, [ReadOnly] ref NodeECS node)
        {
            for (var j = 0; j < scratch.Length; j++)
            {
                if (j == index)
                    continue;

                var n2 = scratch[j];

                float3 d = node.pos - n2.pos;
                float distance = math.length(d) + 0.01f;
                float3 direction = math.normalize(d);

                if (distance < 115)
                {
                    //var dir = math.select(1, -1, j < index);
                    var force = (direction * repulsion) / (distance * distance * 0.5f);
                    force *= forceScale;
                    //force *= dir;
                    forceBuffer[index] = force + forceBuffer[index];
                }
            }

        }
    }

    [BurstCompile]
    struct ApplyMotionJob : IJobForEachWithEntity<Translation, NodeECS>
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> scratch;
        public float DeltaTime;
        public float damping;

        public void Execute(Entity entity, int index, ref Translation c0, ref NodeECS c1)
        {
            var f = scratch[index];
            float3 forceSum = f;
            c1.acc += forceSum;
            c1 = new NodeECS
            {
                vel = (c1.vel + c1.acc * DeltaTime) * damping,
                acc = new float3(),
                pos = c1.pos + c1.vel * DeltaTime
            };
            c0 = new Translation { Value = c1.pos };
        }
    }

    // OnUpdate runs on the main thread.
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        m_lastjob.Complete();
        // Explicitly declare:
        // - Read-Write access to Rotation
        // - Read-Only access to RotationSpeed_IJobChunk
        //var translationType = GetArchetypeChunkComponentType<Translation>();
        //var nodeECSType = GetArchetypeChunkComponentType<NodeECS>();
        scratchStorage = new NativeArray<NodeECS>(m_Group.CalculateLength(), Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        forcesBuffer = new NativeArray<float3>(m_Group.CalculateLength(), Allocator.TempJob);

        //entityForces = GetBufferFromEntity<Forces>();
        var job = new RotationSpeedJob()
        {
            //EntityType = GetArchetypeChunkEntityType(),
            //NodeECSType = nodeECSType,
            DeltaTime = Time.deltaTime,
            forceScale = forceScale,
            repulsion = repulsion,
            scratch = scratchStorage,
            forceBuffer = forcesBuffer
        };

        var gatherJob = new GatherNodesJob() { scratch = scratchStorage };
        var applyMotionJob = new ApplyMotionJob() { DeltaTime = Time.deltaTime, damping = _damping, scratch = forcesBuffer};

        var gatherJobHandle = gatherJob.Schedule(m_Group, inputDependencies);
        var jobHandle = job.Schedule(m_Group, gatherJobHandle);
        var applyMotionJobHandle = applyMotionJob.Schedule(m_Group, jobHandle);
        //m_Group.AddDependency(applyMotionJobHandle);
        m_lastjob = applyMotionJobHandle;
        return applyMotionJobHandle;
    }

    private struct Forces : IBufferElementData
    {
        public float3 Value;
    }
}
