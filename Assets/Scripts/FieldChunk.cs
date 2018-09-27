using System;
using System.Runtime.Serialization;
using UnityEngine;

public class FieldChunk : MonoBehaviour
{
	Cell[] _cells;

	[SerializeField]
	Canvas _canvas;

	public HexagonMesh terrain, rivers, roads, openWater, shoreWater, estuaries, walls;

	public TerrainObjectManager objectManager;

	void Awake()
	{
		_cells = new Cell[Metrics.ChunkSizeX * Metrics.ChunkSizeZ];
		ShowLabels(false);
	}

	public void AddCell(int index, Cell cell)
	{
		_cells[index] = cell;
		cell.chunk = this;
		cell.transform.SetParent(transform, false);
		cell.labelHolder.SetParent(_canvas.transform, false);
	}

	public void Refresh()
	{
		enabled = true;
	}

	void LateUpdate()
	{
		Triangulate();
		enabled = false;
	}

	public void ShowLabels(bool show)
	{
		_canvas.gameObject.SetActive(show);
	}
	
	public void Triangulate()
	{
		terrain.Clear();
		rivers.Clear();
		roads.Clear();
		openWater.Clear();
		shoreWater.Clear();
		estuaries.Clear();
		objectManager.Clear();
		foreach (var cell in _cells)
		{
			TriangulateCell(cell);
		}
		terrain.Apply();
		rivers.Apply();
		roads.Apply();
		openWater.Apply();
		shoreWater.Apply();
		estuaries.Apply();
		objectManager.Apply();
	}
	
	void TriangulateCell(Cell cell)
	{
		for (var d = Direction.NorthEast; d <= Direction.NorthWest; d++)
		{
			Triangulate(d, cell);
		}
		if (!cell.IsUnderWater && !cell.HasRiver && !cell.HasRoads)
		{
			objectManager.AddObjects(cell.Position, cell);
		}
		if (cell.isSpecial)
		{
			objectManager.AddCastle(cell, cell.Position);
		}
	}

	void Triangulate(Direction direction, Cell cell)
	{
		var center = cell.Position;
		var edges = new EdgeVertices(center + Metrics.GetFirstSolidCorner(direction), center + Metrics.GetSecondSolidCorner(direction));

		if (cell.HasRiver)
		{
			if (cell.HasRiverAtDirection(direction))
			{
				edges.vertex3.y = cell.RiverBedY;
				if (cell.HasOneEndOfRiver)
				{
					TriangulateOneEndOfRiver(direction, cell, center, edges);
				}
				else
				{
					TriangulateWithRiver(direction, cell, center, edges);
				}
			}
			else
			{
				TriangulateAdjacentToRiver(direction, cell, center, edges);
			}
		}
		else
		{
			TriangulateWithoutRiver(direction, cell, center, edges);

			if (!cell.IsUnderWater && !cell.HasRoadAtDirection(direction))
			{
				objectManager.AddObjects((center + edges.vertex1 + edges.vertex5)/3, cell);
			}
		}
		
		if (direction <= Direction.SouthEast)
		{
			TriangulateConnection(direction, cell, edges);
		}

		if (cell.IsUnderWater)
		{
			TriangulateWater(direction, cell, center);
		}
	}

	#region Terrain
	
