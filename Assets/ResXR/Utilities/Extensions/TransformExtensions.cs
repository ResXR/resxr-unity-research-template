using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public static class TransformExtensions
{
	public static async UniTask LerpObjectToAnother(this Transform objectToLerp, Transform objectToLerpTo, float lerpDuration, bool shouldParent = false, CancellationToken cancellationToken = default)
	{
		float passedTime = 0;
		Vector3 objectStartingPosition = objectToLerp.position;

		if (shouldParent)
		{
			objectToLerp.transform.parent = objectToLerpTo.transform;
		}

		while (passedTime < lerpDuration)
		{
			if (cancellationToken.IsCancellationRequested || objectToLerpTo == null || objectToLerp == null)
			{
				return;
			}

			float t = SmootherStep(passedTime / lerpDuration);

			Vector3 newPosition = Vector3.Lerp(objectStartingPosition, objectToLerpTo.position, t);
			objectToLerp.SetPositionOrLocalPosition(newPosition, localPosition: false);

			passedTime += Time.deltaTime;
			await UniTask.Yield();
		}

		objectToLerp.position = objectToLerpTo.position;
	}

	public static void SetPositionOrLocalPosition(this Transform objectToTransform, Vector3 targetPosition, bool localPosition)
	{
		if (localPosition)
		{
			objectToTransform.localPosition = targetPosition;
		}
		else
		{
			objectToTransform.position = targetPosition;
		}
	}

	public static async UniTask LerpObjectToPosition(this Transform objectToLerp, Vector3 targetPosition, float lerpDuration, bool localPosition = true, CancellationToken cancellationToken = default)
	{
		float passedTime = 0;
		Vector3 objectStartingPosition = localPosition ? objectToLerp.localPosition : objectToLerp.position;

		while (passedTime < lerpDuration)
		{
			if (cancellationToken.IsCancellationRequested || objectToLerp == null)
			{
				return;
			}

			float t = SmootherStep(passedTime / lerpDuration);

			Vector3 newPosition = Vector3.Lerp(objectStartingPosition, targetPosition, t);
			objectToLerp.SetPositionOrLocalPosition(newPosition, localPosition);

			passedTime += Time.deltaTime;
			await UniTask.Yield();
		}

		objectToLerp.SetPositionOrLocalPosition(targetPosition, localPosition);
	}

	public static async UniTask LerpObjectToRotation(this Transform objectToLerp, Vector3 targetRotation, float lerpDuration, bool localRotation = true, CancellationToken cancellationToken = default)
	{
		float passedTime = 0;
		Vector3 objectStartingRotation = localRotation ? objectToLerp.localEulerAngles : objectToLerp.eulerAngles;

		while (passedTime < lerpDuration)
		{
			if (cancellationToken.IsCancellationRequested || objectToLerp == null)
			{
				return;
			}

			float t = SmootherStep(passedTime / lerpDuration);

			Vector3 newRotation = Vector3.Lerp(objectStartingRotation, targetRotation, t);
			objectToLerp.SetRotationOrLocalRotation(newRotation, localRotation);

			passedTime += Time.deltaTime;
			await UniTask.Yield();
		}

		objectToLerp.SetRotationOrLocalRotation(targetRotation, localRotation);
	}

	public static void SetRotationOrLocalRotation(this Transform objectToTransform, Vector3 targetRotation, bool localRotation)
	{
		if (localRotation)
		{
			objectToTransform.localEulerAngles = targetRotation;
		}
		else
		{
			objectToTransform.eulerAngles = targetRotation;
		}
	}

	private static float SmootherStep(float t)
	{
		return t * t * t * (t * (6f * t - 15f) + 10f);
	}
}
