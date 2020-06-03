using System;
using System.Collections.Generic;

using UnityEngine;

using Valve.VR;

[RequireComponent(typeof(Rigidbody))]
public class PhysicalObject : CustomInteractable 
{
	#region Variables

	[SerializeField] protected bool twoHandTypeOnlyBackHandRotation; //rotate only right hand
	[SerializeField] protected bool twoHandTypeMiddleRotation; //mean rotation for 2 hands
	[SerializeField] protected List<SteamVR_Skeleton_Poser> handleObject;//count=2
	public Rigidbody MyRigidbody; 
	[SerializeField] protected bool GizmoVisible; //display line of hand swing on long grip
	[SerializeField] protected Vector2 clampHandlePosZ; // grip limit
	
	[Range(0,1)]
	[SerializeField] private float SqueezeCheack; // squeeze death zone difference

	private Vector3 _localDirectionWithPivotLeft, _localDirectionWithPivotRight;
	private bool _isLeftForward;

	private Vector3 _leftHandlePos, _rightHandlePos;
	private Quaternion _leftHandleRot, _rightHandleRot;
	
	#endregion

	#region Methods

	[Serializable]
	public struct SaveVariables
	{
		public float maxAngelarVelocity, mass, drag, angularDrag;
		public Vector3 centerOfMass;
		public bool isKinematic,useGravity;
		public void SaveProperty(Rigidbody rigidbody){
			useGravity = rigidbody.useGravity;
			isKinematic = rigidbody.isKinematic;
			maxAngelarVelocity = rigidbody.maxAngularVelocity;
			centerOfMass = rigidbody.centerOfMass;
			mass = rigidbody.mass;
			drag = rigidbody.drag;
			angularDrag = rigidbody.angularDrag;
		}

		public void LoadProperty(Rigidbody rigidbody){
			rigidbody.useGravity = useGravity;
			rigidbody.isKinematic = isKinematic;
			rigidbody.maxAngularVelocity = maxAngelarVelocity;
			rigidbody.centerOfMass = centerOfMass;
			rigidbody.mass = mass;
			rigidbody.drag = drag;
			rigidbody.angularDrag = angularDrag;
		}
	}
	public SaveVariables saveVariables;

	private void Start () {
		if (GetComponent<Rigidbody> ()) {
			MyRigidbody = GetComponent<Rigidbody> ();
			saveVariables.SaveProperty (MyRigidbody);
		} 
		enabled = false;
	}

	public void GrabStart(CustomHand hand)
	{
		Vector3 tempPosHandLocal=transform.InverseTransformPoint (hand.GrabPoint);
		tempPosHandLocal.x = 0;
		tempPosHandLocal.y = 0;
		MyRigidbody.useGravity = false;
		MyRigidbody.isKinematic = false;
		MyRigidbody.maxAngularVelocity = float.MaxValue;
		if (tempPosHandLocal.z > clampHandlePosZ.x && tempPosHandLocal.z < clampHandlePosZ.y) {
			if (hand.handType == SteamVR_Input_Sources.LeftHand) {
				SetInteractableVariable (hand, handleObject [0]);
				handleObject [0].transform.localPosition = tempPosHandLocal;
			} else {
				if (hand.handType == SteamVR_Input_Sources.RightHand) {
					SetInteractableVariable (hand, handleObject [1]);
					handleObject [1].transform.localPosition = tempPosHandLocal;
				}
			}
		} else {
			SetInteractableVariable (hand);
		}

		if (leftHand && rightHand) {
			_isLeftForward = transform.InverseTransformPoint (leftMyGrabPoser.transform.position).z > transform.InverseTransformPoint (rightMyGrabPoser.transform.position).z;
			_localDirectionWithPivotLeft = leftMyGrabPoser.transform.InverseTransformDirection (transform.up);
			_localDirectionWithPivotRight = rightMyGrabPoser.transform.InverseTransformDirection (transform.up);
		}
		if (pickReleaseOnce){
			if (!leftHand||!rightHand){
				Grab.Invoke ();//sound
			}
		}else{
			Grab.Invoke ();
		}
	}

