using UnityEngine;
using UnityEngine.UI;

public class Field : MonoBehaviour
{
	int _cellCountX, _cellCountZ;

	public int chunkCountX = 4, chunkCountZ = 3;

	public Cell cellPrefab;

	public FieldChunk chunkPrefab;

	public Text coordsTextPrefab;

	public Texture2D noisePicture;

	public int randomSeed;
	
	Color _defaultColor = Color.white;

	Cell[] _cells;

	FieldChunk[] _chunks;

	void Awake()
	{
		Metrics.noisePicture = noisePicture;
		Metrics.InitializeHashGrid(randomSeed);
		_cellCountX = chunkCountX * Metrics.ChunkSizeX;
		_cellCountZ = chunkCountZ * Metrics.ChunkSizeZ;
		
		CreateChunks();
		CreateCells();
	}

	void OnEnable()
	{
		if (!Metrics.noisePicture)
		{
			Metrics.noisePicture = noisePicture;
			Metrics.InitializeHashGrid(randomSeed);
		}
	}

	void CreateCells()
	{
		_cells = new Cell[_cellCountZ*_cellCountX];
		for (int z = 0, i = 0; z < _cellCountZ; z++) 
		{
			for (var x = 0; x < _cellCountX; x++) 
			{
				CreateCell(x, z, i++);
			}
		}
	}

	void CreateChunks()
	{
		_chunks = new FieldChunk[chunkCountX*chunkCountZ];
		for (int z = 0, i = 0; z < chunkCountZ; z++)
		{
			for (var x = 0; x < chunkCountX; x++)
			{
				_chunks[i] = Instantiate(chunkPrefab);
				_chunks[i].transform.SetParent(transform);
				i++;
			}
		}
	}

	void CreateCell(int x, int z, int i)
	{
		Vector3 position;
		if (z % 2 == 0)
		{
			position.x = x * (2f * Metrics.InnerRadius);
		}
		else
		{
			position.x = (x + 0.5f) * (2f * Metrics.InnerRadius);
		}
		position.y = 0f;
		position.z = z * (1.5f * Metrics.OuterRadius);

		_cells[i] = Instantiate(cellPrefab);
		_cells[i].transform.localPosition = position;
		_cells[i].coordinates = HexagonCoordinates.FromOffsetCoordinates(x, z);

		_cells[i].Color = _defaultColor;
		//Connecting Neighbours
		if (x > 0)
		{
			_cells[i].SetNeighbour(Direction.West, _cells[i-1]);
		}

		if (z > 0)
		{
			if (z % 2 == 0)
			{
				_cells[i].SetNeighbour(Direction.SouthEast, _cells[i - _cellCountX]);
				if (x > 0)
				{
					_cells[i].SetNeighbour(Direction.SouthWest, _cells[i - _cellCountX - 1]);
				}
			}
			else
			{
				_cells[i].SetNeighbour(Direction.SouthWest, _cells[i - _cellCountX]);
				if (x < _cellCountX - 1)
				{
					_cells[i].SetNeighbour(Direction.SouthEast, _cells[i - _cellCountX + 1]);
				}
			}
		}

		var coords = Instantiate(coordsTextPrefab);
		coords.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
		coords.text = _cells[i].coordinates.ToStringOnSeparateLines();
		_cells[i].labelHolder = coords.rectTransform;
		_cells[i].Elevation = 0;
		
		AddCellToChunk(x, z, _cells[i]);
	}

	public Cell GetCell(Vector3 position)
	{
		position = transform.InverseTransformPoint(position);
		var coordinates = HexagonCoordinates.FromPosition(position);
		var index = coordinates.x + coordinates.z * _cellCountX + coordinates.z / 2;
		return _cells[index];
	}

	public Cell GetCell(HexagonCoordinates coordinates)
	{
		var z = coordinates.z;
		if (z < 0 || z >= _cellCountZ)
		{
			return null;
		}
		var x = coordinates.x + z / 2;
		if (x < 0 || x >= _cellCountX)
		{
			return null;
		}
		return _cells[x + z * _cellCountX];
	}

	void AddCellToChunk(int x, int z, Cell cell)
	{
		var chunkX = x / Metrics.ChunkSizeX;
		var chunkZ = z / Metrics.ChunkSizeZ;

		var chunk = _chunks[chunkX + chunkZ * chunkCountX];
		var cellX = x - chunkX * Metrics.ChunkSizeX;
		var cellZ = z - chunkZ * Metrics.ChunkSizeZ;
		
		chunk.AddCell(cellX + cellZ * Metrics.ChunkSizeX, cell);
	}

	public void ShowLabels(bool show)
	{
		foreach (var chunk in _chunks)
		{
			chunk.ShowLabels(show);
		}
	}
}
