using UnityEngine;

public class TerrainObjectManager : MonoBehaviour
{
	public GameObject[] treePrefabs;

	public TerrainObjectsCollection[] urbanCollections;

	public HexagonMesh walls;

	GameObject _pickedHousePrefab;

	public GameObject towerPrefab, bridgePrefab, castlePrefab;

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
		tree.transform.localRotation = Quaternion.Euler(0f, 360f * hash.rotation, 0f);
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
		house.transform.localRotation = Quaternion.Euler(0f, 360f * hash.rotation, 0f);
		house.transform.SetParent(_objectContainer, false);
	}

	public void AddWall(EdgeVertices closeEdges, Cell closeCell, EdgeVertices farEdges, Cell farCell, bool hasRiver, bool hasRoad)
	{
		if (closeCell.IsWalled != farCell.IsWalled && 
		    !closeCell.IsUnderWater && !farCell.IsUnderWater 
		    && closeCell.GetEdgeType(farCell) != HexagonEdgeType.Cliff)
		{
			AddWallSegment(closeEdges.vertex1, farEdges.vertex1, closeEdges.vertex2, farEdges.vertex2);
			if (!hasRiver && !hasRoad)
			{
				AddWallSegment(closeEdges.vertex2, farEdges.vertex2, closeEdges.vertex3, farEdges.vertex3);
				AddWallSegment(closeEdges.vertex3, farEdges.vertex3, closeEdges.vertex4, farEdges.vertex4);
			}
			else
			{
				AddWallCap(closeEdges.vertex2, farEdges.vertex2);
				AddWallCap(farEdges.vertex4, closeEdges.vertex4);
			}
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

	public void AddBridgeStraightRiver(Vector3 road1Center, Vector3 road2Center)
	{
		road1Center = Metrics.Perturb(road1Center);
		road2Center = Metrics.Perturb(road2Center);
		
		bridgePrefab = Instantiate(bridgePrefab);
		bridgePrefab.transform.localPosition = (road1Center + road2Center) / 2;
		bridgePrefab.transform.localPosition += new Vector3(-1.275f, -0.539f, -2.32f);
		bridgePrefab.transform.right = road1Center - road2Center;
		bridgePrefab.transform.SetParent(_objectContainer, false);
	}
	
	public void AddBridgeCurvedRiver(Vector3 road1Center, Vector3 road2Center)
	{
		road1Center = Metrics.Perturb(road1Center);
		road2Center = Metrics.Perturb(road2Center);
		
		bridgePrefab = Instantiate(bridgePrefab);
		bridgePrefab.transform.localPosition = (road1Center + road2Center) / 2;
		bridgePrefab.transform.right = road1Center - road2Center;
		var length = Vector3.Distance(road1Center, road2Center);
		bridgePrefab.transform.localScale = new Vector3(1, 1, length / 7f);
		bridgePrefab.transform.SetParent(_objectContainer, false);
	}

	public void AddCastle(Cell cell, Vector3 position)
	{
		var castle = Instantiate(castlePrefab);
		castle.transform.localPosition = Metrics.Perturb(position);
		castle.transform.localRotation = Quaternion.Euler(270f, 360f * Metrics.SampleHashGrid(position).rotation, 0f);
		castle.transform.SetParent(_objectContainer, false);
	}

	void AddWallSegment(Vector3 closeLeftVertex, Vector3 farLeftVertex, Vector3 closeRightVertex,
		Vector3 farRightVertex, bool addTower = false)
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

		if (addTower)
		{
			AddTower(bottomLeftWallEdge, bottomRightWallEdge);
		}
	}

	void AddTower(Vector3 leftWallPoint, Vector3 rightWallPoint)
	{
		var tower = Instantiate(towerPrefab);
		tower.transform.localPosition = (leftWallPoint + rightWallPoint) / 2f;

		var rightDirection = leftWallPoint - rightWallPoint;
		rightDirection.y = 0f;
		tower.transform.right = rightDirection;
		
		tower.transform.SetParent(_objectContainer, false);
	}

	void AddWallSegment(Vector3 pivotVertex, Cell pivotCell, Vector3 leftVertex, Cell leftCell, Vector3 rightVertex,
		Cell rightCell)
	{
		if (pivotCell.IsUnderWater)
		{
			return;
		}

		var hasLeftWall = !leftCell.IsUnderWater && pivotCell.GetEdgeType(leftCell) != HexagonEdgeType.Cliff;
		var hasRightWall = !rightCell.IsUnderWater && pivotCell.GetEdgeType(rightCell) != HexagonEdgeType.Cliff;
		
		if (hasLeftWall)
		{
			if (hasRightWall)
			{
				var hasTower = false;
				if (leftCell.Elevation == rightCell.Elevation)
				{
					var probability = Metrics.SampleHashGrid((pivotVertex + leftVertex + rightVertex) / 3f);
					hasTower = probability.existingChance < Metrics.TowerExistenceProbability;
				}
				AddWallSegment(pivotVertex, leftVertex, pivotVertex, rightVertex, hasTower);
			}
			else if (leftCell.Elevation < rightCell.Elevation)
			{
				AddWallWedge(pivotVertex, leftVertex, rightVertex);
			}
			else
			{
				AddWallCap(pivotVertex, leftVertex);
			}
		}
		else if (hasRightWall)
		{
			if (rightCell.Elevation < leftCell.Elevation)
			{
				AddWallWedge(rightVertex, pivotVertex, leftVertex);
			}
			else
			{
				AddWallCap(rightVertex, pivotVertex);
			}
		}
	}

	void AddWallCap(Vector3 closeVertex, Vector3 farVertex)
	{
		closeVertex = Metrics.Perturb(closeVertex);
		farVertex = Metrics.Perturb(farVertex);
		var center = Metrics.WallLerp(closeVertex, farVertex);
		var thickness = Metrics.WallThicknessOffset(closeVertex, farVertex);

		Vector3 vertex3, vertex4;

		var vertex1 = vertex3 = center - thickness;
		var vertex2 = vertex4 = center + thickness;

		vertex3.y = vertex4.y = center.y + Metrics.WallHeight;
		walls.AddQuadUnperturbed(vertex1, vertex2, vertex3, vertex4);
	}
	
	void AddWallWedge(Vector3 closeVertex, Vector3 farVertex, Vector3 point)
	{
		closeVertex = Metrics.Perturb(closeVertex);
		farVertex = Metrics.Perturb(farVertex);
		point = Metrics.Perturb(point);
		
		var center = Metrics.WallLerp(closeVertex, farVertex);
		var thickness = Metrics.WallThicknessOffset(closeVertex, farVertex);

		Vector3 vertex3, vertex4;
		var pointTop = point;
		point.y = center.y;

		var vertex1 = vertex3 = center - thickness;
		var vertex2 = vertex4 = center + thickness;

		vertex3.y = vertex4.y = pointTop.y = center.y + Metrics.WallHeight;
		walls.AddQuadUnperturbed(vertex1, point, vertex3, pointTop);
		walls.AddQuadUnperturbed(point, vertex2, pointTop, vertex4);
		walls.AddTriangleUnperturbed(pointTop, vertex3, vertex4);
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