	void TriangulateConnection(Direction direction, Cell cell, EdgeVertices edges1)
	{
		//Bridge
		var neighbour = cell.GetNeighbour(direction);
		if (neighbour == null)
		{
			return;
		}
		var bridge = Metrics.GetBridge(direction);
		bridge.y = neighbour.Position.y - cell.Position.y;
		var edges2 = new EdgeVertices(edges1.vertex1 + bridge, edges1.vertex5 + bridge);

		if (cell.HasRiverAtDirection(direction))
		{
			edges2.vertex3.y = neighbour.RiverBedY;
			if (!cell.IsUnderWater )
			{
				if (!neighbour.IsUnderWater)
				{
					TriangulateRiverQuad(edges1.vertex2, edges1.vertex4, edges2.vertex2, edges2.vertex4, 
						cell.RiverSurfaceY, neighbour.RiverSurfaceY, 0.8f, cell.HasIncomingRiver && cell.IncomingRiverDirection == direction);
				}
				else if (cell.Elevation > neighbour.WaterLevel)
				{
					TriangulateWaterFallInWater(edges1.vertex2, edges1.vertex4, edges2.vertex2, edges2.vertex4, cell.RiverSurfaceY, neighbour.RiverSurfaceY, neighbour.WaterSurfaceY);
				}
			}
			else if (!neighbour.IsUnderWater && neighbour.Elevation > cell.WaterLevel)
			{
				TriangulateWaterFallInWater(edges2.vertex4, edges2.vertex2, edges1.vertex4, edges1.vertex2, neighbour.RiverSurfaceY, cell.RiverSurfaceY, cell.WaterSurfaceY);
			}
		}
		
		if (cell.GetEdgeType(direction) == HexagonEdgeType.Slope)
		{
			TriangulateTerraces(edges1, cell, edges2, neighbour, cell.HasRoadAtDirection(direction));
		}
		else
		{
			TriangulateEdgeStrip(edges1, edges2, cell.Color, neighbour.Color, cell.HasRoadAtDirection(direction));
		}
		
		objectManager.AddWall(edges1, cell, edges2, neighbour, cell.HasRiverAtDirection(direction), cell.HasRoadAtDirection(direction));
		//Corner triangles
		var nextNeighbour = cell.GetNeighbour(direction.Next());
		if (nextNeighbour != null && neighbour != null && direction <= Direction.East)
		{
			var vertex5 = edges1.vertex5 + Metrics.GetBridge(direction.Next());
			vertex5.y = nextNeighbour.Position.y;
			if (cell.Elevation <= neighbour.Elevation)
			{
				if (cell.Elevation <= nextNeighbour.Elevation)
				{
					TriangulateCorner(edges1.vertex5, cell, edges2.vertex5, neighbour, vertex5, nextNeighbour);
				}
				else
				{
					TriangulateCorner(vertex5, nextNeighbour, edges1.vertex5, cell, edges2.vertex5, neighbour);
				}
			}
			else if (neighbour.Elevation <= nextNeighbour.Elevation)
			{
				TriangulateCorner(edges2.vertex5, neighbour, vertex5, nextNeighbour, edges1.vertex5, cell);
			}
			else
			{
				TriangulateCorner(vertex5, nextNeighbour , edges1.vertex5, cell, edges2.vertex5, neighbour);
			}
		}
	}

	void TriangulateTerraces(EdgeVertices beginEdges, Cell beginCell, EdgeVertices endEdges, Cell endCell, bool hasRoad)
	{
		//First Quad
		var intermediateEdges = EdgeVertices.TerraceLerp(beginEdges, endEdges, 1);
		var intermediateColor = Metrics.TerraceLerp(beginCell.Color, endCell.Color, 1);
		
		TriangulateEdgeStrip(beginEdges, intermediateEdges, beginCell.Color, intermediateColor, hasRoad);

		for (var i = 2; i < Metrics.SlopeSteps; i++)
		{
			var startEdges = intermediateEdges;
			var startColor = intermediateColor;
			intermediateEdges = EdgeVertices.TerraceLerp(beginEdges, endEdges, i);
			intermediateColor = Metrics.TerraceLerp(beginCell.Color, endCell.Color, i);
			TriangulateEdgeStrip(startEdges, intermediateEdges, startColor, intermediateColor, hasRoad);
		}
		
		TriangulateEdgeStrip(intermediateEdges, endEdges, intermediateColor, endCell.Color, hasRoad);
	}

	void TriangulateCorner(Vector3 bottomVertex, Cell bottomCell, Vector3 leftVertex, Cell leftCell,
		Vector3 rightVertex, Cell rightCell)
	{
		var leftEdgeType = bottomCell.GetEdgeType(leftCell);
		var rightEdgeType = bottomCell.GetEdgeType(rightCell);

		if (leftEdgeType == HexagonEdgeType.Slope)
		{
			if (rightEdgeType == HexagonEdgeType.Slope)
			{
				TriangulateCornerTerraces(bottomVertex, bottomCell, leftVertex, leftCell, rightVertex, rightCell);
			}
			else if (rightEdgeType == HexagonEdgeType.Flat)
			{
				TriangulateCornerTerraces(leftVertex, leftCell, rightVertex, rightCell, bottomVertex, bottomCell);
			}
			else
			{
				TriangulateCornerTerracesCliff(bottomVertex, bottomCell, leftVertex, leftCell, rightVertex, rightCell);
			}
		}
		else if (rightEdgeType == HexagonEdgeType.Slope)
		{
			if (leftEdgeType == HexagonEdgeType.Flat)
			{
				TriangulateCornerTerraces(rightVertex, rightCell, bottomVertex, bottomCell, leftVertex, leftCell);
			}
			else
			{
				TriangulateCornerCliffTerraces(bottomVertex, bottomCell, leftVertex, leftCell, rightVertex, rightCell);
			}
		}
		else if (leftCell.GetEdgeType(rightCell) == HexagonEdgeType.Slope)
		{
			if (leftCell.Elevation < rightCell.Elevation)
			{
				TriangulateCornerCliffTerraces(rightVertex, rightCell, bottomVertex, bottomCell, leftVertex, leftCell);
			}
			else
			{
				TriangulateCornerTerracesCliff(leftVertex, leftCell, rightVertex, rightCell, bottomVertex, bottomCell);
			}
		}
		else
		{
			terrain.AddTriangle(bottomVertex, leftVertex, rightVertex);
			terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
		}
		
		objectManager.AddWall(bottomVertex, bottomCell, leftVertex, leftCell, rightVertex, rightCell);
	}

