﻿using UnityEngine;
using UnityEngine.Events;
public class Toggle : CustomInteractable
{
	public UnityEvent SwithOn, SwithOff; 
	public float angle,distance; //angle to hand, hand distance ( temporaty not using )
	public Vector2 Switch; //limits
	public bool onOrOff; //switched on/off
	public Transform MoveObject; //moving part

	private void Start()
    {
		distance = grabPoints [0].transform.localPosition.magnitude;
		if (onOrOff) {
            MoveObject.localEulerAngles = new Vector3 (Switch.x, 0);
		} else {
            MoveObject.localEulerAngles = new Vector3 (Switch.y, 0);
		}
    }

	public void GrabStart(CustomHand hand){
		SetInteractableVariable(hand);
		hand.SkeletonUpdate();
		grab.Invoke ();
	}


	public void GrabUpdate(CustomHand hand){
		angle = -Vector2.SignedAngle (new Vector2(transform.InverseTransformPoint(hand.pivotPoser.position).y, transform.InverseTransformPoint(hand.pivotPoser.position).z),Vector2.up);
        MoveObject.localEulerAngles = new Vector3 (Mathf.Clamp(angle,Switch.x,Switch.y), 0);
        //hand position, if you need them not rotating
        //GetMyGrabPoserTransform (hand).position = RotationObject.position+ RotationObject.forward * distance; 
    }

    public void GrabEnd(CustomHand hand){
        onOrOff = angle < 0;
        if (onOrOff)
            SwithOn.Invoke();
        else
            SwithOff.Invoke();
        MoveObject.localEulerAngles = new Vector3(angle<0?Switch.x:Switch.y, 0);
        DetachHand (hand);
		releaseHand.Invoke ();
	}
}
