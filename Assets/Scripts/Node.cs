using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[RequiresEntityConversion]
public class Node : MonoBehaviour
    , IConvertGameObjectToEntity
{

	public Rigidbody Body { get { return GetComponent<Rigidbody>(); } }

	public Vector3 position {
		get { return pos; }
	}

	public Vector3 pos = new Vector3();
	public Vector3 acc = new Vector3();
	public Vector3 vel = new Vector3();

	public void AddForce(Vector3 f){
		acc += f * 10000;
	}

    // The MonoBehaviour data is converted to ComponentData on the entity.
    // We are specifically transforming from a good editor representation of the data (Represented in degrees)
    // To a good runtime representation (Represented in radians)
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new NodeECS { pos = transform.position, acc = this.acc, vel = this.vel };
        dstManager.AddComponentData(entity, data);
        GetComponent<MeshRenderer>().enabled = false;
    }
}

[System.Serializable]
public struct NodeECS: IComponentData
{
    public float3 acc;
    public float3 vel;
    public float3 pos;
}