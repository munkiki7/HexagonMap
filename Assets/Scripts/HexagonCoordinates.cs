using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct HexagonCoordinates
{
	[SerializeField]
	public int x, y, z;

	public HexagonCoordinates (int x, int z) 
	{
		this.x = x;
		this.z = z;
		y = - x - z;
	}
	
	public static HexagonCoordinates FromOffsetCoordinates (int x, int z) 
	{
		return new HexagonCoordinates(x - z/2, z);
	}
	
	public override string ToString () 
	{
		return "(" + x + ", " + y + ", " + z + ")";
	}

	public string ToStringOnSeparateLines () 
	{
		return x + "\n" + y + "\n" + z;
	}

	public static HexagonCoordinates FromPosition(Vector3 position)
	{
		var x = position.x / (Metrics.InnerRadius * 2f);
		var y = -x;
		var offset = position.z / (Metrics.OuterRadius * 3f);
		x -= offset;
		y -= offset;
		int iX = Mathf.RoundToInt(x);
		int iY = Mathf.RoundToInt(y);
		int iZ = Mathf.RoundToInt(-x -y);
		
		if (iX + iY + iZ != 0) 
		{
			var dX = Mathf.Abs(x - iX);
			var dY = Mathf.Abs(y - iY);
			var dZ = Mathf.Abs(-x -y - iZ);

			if (dX > dY && dX > dZ) 
			{
				iX = -iY - iZ;
			}
			else if (dZ > dY) 
			{
				iZ = -iX - iY;
			}
		}

		return new HexagonCoordinates(iX, iZ);
	}
}
