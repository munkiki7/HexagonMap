using System;
using UnityEngine;

public class Cell : MonoBehaviour
{
	public HexagonCoordinates coordinates;

	Color _color;

	public Color Color
	{
		get { return _color; }
		set
		{
			if (_color == value)
			{
				return;
			}

			_color = value;
			Refresh();
		}
	}

	public RectTransform labelHolder;

	public FieldChunk chunk;

	int _elevation = int.MinValue;

	public int Elevation
	{
		get { return _elevation; }
		set
		{
			if (Elevation == value)
			{
				return;
			}

			_elevation = value;
			var position = transform.localPosition;
			position.y = value * Metrics.ElevationStep;
			position.y += Metrics.SampleNoise(position).y * 2f - 1;
			transform.localPosition = position;

			var newPosition = labelHolder.localPosition;
			newPosition.z = -position.y;
			labelHolder.localPosition = newPosition;

			ValidateRivers();

			RefreshRoads();
			
			Refresh();
		}
	}

	int _waterLevel;

	public int WaterLevel
	{
		get { return _waterLevel; }
		set
		{
			if (_waterLevel == value)
			{
				return;
			}

			_waterLevel = value;
			ValidateRivers();
			Refresh();
		}
	}

	public bool IsUnderWater
	{
		get { return _waterLevel > _elevation; }
	}

	public Vector3 Position
	{
		get { return transform.localPosition; }
	}

	int _urbanizationLevel;

	public int UrbanizationLevel
	{
		get { return _urbanizationLevel; }
		set
		{
			if (_urbanizationLevel != value)
			{
				_urbanizationLevel = value;
				SelfRefresh();
			}
		}
	}

	bool _isWalled;

	public bool IsWalled
	{
		get { return _isWalled; }
		set
		{
			if (_isWalled != value)
			{
				_isWalled = value;
				Refresh();
			}
		}
	}

	[SerializeField]
	Cell[] _neighbours;
	
	[SerializeField]
	bool[] _roads;

	bool hasIncomingRiver, hasOutGoingRiver;

	Direction incomingRiverDirection, outGoingRiverDirection;

	bool IsValidRiverDestination(Cell neighbour)
	{
		return neighbour && (_elevation >= neighbour._elevation || _waterLevel >= neighbour._elevation);
	}

	public bool HasIncomingRiver
	{
		get { return hasIncomingRiver; }
	}

	public bool HasOutGoingRiver
	{
		get { return hasOutGoingRiver; }
	}

	public Direction IncomingRiverDirection
	{
		get { return incomingRiverDirection; }
	}

	public Direction OutGoingRiverDirection
	{
		get { return outGoingRiverDirection; }
	}

	public Direction RiverDirection
	{
		get { return hasIncomingRiver ? incomingRiverDirection : outGoingRiverDirection; }
	}

	public bool HasRiver
	{
		get { return hasIncomingRiver || hasOutGoingRiver; }
	}

	public bool HasOneEndOfRiver
	{
		get { return hasIncomingRiver != hasOutGoingRiver; }
	}

	public bool HasRiverAtDirection(Direction direction)
	{
		return HasIncomingRiver && IncomingRiverDirection == direction ||
		       HasOutGoingRiver && OutGoingRiverDirection == direction;
	}

	public float RiverBedY
	{
		get { return (_elevation + Metrics.RiverBeadOffset) * Metrics.ElevationStep; }
	}
	
	public float RiverSurfaceY
	{
		get { return (_elevation + Metrics.WaterElevationOffset) * Metrics.ElevationStep; }
	}
	
	public float WaterSurfaceY
	{
		get { return (_waterLevel + Metrics.WaterElevationOffset) * Metrics.ElevationStep; }
	}

	public bool HasRoadAtDirection(Direction direction)
	{
		return _roads[(int) direction];
	}

	public bool HasRoads
	{
		get
		{
			foreach (var road in _roads)
			{
				if (road)
				{
					return true;
				}
			}

			return false;
		}
	}