	public void GrabUpdate(CustomHand hand)
	{

		if (rightHand && leftHand) {
			_isLeftForward = transform.InverseTransformPoint (leftMyGrabPoser.transform.position).z > transform.InverseTransformPoint (rightMyGrabPoser.transform.position).z;
            bool leftIsForvardTemp = _isLeftForward;

            if (handleObject != null && handleObject.Count == 2) {
                if ((leftHand.squeeze - rightHand.squeeze) > SqueezeCheack && rightMyGrabPoser == handleObject[1])
                {
                    handleObject[1].transform.localPosition = new Vector3(0, 0, Mathf.Clamp(transform.InverseTransformPoint(rightHand.GrabPoint).z, clampHandlePosZ.x, clampHandlePosZ.y));
                    if (_isLeftForward)
                        _isLeftForward = !_isLeftForward;
                }
                if ((rightHand.squeeze - leftHand.squeeze) > SqueezeCheack && leftMyGrabPoser == handleObject[0])
                {
                    handleObject[0].transform.localPosition = new Vector3(0, 0, Mathf.Clamp(transform.InverseTransformPoint(leftHand.GrabPoint).z, clampHandlePosZ.x, clampHandlePosZ.y));
                    if (!_isLeftForward)
                        _isLeftForward = !_isLeftForward;
                }
			}
			if (useSecondPose && UseSecondPose) 
			{
				if (secondPoses.Contains (leftHand.grabPoser)) {
                    MyRigidbody.centerOfMass = transform.InverseTransformPoint (GetMyGrabPoserTransform (rightHand).position);
					MyRigidbody.velocity = (rightHand.pivotPoser.position - GetMyGrabPoserTransform (rightHand).position) / Time.fixedDeltaTime* hand.GetBlendPose();
					MyRigidbody.angularVelocity = GetAngularVelocities (rightHand.pivotPoser.rotation, GetMyGrabPoserTransform (rightHand).rotation, hand.GetBlendPose());
				} else {
					MyRigidbody.centerOfMass = transform.InverseTransformPoint (GetMyGrabPoserTransform (leftHand).position);
					MyRigidbody.velocity = (leftHand.pivotPoser.position - GetMyGrabPoserTransform (leftHand).position) / Time.fixedDeltaTime* hand.GetBlendPose();
					MyRigidbody.angularVelocity = GetAngularVelocities (leftHand.pivotPoser.rotation, GetMyGrabPoserTransform (leftHand).rotation, hand.GetBlendPose());
				}
			} else {
                if (!twoHandTypeOnlyBackHandRotation)
                    leftIsForvardTemp = _isLeftForward;
                if (twoHandTypeMiddleRotation)
                {
                    if (leftIsForvardTemp)
                    {
                        rightHand.toolTransform.rotation = Quaternion.LookRotation(leftMyGrabPoser.transform.position - rightMyGrabPoser.transform.position, rightMyGrabPoser.transform.TransformDirection(_localDirectionWithPivotRight) + leftMyGrabPoser.transform.TransformDirection(_localDirectionWithPivotLeft));
                        leftHand.toolTransform.rotation = Quaternion.LookRotation(leftHand.pivotPoser.transform.position - rightHand.pivotPoser.transform.position, rightHand.pivotPoser.TransformDirection(_localDirectionWithPivotRight) + leftHand.pivotPoser.TransformDirection(_localDirectionWithPivotLeft));
                    }
                    else
                    {
                        rightHand.toolTransform.rotation = Quaternion.LookRotation(rightMyGrabPoser.transform.position - leftMyGrabPoser.transform.position, leftMyGrabPoser.transform.TransformDirection(_localDirectionWithPivotLeft) + rightMyGrabPoser.transform.TransformDirection(_localDirectionWithPivotRight));
                        leftHand.toolTransform.rotation = Quaternion.LookRotation(rightHand.pivotPoser.transform.position - leftHand.pivotPoser.transform.position, leftHand.pivotPoser.TransformDirection(_localDirectionWithPivotLeft) + rightHand.pivotPoser.TransformDirection(_localDirectionWithPivotRight));
                    }
                }
                else
                {
                    if (leftIsForvardTemp)
                    {
                        rightHand.toolTransform.rotation = Quaternion.LookRotation(leftMyGrabPoser.transform.position - rightMyGrabPoser.transform.position, rightMyGrabPoser.transform.TransformDirection(_localDirectionWithPivotRight));
                        leftHand.toolTransform.rotation = Quaternion.LookRotation(leftHand.pivotPoser.transform.position - rightHand.pivotPoser.transform.position, rightHand.pivotPoser.TransformDirection(_localDirectionWithPivotRight));
                    }
                    else
                    {
                        rightHand.toolTransform.rotation = Quaternion.LookRotation(rightMyGrabPoser.transform.position - leftMyGrabPoser.transform.position, leftMyGrabPoser.transform.TransformDirection(_localDirectionWithPivotLeft));
                        leftHand.toolTransform.rotation = Quaternion.LookRotation(rightHand.pivotPoser.transform.position - leftHand.pivotPoser.transform.position, leftHand.pivotPoser.TransformDirection(_localDirectionWithPivotLeft));
                    }
                }

                if (_isLeftForward)
                {
                    
                    MyRigidbody.centerOfMass = transform.InverseTransformPoint(rightMyGrabPoser.transform.position);
                    MyRigidbody.velocity = (rightHand.pivotPoser.position - rightMyGrabPoser.transform.position) / Time.fixedDeltaTime* rightHand.GetBlendPose();
                    MyRigidbody.angularVelocity = GetAngularVelocities(leftHand.toolTransform.rotation, rightHand.toolTransform.rotation, rightHand.GetBlendPose());
                }
                else
                {
                    
                    MyRigidbody.centerOfMass = transform.InverseTransformPoint(leftMyGrabPoser.transform.position);
                    MyRigidbody.velocity = (leftHand.pivotPoser.position - leftMyGrabPoser.transform.position) / Time.fixedDeltaTime* leftHand.GetBlendPose();
                    MyRigidbody.angularVelocity = GetAngularVelocities(leftHand.toolTransform.rotation, rightHand.toolTransform.rotation, leftHand.GetBlendPose());
                }
			}
		} else {//one hand
            MyRigidbody.centerOfMass = transform.InverseTransformPoint(GetMyGrabPoserTransform(hand).position);
			MyRigidbody.velocity = (hand.pivotPoser.position - GetMyGrabPoserTransform(hand).position)/Time.fixedDeltaTime* hand.GetBlendPose();
			MyRigidbody.angularVelocity = GetAngularVelocities (hand.pivotPoser.rotation, GetMyGrabPoserTransform(hand).rotation, hand.GetBlendPose());
		}	
	}

