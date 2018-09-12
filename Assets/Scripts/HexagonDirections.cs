public enum Direction
{
	NorthEast, East, SouthEast, SouthWest, West, NorthWest
}

public static class HexagonDirectionsExtensions
{
	public static Direction Opposite(this Direction direction)
	{
		if ((int) direction < 3)
		{
			return direction + 3;
		}
		return direction - 3;
	}

	public static Direction Previous(this Direction direction)
	{
		if (direction == Direction.NorthEast)
		{
			return Direction.NorthWest;
		}

		return direction - 1;
	}
	
	public static Direction Next(this Direction direction)
	{
		if (direction == Direction.NorthWest)
		{
			return Direction.NorthEast;
		}

		return direction + 1;
	}

	public static Direction PrePrevious(this Direction direction)
	{
		direction -= 2;
		if (direction >= Direction.NorthEast)
		{
			return direction;
		}
		return direction + 6;
	}
	
	public static Direction AfterNext(this Direction direction)
	{
		direction += 2;
		if (direction <= Direction.NorthWest)
		{
			return direction;
		}
		return direction - 6;
	}
}