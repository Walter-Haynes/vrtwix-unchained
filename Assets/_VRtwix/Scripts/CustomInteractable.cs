using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using JetBrains.Annotations;

using Valve.VR;

public abstract class CustomInteractable : MonoBehaviour 
{
	#region Variables
	
	public bool isInteractable = true;

    public List<SteamVR_Skeleton_Poser> grabPoints,secondPoses; //not influencing on rotation posers
    public CustomHand leftHand, rightHand; //hand which currently holding an object
    public SteamVR_Skeleton_Poser leftMyGrabPoser, rightMyGrabPoser; //current holding posers
    public bool twoHanded, useSecondPose, hideController;//two handed interaction, use posers which influence rotation, hide controllers
	public CustomHand.GrabType grabType=CustomHand.GrabType.Grip;//how object should be grabbed

	[Header("SoundEvents")]
	public bool pickReleaseOnce; //sound if all hands are released or picked both hands
	
    public UnityEvent Grab;
	public UnityEvent ReleaseHand;
	
	#endregion

	#region Methods

	public Transform GetMyGrabPoserTransform() {
        if (leftMyGrabPoser)
            return leftMyGrabPoser.transform;
        if (rightMyGrabPoser)
            return rightMyGrabPoser.transform;
        return null;
    }

	protected Transform GetMyGrabPoserTransform(in CustomHand hand) 
    {
        switch(hand.handType)
		{
			case SteamVR_Input_Sources.LeftHand when leftMyGrabPoser:
				return leftMyGrabPoser.transform;
			case SteamVR_Input_Sources.RightHand when rightMyGrabPoser:
				return rightMyGrabPoser.transform;
			default:
				return null;
		}
	}

	protected SteamVR_Skeleton_Poser GetMyGrabPoser(in CustomHand hand) 
	{
		switch(hand.handType)
		{
			case SteamVR_Input_Sources.LeftHand when leftMyGrabPoser:
				return leftMyGrabPoser;
			case SteamVR_Input_Sources.RightHand when rightMyGrabPoser:
				return rightMyGrabPoser;
			default:
				return null;
		}
	}

    public Transform CloseObject(in Vector3 tempPoint) 
	{
        Transform __closestObject = null;
		if(grabPoints == null) return null;
		
		float MinDistance = float.MaxValue;
		foreach(SteamVR_Skeleton_Poser __poser in grabPoints)
		{
			if(!(Vector3.Distance(tempPoint, __poser.transform.position) < MinDistance)) continue;
				
			MinDistance = Vector3.Distance(tempPoint, __poser.transform.position);
			__closestObject = __poser.transform;
		}

		if(!useSecondPose) return __closestObject;
			
		foreach(SteamVR_Skeleton_Poser __t in secondPoses)
		{
			if(!(Vector3.Distance(tempPoint, __t.transform.position) < MinDistance)) continue;
				
			MinDistance = Vector3.Distance(tempPoint, __t.transform.position);
			__closestObject = __t.transform;
		}
		return __closestObject;
    }

	private SteamVR_Skeleton_Poser ClosePoser(in Vector3 tempPoint) 
	{
        SteamVR_Skeleton_Poser TempClose = null;
		if(grabPoints == null) return null;
		
		float __minDistance = float.MaxValue;
		foreach(SteamVR_Skeleton_Poser __poser in grabPoints)
		{
			if(__poser == leftMyGrabPoser || __poser == rightMyGrabPoser) continue;
			if(!(Vector3.Distance(tempPoint, __poser.transform.position) < __minDistance)) continue;
				
			__minDistance = Vector3.Distance(tempPoint, __poser.transform.position);
			TempClose = __poser;
		}

		if(!useSecondPose || !IfOtherHandUseMainPoseOnThisObject) return TempClose;
			
		foreach(SteamVR_Skeleton_Poser __poser in secondPoses)
		{
			if(__poser == leftMyGrabPoser || __poser == rightMyGrabPoser) continue;
			if(!(Vector3.Distance(tempPoint, __poser.transform.position) < __minDistance)) continue;
				
			__minDistance = Vector3.Distance (tempPoint, __poser.transform.position);
			TempClose = __poser;
		}
		return TempClose;
    }

