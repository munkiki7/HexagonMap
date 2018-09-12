using UnityEngine;

[System.Serializable]
public struct TerrainObjectsCollection
{
	public GameObject[] prefabs;

	public GameObject Pick(float choice)
	{
		return prefabs[(int) (choice * prefabs.Length)];
	}
}
