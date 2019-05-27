using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

class GraphLayoutSystem : ComponentSystem
{
    public float repulsion = 3f;
    public float _damping = 0.075f;
    public float forceScale = 10000;

    protected override void OnUpdate()
    {

        //var nodes = Entities.WithAll<Node>().ToEntityQuery().ToComponentArray<Node>();

        Entities.ForEach((Entity e, ref NodeECS n) =>
        {
            var n1 = n;

            Entities.ForEach((Entity e2, ref NodeECS n2) =>
            {
                if (e2.Index <= e.Index)
                    return;

                float3 d = n1.pos - n2.pos;
                float distance = length(d) + 0.001f;
                float3 direction = normalize(d);

                if (distance < 115)
                {
                    var force = (direction * repulsion) / (distance * distance * 0.5f);
                    force *= forceScale;
                    n1.acc += force;
                    n2.acc -= force;
                }
            });
            n = n1;
        });

        // update positions job
        Entities.ForEach((ref Translation t, ref NodeECS n) =>
        {
            n.vel = (n.vel + n.acc * Time.deltaTime) * _damping;
            n.acc = new float3 { };

            n.pos += n.vel * Time.deltaTime;

            t.Value = n.pos;
        }
        );

        //foreach (var n1 in ents)
        //{
        //    foreach (var n2 in ents)
        //    {
        //        if (n1.node.pos == n2.node.pos)
        //            continue;

        //        Vector3 d = n1.node.pos - n2.node.pos;
        //        float distance = d.magnitude + 0.001f;
        //        Vector3 direction = d.normalized;

        //        if (distance < 115)
        //        {
        //            var force = (direction * repulsion) / (distance * distance * 0.5f);

        //            n1.node.AddForce(force);
        //            n2.node.AddForce(-force);
        //        }
        //    }
        //}

        //foreach (var n in GetEntities<Components2>())
        //{
        //    n.node.vel = (n.node.vel + n.node.acc * Time.deltaTime) * _damping;
        //    n.node.acc = new Vector3 { };

        //    n.node.pos += n.node.vel * Time.deltaTime;

        //    n.transform.position = n.node.pos;
        //}

        //foreach (var e in GetEntities<EdgeRendererFilter>())
        //{
        //    var points = new Vector3[2] { e.edgeRenderer.Body1.position, e.edgeRenderer.Body2.position };
        //    e.edgeRenderer._lineRenderer.SetPositions(points);


        //}



    }
}
