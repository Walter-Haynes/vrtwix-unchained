﻿using UnityEngine;

public class Tuner : CustomInteractable
{
    public Transform RotationObject; //moving object
	public float angle; //angle 
	public Vector2 clamp; //rotation limit, 0 - no limits
	private Vector3 oldDir; //old hands rotation

	public void GrabStart(CustomHand hand)
    {
        SetInteractableVariable(hand);
        hand.SkeletonUpdate();
        GetMyGrabPoserTransform(hand).rotation = Quaternion.LookRotation(transform.forward, hand.pivotPoser.up);
		oldDir = transform.InverseTransformDirection(hand.pivotPoser.up);
		GetMyGrabPoserTransform (hand).transform.position = hand.pivotPoser.position;
		grab.Invoke ();
    }

	public void GrabUpdate(CustomHand hand)
    {
		
		angle+= Vector3.SignedAngle(oldDir, transform.InverseTransformDirection(hand.pivotPoser.up), Vector3.forward);
		if (clamp != Vector2.zero)
		angle = Mathf.Clamp (angle, clamp.x, clamp.y);
        RotationObject.localEulerAngles = new Vector3(0, 0, angle);
		GetMyGrabPoserTransform (hand).transform.position = transform.position;// Vector3.MoveTowards (GetMyGrabPoserTransform (hand).transform.position, transform.TransformPoint(Vector3.zero), Time.deltaTime*.5f);
        oldDir = transform.InverseTransformDirection(hand.pivotPoser.up);
    }

	public void GrabEnd(CustomHand hand)
    {
        DetachHand(hand);
		releaseHand.Invoke ();
    }

}
