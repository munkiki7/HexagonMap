using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
	const float MinZoom = -250f;
	const float MaxZoom = -45f;
	const float MinZoomAngle = 90f;
	const float MaxZoomAngle = 45f;
	const float MinZoomMoveSpeed = 300f;
	const float MaxZoomMoveSpeed = 100f;
	const float RotationSpeed = 180f;
	
	public Transform swiwel, stick;
	public Field field;

	float _zoom = 1f;
	float _rotationAngle = 0f;

	void Update()
	{
		var zoomDelta = Input.GetAxis("Mouse ScrollWheel");
		if (Math.Abs(zoomDelta) > 0.001f)
		{
			AdjustZoom(zoomDelta);
		}

		var xDelta = Input.GetAxis("Horizontal");
		var zDelta = Input.GetAxis("Vertical");
		if (Math.Abs(xDelta) > 0.0001f || Math.Abs(zDelta) > 0.0001f)
		{
			AdjustPosition(xDelta, zDelta);
		}

		var rotationDelta = Input.GetAxis("Rotation");
		if (Math.Abs(rotationDelta) > 0.001f)
		{
			AdjustRotation(rotationDelta);
		}
	}

	void AdjustZoom(float delta)
	{
		_zoom = Mathf.Clamp01(_zoom + delta);

		var distance = Mathf.Lerp(MinZoom, MaxZoom, _zoom);
		stick.localPosition = new Vector3(0, 0, distance);

		var angle = Mathf.Lerp(MinZoomAngle, MaxZoomAngle, _zoom);
		swiwel.localRotation = Quaternion.Euler(angle, 0, 0);
	}

	void AdjustPosition(float xDelta, float zDelta)
	{
		var position = transform.localPosition;
		var distance = Time.deltaTime * Mathf.Lerp(MinZoomMoveSpeed, MaxZoomMoveSpeed, _zoom);
		var direction = transform.localRotation * new Vector3(xDelta, 0, zDelta).normalized;

		var damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
		
		position += direction * distance * damping;
		transform.localPosition = ClampPosition(position);
	}

	void AdjustRotation(float delta)
	{
		_rotationAngle += delta * RotationSpeed * Time.deltaTime;
		if (_rotationAngle >= 360f)
		{
			_rotationAngle -= 360f;
		}
		if (_rotationAngle < 0)
		{
			_rotationAngle += 360f;
		}
		transform.localRotation = Quaternion.Euler(0, _rotationAngle, 0);
	}

	Vector3 ClampPosition(Vector3 position)
	{
		var xMax = (field.chunkCountX * Metrics.ChunkSizeX - 0.5f) * 2f * Metrics.InnerRadius;
		var zMax = ((field.chunkCountZ + 0.5f) * Metrics.ChunkSizeZ - 1f) * 1.5f * Metrics.OuterRadius;

		position.x = Mathf.Clamp(position.x, 0f, xMax);
		position.z = Mathf.Clamp(position.z, 0f, zMax);
		
		return position;
	}
}