	protected void SetInteractableVariable(in CustomHand hand)
	{
		switch(hand.handType)
		{
			case SteamVR_Input_Sources.LeftHand:
			{
				if(leftHand)
				{
					DetachHand(leftHand);
				}
				if(!twoHanded && rightHand)
				{
					DetachHand(rightHand);
				}
				leftMyGrabPoser = ClosePoser(hand.GrabPoint); //?? TODO -Walter- What??
				if (leftMyGrabPoser) 
				{
					hand.grabPoser = leftMyGrabPoser;
					leftHand = hand;
					leftHand.SkeletonUpdate();
				}
				//haptic
				break;
			}
			case SteamVR_Input_Sources.RightHand:
			{
				if(rightHand)
				{
					DetachHand(rightHand);
				}
				if(!twoHanded && leftHand)
				{
					DetachHand(leftHand);
				}
				
				rightMyGrabPoser = ClosePoser(hand.GrabPoint);
				if (rightMyGrabPoser) 
				{
					hand.grabPoser = rightMyGrabPoser;
					rightHand = hand;
					rightHand.SkeletonUpdate();
				}
				//haptic
				break;
			}
		}
	}

	protected void SetInteractableVariable(in CustomHand hand, in SteamVR_Skeleton_Poser poser)
	{
		switch(hand.handType)
		{
			case SteamVR_Input_Sources.LeftHand:
			{
				if (leftHand)
					DetachHand (leftHand);
				if (!twoHanded && rightHand)
					DetachHand (rightHand);
				leftMyGrabPoser = poser;
				
				if (leftMyGrabPoser) 
				{
					hand.grabPoser = leftMyGrabPoser;
					leftHand = hand;
					leftHand.SkeletonUpdate ();
				}
				//haptic
				break;
			}
			case SteamVR_Input_Sources.RightHand:
			{
				if (rightHand)
					DetachHand (rightHand);
				if (!twoHanded && leftHand)
					DetachHand (leftHand);
			
				rightMyGrabPoser = poser;

				if(!rightMyGrabPoser) return;
			
				hand.grabPoser = rightMyGrabPoser;
				rightHand = hand;
				rightHand.SkeletonUpdate ();
				//haptic
				break;
			}
		}
	}

	private bool IfOtherHandUseMainPoseOnThisObject
	{
		get
		{
			if(rightHand) 
			{
				if (grabPoints.Contains(rightHand.grabPoser)) 
				{
					return true;
				}
			}
			else if(leftHand) 
			{
				if (grabPoints.Contains(leftHand.grabPoser)) 
				{
					return true;
				}
			}

			return false;
		}
	}

	protected bool UseSecondPose
	{
		get
		{
			bool __tempBool = false;
			if(!leftHand || !rightHand) return false;
			
			if (secondPoses.Contains(leftHand.grabPoser) || secondPoses.Contains(rightHand.grabPoser)) 
			{
				__tempBool = true;
			}
			
			return __tempBool;
		}
	}

    public bool CanSelected(in CustomHand hand)
	{
		if (!leftHand && !rightHand)
        {
            return true;
        }

		return (leftHand && leftHand == hand) || (rightHand && rightHand == hand);
	}


	protected void DetachHand(in CustomHand hand)
	{
		hand.DetachHand();
		
		switch(hand.handType)
		{
			case SteamVR_Input_Sources.LeftHand:
				leftMyGrabPoser = null;
				leftHand = null;
				break;
			case SteamVR_Input_Sources.RightHand:
				rightMyGrabPoser = null;
				rightHand = null;
				break;
		}
	}

	[PublicAPI]
	public void DetachHands()
	{
		if (leftHand) 
		{
			leftHand.DetachHand();
			leftMyGrabPoser = null;
			leftHand = null;
		}
		else if (rightHand) 
		{
			rightHand.DetachHand();
			rightMyGrabPoser = null;
			rightHand = null;
		}
	}

	#endregion

}