	void TriangulateCornerTerraces(Vector3 bottomVertex, Cell bottomCell, Vector3 leftVertex, Cell leftCell,
		Vector3 rightVertex, Cell rightCell)
	{
		//Bottom triangle
		var intermediateLeftVertex = Metrics.TerraceLerp(bottomVertex, leftVertex, 1);
		var intermediateRightVertex = Metrics.TerraceLerp(bottomVertex, rightVertex, 1);
		var intermediateLeftColor = Metrics.TerraceLerp(bottomCell.Color, leftCell.Color, 1);
		var intermediateRightColor = Metrics.TerraceLerp(bottomCell.Color, rightCell.Color, 1);
		terrain.AddTriangle(bottomVertex, intermediateLeftVertex, intermediateRightVertex);
		terrain.AddTriangleColor(bottomCell.Color, intermediateLeftColor, intermediateRightColor);

		//Intermediate
		for (var i = 2; i < Metrics.SlopeSteps; i++)
		{
			var startLeftVertex = intermediateLeftVertex;
			var startRightVertex = intermediateRightVertex;
			var startLeftColor = intermediateLeftColor;
			var startRightColor = intermediateRightColor;
			intermediateLeftVertex = Metrics.TerraceLerp(bottomVertex, leftVertex, i);
			intermediateRightVertex = Metrics.TerraceLerp(bottomVertex, rightVertex, i);
			intermediateLeftColor = Metrics.TerraceLerp(bottomCell.Color, leftCell.Color, i);
			intermediateRightColor = Metrics.TerraceLerp(bottomCell.Color, rightCell.Color, i);
			
			terrain.AddQuad(startLeftVertex, startRightVertex, intermediateLeftVertex, intermediateRightVertex);
			terrain.AddQuadColor(startLeftColor, startRightColor, intermediateLeftColor, intermediateRightColor);
		}

		//Top Quad
		terrain.AddQuad(intermediateLeftVertex, intermediateRightVertex, leftVertex, rightVertex);
		terrain.AddQuadColor(intermediateLeftColor, intermediateRightColor, leftCell.Color, rightCell.Color);
	}

	void TriangulateCornerTerracesCliff(Vector3 bottomVertex, Cell bottomCell, Vector3 leftVertex, Cell leftCell,
		Vector3 rightVertex, Cell rightCell)
	{
		var terraceBindingVertex =
			Vector3.Lerp(Metrics.Perturb(bottomVertex), Metrics.Perturb(rightVertex), 1f / Math.Abs(rightCell.Elevation - bottomCell.Elevation));
		var terraceBindingColor = Color.Lerp(bottomCell.Color, rightCell.Color,
			1f / Math.Abs(rightCell.Elevation - bottomCell.Elevation));
		
		TriangulateBoundaryTriangle(bottomVertex, bottomCell, leftVertex, leftCell,
			terraceBindingVertex, terraceBindingColor);

		if (leftCell.GetEdgeType(rightCell) == HexagonEdgeType.Slope)
		{
			TriangulateBoundaryTriangle(leftVertex, leftCell, rightVertex, rightCell, terraceBindingVertex, terraceBindingColor);
		}
		else
		{
			terrain.AddTriangleUnperturbed(Metrics.Perturb(leftVertex), Metrics.Perturb(rightVertex), terraceBindingVertex);
			terrain.AddTriangleColor(leftCell.Color, rightCell.Color, terraceBindingColor);
		}
	}
	
	void TriangulateCornerCliffTerraces(Vector3 bottomVertex, Cell bottomCell, Vector3 leftVertex, Cell leftCell,
		Vector3 rightVertex, Cell rightCell)
	{
		var terraceBindingVertex =
			Vector3.Lerp(Metrics.Perturb(bottomVertex), Metrics.Perturb(leftVertex), 1f / Math.Abs(leftCell.Elevation - bottomCell.Elevation));
		var terraceBindingColor = Color.Lerp(bottomCell.Color, leftCell.Color,
			1f / Math.Abs(leftCell.Elevation - bottomCell.Elevation));
		
		TriangulateBoundaryTriangle(rightVertex, rightCell, bottomVertex, bottomCell, terraceBindingVertex, terraceBindingColor);

		if (leftCell.GetEdgeType(rightCell) == HexagonEdgeType.Slope)
		{
			TriangulateBoundaryTriangle(leftVertex, leftCell, rightVertex, rightCell, terraceBindingVertex, terraceBindingColor);
		}
		else
		{
			terrain.AddTriangleUnperturbed(Metrics.Perturb(leftVertex), Metrics.Perturb(rightVertex), terraceBindingVertex);
			terrain.AddTriangleColor(leftCell.Color, rightCell.Color, terraceBindingColor);
		}
	}

