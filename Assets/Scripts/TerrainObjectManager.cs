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
			AddWallSegment(closeEdges.vertex1, farEdges.vertex1, closeEdges.vertex2, farEdges.vertex2);
			AddWallSegment(closeEdges.vertex2, farEdges.vertex2, closeEdges.vertex3, farEdges.vertex3);
			AddWallSegment(closeEdges.vertex3, farEdges.vertex3, closeEdges.vertex4, farEdges.vertex4);
			AddWallSegment(closeEdges.vertex4, farEdges.vertex4, closeEdges.vertex5, farEdges.vertex5);
		}
	}

	public void AddWall(Vector3 vertex1, Cell cell1, Vector3 vertex2, Cell cell2, Vector3 vertex3, Cell cell3)
	{
		if (cell1.IsWalled)
		{
			if (cell2.IsWalled)
			{
				if (!cell3.IsWalled)
				{
					AddWallSegment(vertex3, cell3, vertex1, cell1, vertex2, cell2);
				}
			}
			else if (cell3.IsWalled)
			{
				AddWallSegment(vertex2, cell2, vertex3, cell3, vertex1, cell1);
			}
			else
			{
				AddWallSegment(vertex1, cell1, vertex2, cell2, vertex3, cell3);
			}
		}
		else if (cell2.IsWalled)
		{
			if (cell3.IsWalled)
			{
				AddWallSegment(vertex1, cell1, vertex2, cell2, vertex3, cell3);
			}
			else
			{
				AddWallSegment(vertex2, cell2, vertex3, cell3, vertex1, cell1);
			}
		}
		else if(cell3.IsWalled)
		{
			AddWallSegment(vertex3, cell3, vertex1, cell1, vertex2, cell2);
		}
	}

	void AddWallSegment(Vector3 closeLeftVertex, Vector3 farLeftVertex, Vector3 closeRightVertex,
		Vector3 farRightVertex)
	{
		closeLeftVertex = Metrics.Perturb(closeLeftVertex);
		closeRightVertex = Metrics.Perturb(closeRightVertex);
		farLeftVertex = Metrics.Perturb(farLeftVertex);
		farRightVertex = Metrics.Perturb(farRightVertex);
		
		var centralLeftWallPoint = Metrics.WallLerp(closeLeftVertex, farLeftVertex);
		var centralRightWallPoint = Metrics.WallLerp(closeRightVertex, farRightVertex);
		
		var leftThicknessOffset = Metrics.WallThicknessOffset(closeLeftVertex, farLeftVertex);
		var rightThicknessOffset = Metrics.WallThicknessOffset(closeRightVertex, farRightVertex);

		var bottomLeftWallEdge = centralLeftWallPoint - leftThicknessOffset;
		var bottomRightWallEdge = centralRightWallPoint - rightThicknessOffset;
		
		var topLeftWallEdge = bottomLeftWallEdge;
		var topRightWallEdge = bottomRightWallEdge;
		
		topLeftWallEdge.y += Metrics.WallHeight;
		topRightWallEdge.y += Metrics.WallHeight;
		
		walls.AddQuadUnperturbed(bottomLeftWallEdge, bottomRightWallEdge, topLeftWallEdge, topRightWallEdge);

		var topLeftEdge = topLeftWallEdge;
		var topRightEdge = topRightWallEdge;
		
		bottomLeftWallEdge = topLeftWallEdge = centralLeftWallPoint + leftThicknessOffset;
		bottomRightWallEdge = topRightWallEdge = centralRightWallPoint + rightThicknessOffset;
		
		topLeftWallEdge.y += Metrics.WallHeight;
		topRightWallEdge.y += Metrics.WallHeight;
		
		walls.AddQuadUnperturbed(bottomRightWallEdge, bottomLeftWallEdge, topRightWallEdge, topLeftWallEdge);
		
		walls.AddQuadUnperturbed(topLeftEdge, topRightEdge, topLeftWallEdge, topRightWallEdge);
	}

	void AddWallSegment(Vector3 pivotVertex, Cell pivotCell, Vector3 leftVertex, Cell leftCell, Vector3 rightVertex,
		Cell rightCell)
	{
		AddWallSegment(pivotVertex, leftVertex, pivotVertex, rightVertex);
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
