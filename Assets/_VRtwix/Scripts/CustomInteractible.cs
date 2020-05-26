using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Valve.VR;

public class CustomInteractible : MonoBehaviour 
{
	public bool isInteractible = true;

    public List<SteamVR_Skeleton_Poser> grabPoints,secondPoses; //not influenting on rotation posers
    public CustomHand leftHand, rightHand;//hand which currently holding an object
    public SteamVR_Skeleton_Poser leftMyGrabPoser, rightMyGrabPoser;//current holding posers
    public bool TwoHanded, useSecondPose, HideController;//two handed interaction, use posers which influent on rotation, hide controllers
	public CustomHand.GrabType grabType=CustomHand.GrabType.Grip;//how object should be grabbed

	[Header("SoundEvents")]
	public bool pickReleaseOnce; //sound if all hands are released or picked both hands
	
    public UnityEvent Grab;
	public UnityEvent ReleaseHand;
//

    public Transform GetMyGrabPoserTransform() {
        if (leftMyGrabPoser)
            return leftMyGrabPoser.transform;
        if (rightMyGrabPoser)
            return rightMyGrabPoser.transform;
        return null;
    }

    public Transform GetMyGrabPoserTransform(in CustomHand hand) 
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

	public SteamVR_Skeleton_Poser GetMyGrabPoser(in CustomHand hand) 
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


     
    public Transform CloseObject(Vector3 tempPoint) {
        Transform TempClose = null;
        if (grabPoints != null) {
            float MinDistance = float.MaxValue;
            for (int i = 0; i < grabPoints.Count; i++) {
                if (Vector3.Distance(tempPoint, grabPoints[i].transform.position) < MinDistance) {
                    MinDistance = Vector3.Distance(tempPoint, grabPoints[i].transform.position);
                    TempClose = grabPoints[i].transform;
                }
            } 
			if (useSecondPose) {
				for (int i = 0; i < secondPoses.Count; i++) {
					if (Vector3.Distance(tempPoint, secondPoses[i].transform.position) < MinDistance) {
						MinDistance = Vector3.Distance(tempPoint, secondPoses[i].transform.position);
						TempClose = secondPoses[i].transform;
					}
				} 
			}
        }
        return TempClose;
    }

    public SteamVR_Skeleton_Poser ClosePoser(in Vector3 tempPoint) 
	{
        SteamVR_Skeleton_Poser TempClose = null;
		if(grabPoints == null) return null;
		
		float __minDistance = float.MaxValue;
		foreach(SteamVR_Skeleton_Poser __t in grabPoints)
		{
			if(__t == leftMyGrabPoser || __t == rightMyGrabPoser) continue;
			if(!(Vector3.Distance(tempPoint, __t.transform.position) < __minDistance)) continue;
				
			__minDistance = Vector3.Distance(tempPoint, __t.transform.position);
			TempClose = __t;
		}

		if(!useSecondPose || !ifOtherHandUseMainPoseOnThisObject()) return TempClose;
			
		foreach(SteamVR_Skeleton_Poser __t in secondPoses)
		{
			if(__t == leftMyGrabPoser || __t == rightMyGrabPoser) continue;
			if(!(Vector3.Distance(tempPoint, __t.transform.position) < __minDistance)) continue;
				
			__minDistance = Vector3.Distance (tempPoint, __t.transform.position);
			TempClose = __t;
		}
		return TempClose;
    }

    public void SetInteractibleVariable(in CustomHand hand) {
        if (hand.handType == SteamVR_Input_Sources.LeftHand) {
            if (leftHand)
                DettachHand(leftHand);
            if (!TwoHanded && rightHand)
                DettachHand(rightHand);
            leftMyGrabPoser = ClosePoser(hand.GrabPoint());
            if (leftMyGrabPoser) {
                hand.grabPoser = leftMyGrabPoser;
                leftHand = hand;
                leftHand.SkeletonUpdate();
            }
            //haptic
        }
        if (hand.handType == SteamVR_Input_Sources.RightHand) {
            if (rightHand)
                DettachHand(rightHand);
            if (!TwoHanded && leftHand)
                DettachHand(leftHand);
            rightMyGrabPoser = ClosePoser(hand.GrabPoint());
            if (rightMyGrabPoser) {
                hand.grabPoser = rightMyGrabPoser;
                rightHand = hand;
                rightHand.SkeletonUpdate();
            }
            //haptic
        }
    }

	public void SetInteractibleVariable(in CustomHand hand, in SteamVR_Skeleton_Poser poser)
	{
		switch(hand.handType)
		{
			case SteamVR_Input_Sources.LeftHand:
			{
				if (leftHand)
					DettachHand (leftHand);
				if (!TwoHanded && rightHand)
					DettachHand (rightHand);
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
					DettachHand (rightHand);
				if (!TwoHanded && leftHand)
					DettachHand (leftHand);
			
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

	public bool ifOtherHandUseMainPoseOnThisObject(){
		bool tempBool=false;
		if (rightHand) {
			if (grabPoints.Contains (rightHand.grabPoser)) {
				tempBool = true;
			}
		}


		if (leftHand) {
			if (grabPoints.Contains (leftHand.grabPoser)) {
				tempBool = true;
			}
		}

		return tempBool;
	}

	public bool ifUseSecondPose(){
		bool tempBool=false;
		if (leftHand && rightHand) {
			if (secondPoses.Contains (leftHand.grabPoser)||secondPoses.Contains(rightHand.grabPoser)) {
				tempBool = true;
			}
		}
		return tempBool;
	}

    public bool CanSelected(in CustomHand hand) {
        if (!leftHand && !rightHand)
        {
            return true;
        } else {
            if ((leftHand && leftHand == hand) || (rightHand && rightHand == hand))
            {
                return true;
            }
            else
                return false;
        }
    }

	public void DettachHand(in CustomHand hand){
		hand.DetachHand ();
		if (hand.handType == SteamVR_Input_Sources.LeftHand) {
			leftMyGrabPoser = null;
			leftHand = null;
		}
		if (hand.handType == SteamVR_Input_Sources.RightHand) {
			rightMyGrabPoser = null;
			rightHand = null;
		}
	}

	public void DettachHands(){
		if (leftHand) {
			leftHand.DetachHand ();
			leftMyGrabPoser = null;
			leftHand = null;
		}
		if (rightHand) {
			rightHand.DetachHand ();
			rightMyGrabPoser = null;
			rightHand = null;
		}
	}

}