	public void GrabEnd(CustomHand hand){
		DetachHand (hand);
		if (!leftHand && !rightHand) {
			saveVariables.LoadProperty (MyRigidbody);
		}
		if (pickReleaseOnce){
			if (!rightHand&&!leftHand){
				ReleaseHand.Invoke ();//sound
			}
		}else{
			ReleaseHand.Invoke ();
		}
        if (leftHand)
        {
            leftHand.SetBlendPose(1);
            leftHand.SetEndFramePos();
        }
        if (rightHand)
        {
            rightHand.SetBlendPose(1);
            rightHand.SetEndFramePos();
        }
    }

	public void Initialize(){
		if (GetComponent<Rigidbody> ()) {
			MyRigidbody = GetComponent<Rigidbody> ();
			saveVariables.SaveProperty (MyRigidbody);
		}
	}
	
	public void GrabStartCustom(CustomHand hand){
		Vector3 tempPosHandLocal=transform.InverseTransformPoint (hand.GrabPoint);
		tempPosHandLocal.x = 0;
		tempPosHandLocal.y = 0;
		MyRigidbody.useGravity = false;
		MyRigidbody.isKinematic = false;
		MyRigidbody.maxAngularVelocity = float.MaxValue;

        if (tempPosHandLocal.z > clampHandlePosZ.x && tempPosHandLocal.z < clampHandlePosZ.y)
		{
			switch(hand.handType)
			{
				case SteamVR_Input_Sources.LeftHand:
					SetInteractableVariable (hand, handleObject [0]);
					handleObject [0].transform.localPosition = tempPosHandLocal;
					break;
				case SteamVR_Input_Sources.RightHand:
					SetInteractableVariable (hand, handleObject [1]);
					handleObject [1].transform.localPosition = tempPosHandLocal;
					break;
			}
		} 
		else 
		{
			SetInteractableVariable (hand);
		}

		if (leftHand && rightHand) 
		{
			_isLeftForward = transform.InverseTransformPoint (leftMyGrabPoser.transform.position).z > transform.InverseTransformPoint (rightMyGrabPoser.transform.position).z;
			_localDirectionWithPivotLeft = leftMyGrabPoser.transform.InverseTransformDirection (transform.up);
			_localDirectionWithPivotRight = rightMyGrabPoser.transform.InverseTransformDirection (transform.up);
		}
		if (pickReleaseOnce)
		{
			if (!leftHand||!rightHand)
			{
				Grab.Invoke ();//sound
			}
		}else{
			Grab.Invoke ();
		}
        
	}

	public void GrabUpdateCustom(CustomHand hand){
        GrabUpdate(hand);
	}

	public void GrabEndCustom(CustomHand hand){
        GrabEnd(hand);
	}

	public static Vector3 GetAngularVelocities(Quaternion hand,Quaternion fake,float blend)
	{
		Quaternion rotationDelta = hand * Quaternion.Inverse(fake);
		Vector3 angularTarget=Vector3.zero;

		float angle;
		Vector3 axis;
		rotationDelta.ToAngleAxis(out angle, out axis);

		if (angle > 180)
			angle -= 360;

		if (angle != 0 && float.IsNaN(axis.x) == false && float.IsInfinity(axis.x) == false)
		{
			angularTarget = angle * axis *0.95f*blend;
		}
		else
			angularTarget = Vector3.zero;
		return angularTarget;
	}

	private void OnDrawGizmosSelected(){
		if (GizmoVisible) {
			Gizmos.color=Color.red;
			Gizmos.DrawLine(transform.TransformPoint(new Vector3 (0, 0, clampHandlePosZ.x)),transform.TransformPoint(new Vector3 (0, 0, clampHandlePosZ.y)));
		}
	}
	
	#endregion

}