	void TriangulateBoundaryTriangle(Vector3 bottomVertex, Cell bottomCell, Vector3 leftVertex, Cell leftCell,
		Vector3 terraceBindingVertex, Color terraceBindingColor)
	{
		//First triangle
		var intermediateVertex = Metrics.Perturb(Metrics.TerraceLerp(bottomVertex, leftVertex, 1));
		var intermediateColor = Metrics.TerraceLerp(bottomCell.Color, leftCell.Color, 1);
		terrain.AddTriangleUnperturbed(Metrics.Perturb(bottomVertex), intermediateVertex, terraceBindingVertex);
		terrain.AddTriangleColor(bottomCell.Color, intermediateColor, terraceBindingColor);
		//Intermediate
		for (var i = 2; i < Metrics.SlopeSteps; i++)
		{
			var startVertex = intermediateVertex;
			var startColor = intermediateColor;
			intermediateVertex = Metrics.Perturb(Metrics.TerraceLerp(bottomVertex, leftVertex, i));
			intermediateColor = Metrics.TerraceLerp(bottomCell.Color, leftCell.Color, i);
			terrain.AddTriangleUnperturbed(startVertex, intermediateVertex, terraceBindingVertex);
			terrain.AddTriangleColor(startColor, intermediateColor, terraceBindingColor);
		}
		//Last triangle
		terrain.AddTriangleUnperturbed(intermediateVertex, Metrics.Perturb(leftVertex), terraceBindingVertex);
		terrain.AddTriangleColor(intermediateColor, leftCell.Color, terraceBindingColor);
	}

	void TriangulateEdgeFan(Vector3 center, EdgeVertices edges, Color color)
	{
		terrain.AddTriangle(center, edges.vertex1, edges.vertex2);
		terrain.AddTriangleColor(color, color, color);
		terrain.AddTriangle(center, edges.vertex2, edges.vertex3);
		terrain.AddTriangleColor(color, color, color);
		terrain.AddTriangle(center, edges.vertex3, edges.vertex4);
		terrain.AddTriangleColor(color, color, color);
		terrain.AddTriangle(center, edges.vertex4, edges.vertex5);
		terrain.AddTriangleColor(color, color, color);
	}

	void TriangulateEdgeStrip(EdgeVertices edges1, EdgeVertices edges2, Color color1, Color color2, bool hasRoad =  false)
	{
		terrain.AddQuad(edges1.vertex1, edges1.vertex2, edges2.vertex1, edges2.vertex2);
		terrain.AddQuadColor(color1, color2);
		terrain.AddQuad(edges1.vertex2, edges1.vertex3, edges2.vertex2, edges2.vertex3);
		terrain.AddQuadColor(color1, color2);
		terrain.AddQuad(edges1.vertex3, edges1.vertex4, edges2.vertex3, edges2.vertex4);
		terrain.AddQuadColor(color1, color2);
		terrain.AddQuad(edges1.vertex4, edges1.vertex5, edges2.vertex4, edges2.vertex5);
		terrain.AddQuadColor(color1, color2);

		if (hasRoad)
		{
			TriangulateRoadSegment(edges1.vertex2, edges1.vertex3, edges1.vertex4, edges2.vertex2, edges2.vertex3, edges2.vertex4);
		}
	}
	
	#endregion

	#region River

	void TriangulateRiverQuad(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 vertex4, float height1, float height2, float v, bool isReversed)
	{
		vertex1.y = vertex2.y = height1;
		vertex3.y = vertex4.y = height2;
		rivers.AddQuad(vertex1, vertex2, vertex3, vertex4);
		if (!isReversed)
		{
			rivers.AddQuadUv(0, 1, v, v + 0.2f);
		}
		else
		{
			rivers.AddQuadUv(1, 0, 0.8f - v, 0.6f - v);
		}
	}

	void TriangulateRiverQuad(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 vertex4, float height, float v,
		bool isReversed)
	{
		TriangulateRiverQuad(vertex1, vertex2, vertex3, vertex4, height, height, v, isReversed);
	}
	
