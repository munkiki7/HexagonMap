using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapEditor : MonoBehaviour
{
	public Color[] colors;

	public Field field;

	Color _activeColor;

	int _activeElevation;

	int _activeWaterLevel;
	
	int _brushSize;

	int _activeUrbanizationLevel;

	bool _applyColor;

	bool _applyElevation = true;

	bool _applyWaterLevel = true;

	bool _applyUrbanizationLevel = true;
	
	enum EditMode
	{Ignore = 0, Create = 1, Delete = 2}
	
	EditMode _editRiverMode, _editRoadMode;

	bool _isDragActive;
	Direction _dragDirection;
	Cell _previousCell;

	void Awake () 
	{
		SelectColor(0);
	}
	
	void Update () 
	{
		if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) 
		{
			HandleInput();
		}
		else
		{
			_previousCell = null;
		}
	}
	
	void HandleInput()
	{
		if (Camera.main != null)
		{
			var clickRay = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(clickRay, out hit))
			{
				var currentCell = field.GetCell(hit.point);
				if (_previousCell && _previousCell != currentCell)
				{
					ValidateDrag(currentCell);
				}
				else
				{
					_isDragActive = false;
				}
				EditCells(currentCell);
				_previousCell = currentCell;
			}
		}
		else
		{
			_previousCell = null;
		}
	}
	
	void EditCells(Cell centerCell)
	{
		var centerX = centerCell.coordinates.x;
		var centerZ = centerCell.coordinates.z;

		for (int row = 0, z = centerZ - _brushSize; z <= centerZ; z++, row++)
		{
			for (var x = centerX - row; x <= centerX + _brushSize; x++)
			{
				EditCell(field.GetCell(new HexagonCoordinates(x, z)));
			}
		}
		
		for (int row = 0, z = centerZ + _brushSize; z > centerZ; z--, row++)
		{
			for (var x = centerX - _brushSize; x <= centerX + row; x++)
			{
				EditCell(field.GetCell(new HexagonCoordinates(x, z)));
			}
		}
	}
	
	void EditCell(Cell cell)
	{
		if (!cell) return;
		if (_applyColor)
		{
			cell.Color = _activeColor;
		}

		if (_applyElevation)
		{
			cell.Elevation = _activeElevation;
		}

		if (_applyWaterLevel)
		{
			cell.WaterLevel = _activeWaterLevel;
		}

		if (_applyUrbanizationLevel)
		{
			cell.UrbanizationLevel = _activeUrbanizationLevel;
		}

		if (_editRiverMode == EditMode.Delete)
		{
			cell.RemoveRiver();
		}
		
		if (_editRoadMode == EditMode.Delete)
		{
			cell.RemoveRoads();
		}
		
		else if (_isDragActive)
		{
			var otherCell = cell.GetNeighbour(_dragDirection.Opposite());
			if (otherCell)
			{
				if (_editRiverMode == EditMode.Create)
				{
					otherCell.CreateOutGoingRiver(_dragDirection);
				}
				if (_editRoadMode == EditMode.Create)
				{
					otherCell.CreateRoad(_dragDirection);
				}
			}
		}
	}

	void ValidateDrag(Cell currentCell)
	{
		for (_dragDirection = Direction.NorthEast; _dragDirection <= Direction.NorthWest; _dragDirection++)
		{
			if (_previousCell.GetNeighbour(_dragDirection) == currentCell)
			{
				_isDragActive = true;
				return;
			}
		}

		_isDragActive = false;
	}

	[UsedImplicitly]
	public void SelectColor (int index)
	{
		_applyColor = index >= 0;
		if (_applyColor)
		{
			_activeColor = colors[index];
		}
	}

	[UsedImplicitly]
	public void SetElevation(float elevation)
	{
		_activeElevation = (int) elevation;
	}

	[UsedImplicitly]
	public void SetApplyElevation(bool toggle)
	{
		_applyElevation = toggle;
	}
	
	[UsedImplicitly]
	public void SetBrushSize(float brushSize)
	{
		_brushSize = (int) brushSize;
	}
	
	[UsedImplicitly]
	public void SetRiverMode(int mode)
	{
		_editRiverMode = (EditMode) mode;
	}
	
	[UsedImplicitly]
	public void SetRoadMode(int mode)
	{
		_editRoadMode = (EditMode) mode;
	}
	
	[UsedImplicitly]
	public void SetApplyWaterLevel(bool toggle)
	{
		_applyWaterLevel = toggle;
	}
	
	[UsedImplicitly]
	public void SetWaterLevel(float waterLevel)
	{
		_activeWaterLevel = (int) waterLevel;
	}

	[UsedImplicitly]
	public void SetApplyUrbanizationLevel(bool toggle)
	{
		_applyUrbanizationLevel = toggle;
	}

	[UsedImplicitly]
	public void SetUrbanizationLevel(float level)
	{
		_activeUrbanizationLevel = (int) level;
	}
}