	public int ElevationDifferenceAtDirection(Direction direction)
	{
		return Math.Abs(_elevation - _neighbours[(int) direction]._elevation);
	}

	
	void RefreshRoads()
	{
		for (var i = 0; i < _roads.Length; i++)
		{
			if (_roads[i] && ElevationDifferenceAtDirection((Direction) i) > 2)
			{
				SetRoad(i, false);
			}
		}
	}

	public Cell GetNeighbour(Direction direction)
	{
		return _neighbours[(int) direction];
	}

	public void SetNeighbour(Direction direction, Cell cell)
	{
		_neighbours[(int) direction] = cell;
		cell._neighbours[(int) direction.Opposite()] = this;
	}

	public HexagonEdgeType GetEdgeType(Direction direction)
	{
		if (_neighbours[(int) direction] != null)
		{
			return Metrics.GetEdgeType(Elevation, _neighbours[(int) direction].Elevation);
		}

		return HexagonEdgeType.Flat;
	}

	public HexagonEdgeType GetEdgeType(Cell other)
	{
		return Metrics.GetEdgeType(Elevation, other.Elevation);
	}

	void Refresh()
	{
		if (chunk)
		{
			chunk.Refresh();
			foreach (var neighbour in _neighbours)
			{
				if (neighbour != null && neighbour.chunk != chunk)
				{
					neighbour.chunk.Refresh();
				}
			}
		}
	}

	void SelfRefresh()
	{
		chunk.Refresh();
	}

	public void RemoveOutGoingRiver()
	{
		if (!hasOutGoingRiver) return;
		hasOutGoingRiver = false;
		SelfRefresh();

		var neighbour = GetNeighbour(outGoingRiverDirection);
		neighbour.hasIncomingRiver = false;
		neighbour.SelfRefresh();
	}
	
	public void RemoveIncomingRiver()
	{
		if (!hasIncomingRiver) return;
		hasIncomingRiver = false;
		SelfRefresh();

		var neighbour = GetNeighbour(incomingRiverDirection);
		neighbour.hasOutGoingRiver = false;
		neighbour.SelfRefresh();
	}

	public void RemoveRiver()
	{
		RemoveIncomingRiver();
		RemoveOutGoingRiver();
	}

	public void CreateOutGoingRiver(Direction direction)
	{
		if (hasOutGoingRiver && outGoingRiverDirection == direction) return;
		
		var neighbour = GetNeighbour(direction);
		if(!IsValidRiverDestination(neighbour)) return;
		
		RemoveOutGoingRiver();

		if (hasIncomingRiver && incomingRiverDirection == direction)
		{
			RemoveIncomingRiver();
		}

		hasOutGoingRiver = true;
		outGoingRiverDirection = direction;
		
		neighbour.RemoveIncomingRiver();
		neighbour.hasIncomingRiver = true;
		neighbour.incomingRiverDirection = direction.Opposite();
		
		SetRoad((int) direction, false);
	}

	public void CreateRoad(Direction direction)
	{
		if (!_roads[(int) direction] && !HasRiverAtDirection(direction) && ElevationDifferenceAtDirection(direction) < 3)
		{
			SetRoad((int) direction, true);
		}
	}
	
	public void RemoveRoads()
	{
		for (var i = 0; i < _roads.Length; i++)
		{
			SetRoad(i, false);
		}
	}

	void SetRoad(int index, bool state)
	{
		_roads[index] = state;
		if (_neighbours[index])
		{
			_neighbours[index]._roads[(int)((Direction) index).Opposite()] = state;
			_neighbours[index].SelfRefresh();
		}
		SelfRefresh();
	}

	void ValidateRivers()
	{
		if (HasOutGoingRiver && !IsValidRiverDestination(GetNeighbour(outGoingRiverDirection)))
		{
			RemoveOutGoingRiver();
		}

		if (hasIncomingRiver && !IsValidRiverDestination(GetNeighbour(incomingRiverDirection)))
		{
			RemoveIncomingRiver();
		}
	}
}