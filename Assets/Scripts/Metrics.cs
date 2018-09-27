using System;
using UnityEngine;
using Random = UnityEngine.Random;

public static class Metrics
{
	//Hexagon basic constants
	public const float OuterToInner = 0.866025404f;
	public const float InnerToOuter = 1f / OuterToInner;
	public const float OuterRadius = 10f;
	public const float InnerRadius = OuterRadius * OuterToInner;
	const float SolidFactor = 0.8f;
	const float BlendFactor = 1 - SolidFactor;
	public const float WaterFactor = 0.6f;
	public const float WaterBlendFactor = 1 - WaterFactor;
	//Elevation module
	public const float ElevationStep = 2f;
	const int TerracesNumber = 2;
	public const int SlopeSteps = TerracesNumber * 2 + 1;
	const float TerraceHorizontalStepSize = 1f / SlopeSteps;
	const float TerraceVerticalStepSize = 1f /(TerracesNumber + 1) ;
	//Noise module
	public static Texture2D noisePicture;
	public const float PerturbationStrength = 4f;
	const float NoiseScale = 0.003f;
	//Map chunks
	public const int ChunkSizeX = 5, ChunkSizeZ = 5;
	//Rivers
	public const float RiverBeadOffset = -1.75f;
	public const float WaterElevationOffset = -0.5f;
	//Object generation
	public const int HashGridSize = 256;
	public const float HashGridScale = 0.25f;
	public const float WallHeight = 4f;
	public const float WallThickness = 0.75f;
	public const float WallElevationOffset = TerraceVerticalStepSize;
	public const float TowerExistenceProbability = 0.5f;
	public const float WallYOffset = -1f;

	static readonly Vector3[] Corners = 
	{
		new Vector3(0f, 0f, OuterRadius),
		new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
		new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
		new Vector3(0f, 0f, -OuterRadius),
		new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
		new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius),
		new Vector3(0f, 0f, OuterRadius)
	};

	static readonly float[][] HouseThesholds =
	{
		new[] {0, 0, 0.4f},
		new[] {0, 0.4f, 0.6f},
		new[] {0.4f, 0.6f, 0.8f}
	};

	static PseudoRandom[] _hashGrid;
	
	public static Vector3 GetFirstCorner(Direction direction)
	{
		return Corners[(int) direction];
	}
	
	public static Vector3 GetSecondCorner(Direction direction)
	{
		return Corners[(int) direction + 1];
	}

	public static Vector3 GetFirstSolidCorner(Direction direction)
	{
		return Corners[(int) direction] * SolidFactor;
	}
	
	public static Vector3 GetSecondSolidCorner(Direction direction)
	{
		return Corners[(int) direction + 1] * SolidFactor;
	}
	
	public static Vector3 GetMiddleSolidCorner(Direction direction)
	{
		return (Corners[(int) direction] + Corners[(int) direction + 1]) * SolidFactor * 0.5f;
	}
	
	public static Vector3 GetFirstWaterCorner(Direction direction)
	{
		return Corners[(int) direction] * WaterFactor;
	}
	
	public static Vector3 GetSecondWaterCorner(Direction direction)
	{
		return Corners[(int) direction + 1] * WaterFactor;
	}

	public static Vector3 GetBridge(Direction direction)
	{
		return (Corners[(int) direction] + Corners[(int) direction + 1]) * BlendFactor;
	}
	
	public static Vector3 GetWaterBridge(Direction direction)
	{
		return (Corners[(int) direction] + Corners[(int) direction + 1]) * WaterBlendFactor;
	}

	public static Vector3 TerraceLerp(Vector3 minValue, Vector3 maxValue, int step)
	{
		var horizontalShift = step * TerraceHorizontalStepSize;
		var currentValue = minValue;
		currentValue.x += horizontalShift * (maxValue.x - minValue.x);
		currentValue.z += horizontalShift * (maxValue.z - minValue.z);
		
		var verticalShift = (step + 1) / 2 * TerraceVerticalStepSize; //Don't convert to 2f
		currentValue.y += (maxValue.y - minValue.y) * verticalShift;
		
		return currentValue;
	}

	public static Color TerraceLerp(Color minColor, Color maxColor, int step)
	{
		var colorShift = step * TerraceHorizontalStepSize;
		return Color.Lerp(minColor, maxColor, colorShift);
	}

	public static HexagonEdgeType GetEdgeType(int elevation1, int elevation2)
	{
		var difference = elevation1 - elevation2;
		if (difference == 0)
		{
			return HexagonEdgeType.Flat;
		}
		if (Math.Abs(difference) <= 2)
		{
			return HexagonEdgeType.Slope;
		}
		return HexagonEdgeType.Cliff;
	}

	public static Vector4 SampleNoise(Vector3 position)
	{
		return noisePicture.GetPixelBilinear(position.x * NoiseScale, position.z * NoiseScale);
	}
	
	public static Vector3 Perturb(Vector3 position)
	{
		var noiseSample = SampleNoise(position);
		position.x += (noiseSample.x * 2f - 1) * PerturbationStrength;
		position.z += (noiseSample.z * 2f - 1) * PerturbationStrength;
		return position;
	}

	public static void InitializeHashGrid(int seed)
	{
		_hashGrid = new PseudoRandom[HashGridSize * HashGridSize];
		var currentState = Random.state;
		Random.InitState(seed);
		for (var i = 0; i < _hashGrid.Length; i++)
		{
			_hashGrid[i] = PseudoRandom.Create();
		}

		Random.state = currentState;
	}

	public static PseudoRandom SampleHashGrid(Vector3 position)
	{
		var x = (int) (position.x * HashGridScale) % HashGridSize;
		if (x < 0)
		{
			x += HashGridSize;
		}
		var z = (int) (position.z * HashGridScale) % HashGridSize;
		if (z < 0)
		{
			z += HashGridSize;
		}
		return _hashGrid[x + z * HashGridSize];
	}

	public static float[] GetHouseThresholds(int urbanizationLevel)
	{
		return HouseThesholds[urbanizationLevel - 1];
	}

	public static Vector3 WallThicknessOffset(Vector3 closeEdge, Vector3 farEdge)
	{
		Vector3 offset;
		offset.x = farEdge.x - closeEdge.x;
		offset.y = 0;
		offset.z = farEdge.z - closeEdge.z;
		return offset.normalized * (WallThickness * 0.5f);
	}

	public static Vector3 WallLerp(Vector3 closeVertex, Vector3 farVertex)
	{
		var middle = Vector3.Lerp(closeVertex, farVertex, 0.5f);
		var factor = closeVertex.y < farVertex.y ? WallElevationOffset : 1f - WallElevationOffset;
		middle.y = Mathf.Lerp(closeVertex.y, farVertex.y, factor) + WallYOffset;
		return middle;
	}
}
