﻿using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class Graph : MonoBehaviour
{

    [SerializeField] TextAsset _dataFile = null;

    [SerializeField] int _maxGraphSize = 200;

    [SpaceAttribute(10)]

    [SerializeField] GameObject _nodePrefab = null;
    [SerializeField] GameObject _edgePrefab = null;

    [SerializeField] float repulsion = 0.005f;
    [SerializeField] float _damping = 0.005f;
    [SerializeField] float _springLength = 0.5f;
    [SerializeField] float springk = 12.2f;

    [SpaceAttribute(10)]

    [SerializeField] Vector3 _sphereProjectionCentre;
    [SerializeField] float _sphereRadius = 5.0f;

    List<Node> _nodes = new List<Node>();
    List<Edge> _edges = new List<Edge>();

    void Start()
    {
        Camera.main.useOcclusionCulling = false;
        TestLoadGraph();
    }

    void TestLoadGraph()
    {
        var nodeLookup = new Dictionary<string, Node>();

        var sr = new System.IO.StringReader(_dataFile.text);

        while (true)
        {
            var line = sr.ReadLine();
            if (line == null)
                break;
            if (line.Trim().Length > 0)
            {
                var tokens = line.Split(new string[] { "\t", " " }, StringSplitOptions.None);
                var n1tok = tokens[0];
                var n2tok = tokens[1];

                if (!nodeLookup.ContainsKey(n1tok))
                {
                    nodeLookup[n1tok] = CreateRandomNode();
                }
                if (!nodeLookup.ContainsKey(n2tok))
                {
                    nodeLookup[n2tok] = CreateRandomNode();
                }

                CreateEdge(nodeLookup[n1tok], nodeLookup[n2tok]);
            }
            if (_nodes.Count >= _maxGraphSize)
                break;
        }

        print("nodes: " + _nodes.Count);
    }

    void TestRandomGraph()
    {
        // create some nodes
        for (int i = 0; i < _maxGraphSize; ++i)
        {
            CreateRandomNode();
        }

        for (int i = 0; i < _maxGraphSize / 5; ++i)
        {
            Node n1 = _nodes[UnityEngine.Random.Range(0, _nodes.Count)];
            Node n2 = _nodes[UnityEngine.Random.Range(0, _nodes.Count)];
            if (n1 != n2)
            {
                CreateEdge(n1, n2);
            }
        }
    }

    void Update()
    {
        //ApplyHookesLaw();
        //ApplyColombsLaw();

        //UpdateVelocity();
        //UpdatePosition();
    }

    void ApplyHookesLaw()
    {
        foreach (var e in _edges)
        {
            Vector3 d = e.Body2.position - e.Body1.position;
            float displacement = _springLength - d.magnitude;
            Vector3 direction = d.normalized;

            e.Body1.AddForce(springk * direction * displacement * -0.5f);
            e.Body2.AddForce(springk * direction * displacement * 0.5f);
        }
    }

    void ApplyColombsLaw()
    {
        //int nodeCount = _nodes.Count;

        //for (int i = 0; i < nodeCount; ++i)
        //{
        //    for (int j = i; j < nodeCount; ++j)
        //    {
        //        if (i == j)
        //            continue;

        //        var n1 = _nodes[i];
        //        var n2 = _nodes[j];

        //        Vector3 d = n1.position - n2.position;
        //        float distance = d.magnitude + 0.001f;
        //        Vector3 direction = d.normalized;

        //        if (distance < 115)
        //        {
        //            var force = (direction * repulsion) / (distance * distance * 0.5f);

        //            n1.AddForce(force);
        //            n2.AddForce(-force);
        //        }
        //    }
        //}
    }

    void UpdateVelocity()
    {
        for (int i = 0; i < _nodes.Count; ++i)
        {
            Node n = _nodes[i];
            n.vel = (n.vel + n.acc * Time.deltaTime) * _damping;
            //if (float.IsNaN(n.pos.x))
            //{
            //    print("BLAM2");
            //}
            n.acc.Set(0, 0, 0);
        }
    }

    void UpdatePosition()
    {
        int nodeCount = _nodes.Count;
        float dt = Time.deltaTime;
        for (int i = 0; i < nodeCount; ++i)
        {
            Node n = _nodes[i];
            if (float.IsNaN(n.pos.x))
            {
                Debug.Break();
            }
            n.pos += n.vel * dt;

            // project the node's position onto a sphere
            Vector3 pos = n.pos;
            n.transform.position = pos;
            //pos.z = _sphereRadius;
            //n.transform.localPosition = pos.normalized * _sphereRadius;
        }
    }

    Node CreateRandomNode()
    {
        var pos = UnityEngine.Random.insideUnitSphere * 20;
        pos.z = 0;

        GameObject obj = (GameObject)Instantiate(_nodePrefab, pos, Quaternion.identity, transform);
        obj.transform.SetAsFirstSibling();
        Node n = obj.GetComponent<Node>();
        n.pos = pos;
        n.acc = new Vector3();
        n.vel = new Vector3();
        _nodes.Add(n);
        return n;
    }

    void CreateEdge(Node n1, Node n2)
    {
        GameObject edge = (GameObject)Instantiate(_edgePrefab, transform);
        var spring = edge.GetComponent<Edge>();
        spring.Body1 = n1;
        spring.Body2 = n2;

        _edges.Add(spring);

        var render = edge.GetComponent<EdgeRenderer>();
        render.Body1 = n1.transform;
        render.Body2 = n2.transform;
    }
}

class GraphLayoutSystem : ComponentSystem
{
    struct Components {
        public Node node;
    }

    struct Components2
    {
        public Node node;
        public Transform transform;
    }

    struct EdgeRendererFilter {
        public EdgeRenderer edgeRenderer;
    }


    protected override void OnUpdate()
    {
        float repulsion = 3f;
        float _damping = 0.00075f;


        var nodes = Entities.WithAll<Node>().ToEntityQuery().ToComponentArray<Node>();

        //Entities.ForEach((Entity e, ref Node n) =>
        //{

        //});



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
        //    n.node.acc = new Vector3 {};

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