	void TriangulateWithRiver(Direction direction, Cell cell, Vector3 center, EdgeVertices edges)
	{
		Vector3 channelLeftVertice;
		Vector3 channelRightVertice;
		if (cell.HasRiverAtDirection(direction.Opposite()))
		{
			channelLeftVertice = center + Metrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
			channelRightVertice = center + Metrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
		}
		else if (cell.HasRiverAtDirection(direction.Next()))
		{
			channelLeftVertice = center;
			channelRightVertice = Vector3.Lerp(center, edges.vertex5, 2f/3f);
		}
		else if (cell.HasRiverAtDirection(direction.Previous()))
		{
			channelLeftVertice = Vector3.Lerp(center, edges.vertex1, 2f/3f);
			channelRightVertice = center;
		}
		else if (cell.HasRiverAtDirection(direction.AfterNext()))
		{
			channelLeftVertice = center;
			channelRightVertice = center + Metrics.GetMiddleSolidCorner(direction.Next()) * 0.5f * Metrics.InnerToOuter;
		}
		else if (cell.HasRiverAtDirection(direction.PrePrevious()))
		{
			channelLeftVertice = center + Metrics.GetMiddleSolidCorner(direction.Previous()) * 0.5f * Metrics.InnerToOuter;
			channelRightVertice = center;
		}
		else
		{
			channelLeftVertice = channelRightVertice = center;
		}

		center = Vector3.Lerp(channelLeftVertice, channelRightVertice, 0.5f);
		
		var middleChannelVertices = new EdgeVertices(
			Vector3.Lerp(channelLeftVertice, edges.vertex1, 0.5f), 
			Vector3.Lerp(channelRightVertice, edges.vertex5, 0.5f), 
			1f / 6f );
		middleChannelVertices.vertex3.y = center.y = edges.vertex3.y;
		TriangulateEdgeStrip(middleChannelVertices, edges, cell.Color, cell.Color);
		
		terrain.AddTriangle(channelLeftVertice, middleChannelVertices.vertex1, middleChannelVertices.vertex2);
		terrain.AddTriangleColor(cell.Color, cell.Color, cell.Color);
		
		terrain.AddQuad(channelLeftVertice, center, middleChannelVertices.vertex2, middleChannelVertices.vertex3);
		terrain.AddQuadColor(cell.Color, cell.Color);
		terrain.AddQuad(center, channelRightVertice, middleChannelVertices.vertex3, middleChannelVertices.vertex4);
		terrain.AddQuadColor(cell.Color, cell.Color);
		
		terrain.AddTriangle(channelRightVertice, middleChannelVertices.vertex4, middleChannelVertices.vertex5);
		terrain.AddTriangleColor(cell.Color, cell.Color, cell.Color);

		if (!cell.IsUnderWater)
		{
			var isReversed = cell.IncomingRiverDirection == direction;
			TriangulateRiverQuad(channelLeftVertice, channelRightVertice, middleChannelVertices.vertex2, middleChannelVertices.vertex4, cell.RiverSurfaceY, 0.4f, isReversed);
			TriangulateRiverQuad(middleChannelVertices.vertex2, middleChannelVertices.vertex4, edges.vertex2, edges.vertex4, cell.RiverSurfaceY, 0.6f, isReversed);
		}
	}
	
	void TriangulateOneEndOfRiver(Direction direction, Cell cell, Vector3 center, EdgeVertices edges)
	{
		var middleChannelVertices = new EdgeVertices(
			Vector3.Lerp(center, edges.vertex1, 0.5f), 
			Vector3.Lerp(center, edges.vertex5, 0.5f));
		middleChannelVertices.vertex3.y = edges.vertex3.y;
		TriangulateEdgeStrip(middleChannelVertices, edges, cell.Color, cell.Color);
		TriangulateEdgeFan(center, middleChannelVertices, cell.Color);
		if (!cell.IsUnderWater)
		{
			TriangulateRiverQuad(middleChannelVertices.vertex2, middleChannelVertices.vertex4, edges.vertex2, edges.vertex4, cell.RiverSurfaceY, 0.6f, cell.HasIncomingRiver);

			center.y = middleChannelVertices.vertex2.y = middleChannelVertices.vertex4.y = cell.RiverSurfaceY;
			rivers.AddTriangle(center, middleChannelVertices.vertex2, middleChannelVertices.vertex4);
			if (cell.HasIncomingRiver)
			{
				rivers.AddTriangleUv(new Vector2(0.5f, 0.4f), new Vector2(1, 0.2f), new Vector2(0, 0.2f));
			}
			else
			{
				rivers.AddTriangleUv(new Vector2(0.5f, 0.4f), new Vector2(0, 0.6f), new Vector2(1, 0.6f));
			}
		}
	}

