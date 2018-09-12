using UnityEngine;

public class TerrainObjectManager : MonoBehaviour
{
	public GameObject[] treePrefabs;

	public TerrainObjectsCollection[] urbanCollections;

	public HexagonMesh walls;

	GameObject _pickedHousePrefab;

	Transform _objectContainer;
	
	public void Clear()
	{
		if (_objectContainer)
		{
			Destroy(_objectContainer.gameObject);
		}
		_objectContainer = new GameObject("Object Container").transform;
		_objectContainer.SetParent(transform, false);
		walls.Clear();
	}

	public void Apply()
	{
		walls.Apply();
	}

	public void AddObjects(Vector3 position, Cell cell)
	{
		var hash = Metrics.SampleHashGrid(position);
		
		if (cell.UrbanizationLevel > 0)
		{
			AddBuildings(position, cell, hash);
		}
		else
		{
			AddTrees(position, cell, hash);
		}
	}

	public void AddTrees(Vector3 position, Cell cell, PseudoRandom hash)
	{
		if (hash.existingChance >= Mathf.Abs(cell.UrbanizationLevel) * 0.25f)
		{
			return;
		}
		var randomNumber = hash.treeType;
		var tree = Instantiate(treePrefabs[randomNumber]);
		if (randomNumber == 0)
		{
			position.y += 2;
		}
		tree.transform.localPosition = Metrics.Perturb(position);
		tree.transform.localRotation = Quaternion.Euler(0f, 360f * hash.houseRotation, 0f);
		tree.transform.SetParent(_objectContainer, false);
	}

	public void AddBuildings(Vector3 position, Cell cell, PseudoRandom hash)
	{
		var prefab = PickHousePrefab(cell.UrbanizationLevel, hash.existingChance, hash.housePick);
		if (!prefab)
		{
			return;
		}
		var house = Instantiate(prefab);
		position.y += house.transform.localScale.y * 0.5f;
		house.transform.localPosition = Metrics.Perturb(position);
		house.transform.localRotation = Quaternion.Euler(0f, 360f * hash.houseRotation, 0f);
		house.transform.SetParent(_objectContainer, false);
	}

	public void AddWall(EdgeVertices closeEdges, Cell closeCell, EdgeVertices farEdges, Cell farCell)
	{
		if (closeCell.IsWalled != farCell.IsWalled)
		{
			AddWallSegment(closeEdges.vertex1, farEdges.vertex1, closeEdges.vertex5, farEdges.vertex5);
		}
	}

	void AddWallSegment(Vector3 closeLeftVertex, Vector3 farLeftVertex, Vector3 closeRightVertex,
		Vector3 farRightVertex)
	{
		var bottomLeftWallEdge = Vector3.Lerp(closeLeftVertex, farLeftVertex, 0.5f);
		var topLeftWallEdge = bottomLeftWallEdge;
		topLeftWallEdge.y += Metrics.WallHeight;
		var bottomRightWallEdge = Vector3.Lerp(closeRightVertex, farRightVertex, 0.5f);
		var topRightWallEdge = bottomRightWallEdge;
		topRightWallEdge.y += Metrics.WallHeight;
		
		walls.AddQuad(bottomLeftWallEdge, bottomRightWallEdge, topLeftWallEdge, topRightWallEdge);
		walls.AddQuad(bottomRightWallEdge, bottomLeftWallEdge, topRightWallEdge, topLeftWallEdge);
	}

	GameObject PickHousePrefab(int urbanizationLevel, float hash, float choice)
	{
		if (urbanizationLevel > 0)
		{
			var houseThresholds = Metrics.GetHouseThresholds(urbanizationLevel);
			for (var i = 0; i < houseThresholds.Length; i++)
			{
				if (hash < houseThresholds[i])
				{
					return urbanCollections[i].Pick(choice);
				}
			}
		}

		return null;
	}
}
