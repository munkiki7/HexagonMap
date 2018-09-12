using UnityEngine;

public struct EdgeVertices
{
	public Vector3 vertex1, vertex2, vertex3, vertex4, vertex5;

	public EdgeVertices(Vector3 corner1, Vector3 corner2)
	{
		vertex1 = corner1;
		vertex2 = Vector3.Lerp(corner1, corner2, 1f / 4f);
		vertex3 = Vector3.Lerp(corner1, corner2, 2f / 4f);
		vertex4 = Vector3.Lerp(corner1, corner2, 3f / 4f);
		vertex5 = corner2;
	}
	
	public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
	{
		vertex1 = corner1;
		vertex2 = Vector3.Lerp(corner1, corner2, outerStep);
		vertex3 = Vector3.Lerp(corner1, corner2, 0.5f);
		vertex4 = Vector3.Lerp(corner1, corner2, 1f - outerStep);
		vertex5 = corner2;
	}

	public static EdgeVertices TerraceLerp(EdgeVertices edges1, EdgeVertices edges2, int step)
	{
		EdgeVertices result;
		result.vertex1 = Metrics.TerraceLerp(edges1.vertex1, edges2.vertex1, step);
		result.vertex2 = Metrics.TerraceLerp(edges1.vertex2, edges2.vertex2, step);
		result.vertex3 = Metrics.TerraceLerp(edges1.vertex3, edges2.vertex3, step);
		result.vertex4 = Metrics.TerraceLerp(edges1.vertex4, edges2.vertex4, step);
		result.vertex5 = Metrics.TerraceLerp(edges1.vertex5, edges2.vertex5, step);
		return result;
	}
}