	void TriangulateAdjacentToRiver(Direction direction, Cell cell, Vector3 center, EdgeVertices edges)
	{
		if (cell.HasRoads)
		{
			TriangulateRoadsNearRiver(direction, cell, center, edges);
		}
		
		if (cell.HasRiverAtDirection(direction.Next()))
		{
			if (cell.HasRiverAtDirection(direction.Previous()))
			{
				center += Metrics.GetMiddleSolidCorner(direction) * Metrics.InnerToOuter * 0.5f;
			}
			else if (cell.HasRiverAtDirection(direction.PrePrevious()))
			{
				center += Metrics.GetFirstSolidCorner(direction) * 0.25f;
			}
		}
		else if (cell.HasRiverAtDirection(direction.Previous()) && cell.HasRiverAtDirection(direction.AfterNext()))
		{
			center += Metrics.GetSecondSolidCorner(direction) * 0.25f;
		}
		
		var middleChannelVertices = new EdgeVertices(
			Vector3.Lerp(center, edges.vertex1, 0.5f), 
			Vector3.Lerp(center, edges.vertex5, 0.5f));
		middleChannelVertices.vertex3.y = edges.vertex3.y;
		TriangulateEdgeStrip(middleChannelVertices, edges, cell.Color, cell.Color);
		TriangulateEdgeFan(center, middleChannelVertices, cell.Color);
		
		if (!cell.IsUnderWater && !cell.HasRoadAtDirection(direction)) {
			objectManager.AddObjects((center + edges.vertex1 + edges.vertex5)/3, cell);
		}
	}

	void TriangulateWithoutRiver(Direction direction, Cell cell, Vector3 center, EdgeVertices edges)
	{
		TriangulateEdgeFan(center, edges, cell.Color);

		if (cell.HasRoads)
		{
			var interpolators = GetRoadInterpolators(direction, cell);
			TriangulateRoad(center, Vector3.Lerp(center, edges.vertex1, interpolators.x), Vector3.Lerp(center, edges.vertex5, interpolators.y), edges, cell.HasRoadAtDirection(direction));
		}
	}

	void TriangulateWaterFallInWater(Vector3 topLeftVertex, Vector3 topRightVertex, Vector3 bottomLeftVertex,
		Vector3 bottomRightVertex, float topY, float bottomY, float waterY)
	{
		topLeftVertex.y = topRightVertex.y = topY;
		bottomLeftVertex.y = bottomRightVertex.y = bottomY;

		topLeftVertex = Metrics.Perturb(topLeftVertex);
		topRightVertex = Metrics.Perturb(topRightVertex);
		bottomLeftVertex = Metrics.Perturb(bottomLeftVertex);
		bottomRightVertex = Metrics.Perturb(bottomRightVertex);
		
		var partBelowWater = (waterY - bottomY) / (topY - bottomY);
		var surfaceLeftVertex = Vector3.Lerp(bottomLeftVertex, topLeftVertex, partBelowWater);
		var surfaceRightVertex = Vector3.Lerp(bottomRightVertex, topRightVertex, partBelowWater);
		rivers.AddQuadUnperturbed(topLeftVertex, topRightVertex, surfaceLeftVertex, surfaceRightVertex);
		rivers.AddQuadUv(0, 1, 0.8f, 1);
	}

	#endregion
	
	#region Roads
	
	void TriangulateRoadSegment(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 vertex4, Vector3 vertex5,
		Vector3 vertex6)
	{
		roads.AddQuad(vertex1, vertex2, vertex4, vertex5);
		roads.AddQuad(vertex2, vertex3, vertex5, vertex6);
		roads.AddQuadUv(0f, 1f, 0f, 0f);
		roads.AddQuadUv(1f, 0f, 0f, 0f);
	}

