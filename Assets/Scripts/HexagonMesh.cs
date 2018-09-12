using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexagonMesh : MonoBehaviour 
{
	Mesh _hexagonMesh;
	MeshCollider _meshCollider;
	
	[NonSerialized] List<Vector3> _vertices;
	[NonSerialized] List<int> _triangles;
	[NonSerialized] List<Color> _colors;
	[NonSerialized] List<Vector2> _uvCoordinates, _uv2Coordinates;

	public bool isCollidable, useColors, useUvCoordinates, useUv2Coordinates;
	
	void Awake()
	{
		GetComponent<MeshFilter>().mesh = _hexagonMesh = new Mesh();
		if (isCollidable)
		{
			_meshCollider = gameObject.AddComponent<MeshCollider>();
		}
		//_hexagonMesh.name = "Hexagon Mesh"; WHY?
	}

	

	public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
	{
		var vertexIndex = _vertices.Count;
		_vertices.Add(Metrics.Perturb(v1));
		_vertices.Add(Metrics.Perturb(v2));
		_vertices.Add(Metrics.Perturb(v3));
		_triangles.Add(vertexIndex);
		_triangles.Add(vertexIndex + 1);
		_triangles.Add(vertexIndex + 2);
	}
	
	public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
	{
		var vertexIndex = _vertices.Count;
		_vertices.Add(v1);
		_vertices.Add(v2);
		_vertices.Add(v3);
		_triangles.Add(vertexIndex);
		_triangles.Add(vertexIndex + 1);
		_triangles.Add(vertexIndex + 2);
	}
	
	public void AddTriangleColor (Color color1, Color color2, Color color3)
	{
		_colors.Add(color1);
		_colors.Add(color2);
		_colors.Add(color3);
	}

	public void AddTriangleUv(Vector2 uv1, Vector2 uv2, Vector2 uv3)
	{
		_uvCoordinates.Add(uv1);
		_uvCoordinates.Add(uv2);
		_uvCoordinates.Add(uv3);
	}
	
	public void AddTriangleUv2(Vector2 uv1, Vector2 uv2, Vector2 uv3)
	{
		_uv2Coordinates.Add(uv1);
		_uv2Coordinates.Add(uv2);
		_uv2Coordinates.Add(uv3);
	}

	public void AddQuad(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 vertex4)
	{
		var vertexIndex = _vertices.Count;
		_vertices.Add(Metrics.Perturb(vertex1));
		_vertices.Add(Metrics.Perturb(vertex2));
		_vertices.Add(Metrics.Perturb(vertex3));
		_vertices.Add(Metrics.Perturb(vertex4));
		_triangles.Add(vertexIndex);
		_triangles.Add(vertexIndex + 2);
		_triangles.Add(vertexIndex + 1);
		_triangles.Add(vertexIndex + 1);
		_triangles.Add(vertexIndex + 2);
		_triangles.Add(vertexIndex + 3);
	}
	
	public void AddQuadUnperturbed(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 vertex4)
	{
		var vertexIndex = _vertices.Count;
		_vertices.Add(vertex1);
		_vertices.Add(vertex2);
		_vertices.Add(vertex3);
		_vertices.Add(vertex4);
		_triangles.Add(vertexIndex);
		_triangles.Add(vertexIndex + 2);
		_triangles.Add(vertexIndex + 1);
		_triangles.Add(vertexIndex + 1);
		_triangles.Add(vertexIndex + 2);
		_triangles.Add(vertexIndex + 3);
	}
	
	public void AddQuadColor(Color color1, Color color2) 
	{
		_colors.Add(color1);
		_colors.Add(color1);
		_colors.Add(color2);
		_colors.Add(color2);
	}
	
	public void AddQuadColor(Color color1, Color color2, Color color3, Color color4) 
	{
		_colors.Add(color1);
		_colors.Add(color2);
		_colors.Add(color3);
		_colors.Add(color4);
	}
	
	public void AddQuadUv(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
	{
		_uvCoordinates.Add(uv1);
		_uvCoordinates.Add(uv2);
		_uvCoordinates.Add(uv3);
		_uvCoordinates.Add(uv4);
	}

	public void AddQuadUv(float uMin, float uMax, float vMin, float vMax)
	{
		_uvCoordinates.Add(new Vector2(uMin, vMin));
		_uvCoordinates.Add(new Vector2(uMax, vMin));
		_uvCoordinates.Add(new Vector2(uMin, vMax));
		_uvCoordinates.Add(new Vector2(uMax, vMax));
	}
	
	public void AddQuadUv2(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
	{
		_uv2Coordinates.Add(uv1);
		_uv2Coordinates.Add(uv2);
		_uv2Coordinates.Add(uv3);
		_uv2Coordinates.Add(uv4);
	}

	public void AddQuadUv2(float uMin, float uMax, float vMin, float vMax)
	{
		_uv2Coordinates.Add(new Vector2(uMin, vMin));
		_uv2Coordinates.Add(new Vector2(uMax, vMin));
		_uv2Coordinates.Add(new Vector2(uMin, vMax));
		_uv2Coordinates.Add(new Vector2(uMax, vMax));
	}

	public void Clear()
	{
		_hexagonMesh.Clear();
		_vertices = ListPool<Vector3>.Get();
		if (useColors)
		{
			_colors = ListPool<Color>.Get();
		}

		if (useUvCoordinates)
		{
			_uvCoordinates = ListPool<Vector2>.Get();
		}
		
		if (useUv2Coordinates)
		{
			_uv2Coordinates = ListPool<Vector2>.Get();
		}
		
		_triangles = ListPool<int>.Get();
	}

	public void Apply()
	{
		_hexagonMesh.SetVertices(_vertices);
		ListPool<Vector3>.Add(_vertices);
		if (useColors)
		{
			_hexagonMesh.SetColors(_colors);
			ListPool<Color>.Add(_colors);
		}
		if (useUvCoordinates)
		{
			_hexagonMesh.SetUVs(0, _uvCoordinates);
			ListPool<Vector2>.Add(_uvCoordinates);
		}
		if (useUv2Coordinates)
		{
			_hexagonMesh.SetUVs(1, _uv2Coordinates);
			ListPool<Vector2>.Add(_uv2Coordinates);
		}
		
		_hexagonMesh.SetTriangles(_triangles, 0);
		ListPool<int>.Add(_triangles);
		_hexagonMesh.RecalculateNormals();
		if (isCollidable)
		{
			_meshCollider.sharedMesh = _hexagonMesh;
		}
	}
}
