﻿using UnityEngine;
using Valve.VR;

public class SteeringWheel : CustomInteractable {
	public float angle,clamp;//steerwing wheel angle, rotation limit
	private float angleLeft,angleRight; //angle from steering wheel to hands
	private Vector2 oldPosLeft,oldPosRight; //old hands positions
	public Transform RotationObject; //moving object

	public float radius; //wheel radius
	private bool ReversHand; //turn out hands, depending of interaction side 

	private void Start () {
		if (grabPoints!=null&&grabPoints.Count>0)
			radius = grabPoints [0].transform.localPosition.magnitude;
	}
	public void GrabStart(CustomHand hand){
		SetInteractableVariable (hand);
		hand.SkeletonUpdate ();
		hand.PivotUpdate ();
		Transform tempPoser=GetMyGrabPoserTransform (hand);
		Vector3 HandTolocalPos = transform.InverseTransformPoint (hand.pivotPoser.position);
		HandTolocalPos.z = 0;
		tempPoser.localPosition = HandTolocalPos;
		if (hand.handType == SteamVR_Input_Sources.LeftHand) {
			oldPosLeft = new Vector2 (HandTolocalPos.x, HandTolocalPos.y);
		} else {
			if (hand.handType == SteamVR_Input_Sources.RightHand) {
				oldPosRight = new Vector2 (HandTolocalPos.x, HandTolocalPos.y);
			} 
		}
		ReversHand = Vector3.Angle (transform.forward, hand.pivotPoser.forward) < 90;
		grab.Invoke ();
	}

	public void GrabUpdate(CustomHand hand){
		Transform tempPoser = GetMyGrabPoserTransform (hand);
		Vector3 HandTolocalPos = transform.InverseTransformPoint (hand.pivotPoser.position);
		HandTolocalPos.z = 0;
		tempPoser.localPosition = HandTolocalPos;


		if (hand.handType == SteamVR_Input_Sources.LeftHand) {
				angle-=Vector2.SignedAngle (tempPoser.localPosition, oldPosLeft)*(leftHand&&rightHand?leftHand.squeeze==rightHand.squeeze?.5f:hand.squeeze/(Mathf.Epsilon+(leftHand.squeeze+rightHand.squeeze)):1f);
			
			oldPosLeft = new Vector2 (HandTolocalPos.x, HandTolocalPos.y);
		} else {
			if (hand.handType == SteamVR_Input_Sources.RightHand) {
					angle-=Vector2.SignedAngle (tempPoser.localPosition, oldPosRight)*(leftHand&&rightHand?leftHand.squeeze==rightHand.squeeze?.5f:hand.squeeze/(Mathf.Epsilon+(leftHand.squeeze+rightHand.squeeze)):1f);
				
				oldPosRight = new Vector2 (HandTolocalPos.x, HandTolocalPos.y);
			} 
		}
		angle = Mathf.Clamp (angle, -clamp, clamp);
		RotationObject.localEulerAngles=new Vector3 (0, 0, angle);
		tempPoser.localPosition = tempPoser.localPosition.normalized * radius;
		tempPoser.rotation = Quaternion.LookRotation (ReversHand? transform.forward:-transform.forward, tempPoser.position-transform.position);

	}

	public void GrabEnd(CustomHand hand){
		DetachHand (hand);
		releaseHand.Invoke ();
	}
}