	void TriangulateRoad(Vector3 center, Vector3 roadLeftVertex, Vector3 roadRightVertex, EdgeVertices edges, bool hasRoadAtDirection)
	{
		if (hasRoadAtDirection)
		{
			var roadMiddleVertex = Vector3.Lerp(roadLeftVertex, roadRightVertex, 0.5f);
			TriangulateRoadSegment(roadLeftVertex, roadMiddleVertex, roadRightVertex, edges.vertex2, edges.vertex3, edges.vertex4);
			roads.AddTriangle(center, roadLeftVertex, roadMiddleVertex);
			roads.AddTriangle(center, roadMiddleVertex, roadRightVertex);
			roads.AddTriangleUv(
				new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f)
			);
			roads.AddTriangleUv(
				new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f)
			);
		}
		else
		{
			TriangulateRoadEnd(center, roadLeftVertex, roadRightVertex);
		}
	}

	void TriangulateRoadEnd(Vector3 center, Vector3 roadLeftVertex, Vector3 roadRightVertex)
	{
		roads.AddTriangle(center, roadLeftVertex, roadRightVertex);
		roads.AddTriangleUv(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
	}

	void TriangulateRoadsNearRiver(Direction direction, Cell cell, Vector3 center, EdgeVertices edges)
	{
		var interpolators = GetRoadInterpolators(direction, cell);
		var roadCenter = center;
		var previousDirectionHasRiver = cell.HasRiverAtDirection(direction.Previous());
		var nextDirectionHasRiver = cell.HasRiverAtDirection(direction.Next());

		if (cell.HasOneEndOfRiver)
		{
			roadCenter += Metrics.GetMiddleSolidCorner(cell.RiverDirection.Opposite()) / 3f;
		}
		else if (cell.IncomingRiverDirection == cell.OutGoingRiverDirection.Opposite())
		{
			Vector3 corner;
			if (previousDirectionHasRiver)
			{
				if (!cell.HasRoadAtDirection(direction) && !cell.HasRoadAtDirection(direction.Next()))
				{
					return;
				}
				corner = Metrics.GetSecondSolidCorner(direction);
			}
			else
			{
				if (!cell.HasRoadAtDirection(direction) && !cell.HasRoadAtDirection(direction.Previous()))
				{
					return;
				}
				corner = Metrics.GetFirstSolidCorner(direction);
			}

			roadCenter += corner * 0.5f;
			if (cell.IncomingRiverDirection == direction.Next() && (cell.HasRoadAtDirection(direction.AfterNext()) || cell.HasRoadAtDirection(direction.Opposite())))
			{
				objectManager.AddBridgeStraightRiver(roadCenter, center - corner * 0.5f);
			}
			center += corner * 0.25f;
		}
		else if (cell.IncomingRiverDirection == cell.OutGoingRiverDirection.Previous())
		{
			roadCenter -= Metrics.GetSecondCorner(cell.IncomingRiverDirection) * 0.2f;
		}
		else if (cell.IncomingRiverDirection == cell.OutGoingRiverDirection.Next())
		{
			roadCenter -= Metrics.GetFirstCorner(cell.IncomingRiverDirection) * 0.2f;
		}
		else if (previousDirectionHasRiver && nextDirectionHasRiver)
		{
			if (!cell.HasRoadAtDirection(direction))
			{
				return;
			}
			var offset = Metrics.GetMiddleSolidCorner(direction) * Metrics.InnerToOuter;
			roadCenter += offset * 0.7f;
			center += offset * 0.5f;
		}
		else
		{
			Direction middleDirection;
			if (previousDirectionHasRiver)
			{
				middleDirection = direction.Next();
			}
			else if (nextDirectionHasRiver)
			{
				middleDirection = direction.Previous();
			}
			else
			{
				middleDirection = direction;
			}

			if (!cell.HasRoadAtDirection(middleDirection) && !cell.HasRoadAtDirection(middleDirection.Next()) &&
			    !cell.HasRoadAtDirection(middleDirection.Previous()))
			{
				return;
			}

			var offset = Metrics.GetMiddleSolidCorner(middleDirection);
			roadCenter += offset * 0.25f;
			if (direction == middleDirection && cell.HasRoadAtDirection(direction.Opposite()))
			{
				objectManager.AddBridgeCurvedRiver(roadCenter, center - offset * Metrics.InnerToOuter * 0.7f);
			}
		}
		
		
		var roadLeftVertex = Vector3.Lerp(roadCenter, edges.vertex1, interpolators.x);
		var roadRightVertex = Vector3.Lerp(roadCenter, edges.vertex5, interpolators.y);
		TriangulateRoad(roadCenter, roadLeftVertex, roadRightVertex, edges, cell.HasRoadAtDirection(direction));

		if (previousDirectionHasRiver)
		{
			TriangulateRoadEnd(roadCenter, center, roadLeftVertex);
		}
		if (nextDirectionHasRiver)
		{
			TriangulateRoadEnd(roadCenter, roadRightVertex, center);
		}
	}

	static Vector2 GetRoadInterpolators(Direction direction, Cell cell)
	{
		var interpolators = new Vector2();
		if (cell.HasRoadAtDirection(direction))
		{
			interpolators.x = interpolators.y = 0.5f;
		}
		else
		{
			interpolators.x = cell.HasRoadAtDirection(direction.Previous()) ? 0.5f : 0.25f;
			interpolators.y = cell.HasRoadAtDirection(direction.Next()) ? 0.5f : 0.25f;
		}
		return interpolators;
	}

	void TriangulateWater(Direction direction, Cell cell, Vector3 center)
	{
		center.y = cell.WaterSurfaceY;
		
		var neighbour = cell.GetNeighbour(direction);

		if (neighbour != null && !neighbour.IsUnderWater)
		{
			TriangulateShoreWater(direction, cell, neighbour, center);
		}
		else
		{
			TriangulateOpenWater(direction, cell, neighbour, center);
		}
	}
#endregion

	#region Water

	void TriangulateOpenWater(Direction direction, Cell cell, Cell neighbour, Vector3 center)
	{
		var corner1 = center + Metrics.GetFirstWaterCorner(direction);
		var corner2 = center + Metrics.GetSecondWaterCorner(direction);
		
		openWater.AddTriangle(center, corner1, corner2);
		
		if (direction <= Direction.SouthEast && neighbour != null)
		{
			var bridge = Metrics.GetWaterBridge(direction);
			var edge1 = corner1 + bridge;
			var edge2 = corner2 + bridge;
			
			openWater.AddQuad(corner1, corner2, edge1, edge2);

			if (direction <= Direction.East)
			{
				var nextNeighbour = cell.GetNeighbour(direction.Next());
				if (!nextNeighbour || !nextNeighbour.IsUnderWater)
				{
					return;
				}
				
				openWater.AddTriangle(corner2, edge2, corner2 + Metrics.GetWaterBridge(direction.Next()));
			}
		}
	}

	void TriangulateShoreWater(Direction direction, Cell cell, Cell neighbour, Vector3 center)
	{
		var edges1 = new EdgeVertices(center + Metrics.GetFirstWaterCorner(direction), center + Metrics.GetSecondWaterCorner(direction));
		
		openWater.AddTriangle(center, edges1.vertex1, edges1.vertex2);
		openWater.AddTriangle(center, edges1.vertex2, edges1.vertex3);
		openWater.AddTriangle(center, edges1.vertex3, edges1.vertex4);
		openWater.AddTriangle(center, edges1.vertex4, edges1.vertex5);

		var center2 = neighbour.Position;
		center2.y = center.y;
		var edges2 = new EdgeVertices(center2 + Metrics.GetSecondSolidCorner(direction.Opposite()), center2 + Metrics.GetFirstSolidCorner(direction.Opposite()));

		if (cell.HasRiverAtDirection(direction))
		{
			TriangulateEstuary(edges1, edges2, cell.IncomingRiverDirection == direction);
		}
		else
		{
			shoreWater.AddQuad(edges1.vertex1, edges1.vertex2, edges2.vertex1, edges2.vertex2);
			shoreWater.AddQuad(edges1.vertex2, edges1.vertex3, edges2.vertex2, edges2.vertex3);
			shoreWater.AddQuad(edges1.vertex3, edges1.vertex4, edges2.vertex3, edges2.vertex4);
			shoreWater.AddQuad(edges1.vertex4, edges1.vertex5, edges2.vertex4, edges2.vertex5);
			shoreWater.AddQuadUv(0f, 0f, 0f, 1f);
			shoreWater.AddQuadUv(0f, 0f, 0f, 1f);
			shoreWater.AddQuadUv(0f, 0f, 0f, 1f);
			shoreWater.AddQuadUv(0f, 0f, 0f, 1f);
		}
		
		var nextNeighbour = cell.GetNeighbour(direction.Next());
		if (nextNeighbour)
		{
			var center3 = nextNeighbour.Position + (nextNeighbour.IsUnderWater ? Metrics.GetFirstWaterCorner(direction.Previous()) : Metrics.GetFirstSolidCorner(direction.Previous()));
			center3.y = center.y;
			shoreWater.AddTriangle(edges1.vertex5, edges2.vertex5, center3);
			shoreWater.AddTriangleUv(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, nextNeighbour.IsUnderWater ? 0f : 1f));
		}
	}

	void TriangulateEstuary(EdgeVertices edges1, EdgeVertices edges2, bool isRiverIncomingIntoWaterPool)
	{
		shoreWater.AddTriangle(edges2.vertex1, edges1.vertex2, edges1.vertex1);
		shoreWater.AddTriangleUv(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
		
		shoreWater.AddTriangle(edges2.vertex5, edges1.vertex5, edges1.vertex4);
		shoreWater.AddTriangleUv(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
		
		estuaries.AddQuad(edges2.vertex1, edges1.vertex2, edges2.vertex2, edges1.vertex3);
		estuaries.AddQuadUv(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
		
		estuaries.AddTriangle(edges1.vertex3, edges2.vertex2, edges2.vertex4);
		estuaries.AddTriangleUv(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f));
		
		estuaries.AddQuad(edges1.vertex3, edges1.vertex4, edges2.vertex4, edges2.vertex5);
		estuaries.AddQuadUv(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));

		if (isRiverIncomingIntoWaterPool)
		{
			AddIncomingEstuaryUv2();
		}
		else
		{
			AddOutgoingEstuaryUv2();
		}
	}

	void AddIncomingEstuaryUv2()
	{
		estuaries.AddQuadUv2(new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f), new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
		estuaries.AddTriangleUv2(new Vector2(0.5f, 1.1f), new Vector2(1f, 0.8f), new Vector2(0f, 0.8f));
		estuaries.AddQuadUv2(new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f), new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f));
	}

	void AddOutgoingEstuaryUv2()
	{
		estuaries.AddQuadUv2(new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f), new Vector2(0f, 0f), new Vector2(0.5f, -0.3f));
		estuaries.AddTriangleUv2(new Vector2(0.5f, -0.3f), new Vector2(0f, 0f), new Vector2(1f, 0f));
		estuaries.AddQuadUv2(new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f), new Vector2(1f, 0f), new Vector2(1.5f, -0.2f));
	}
	#endregion
	
}
