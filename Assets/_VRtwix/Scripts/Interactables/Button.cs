using UnityEngine;
using UnityEngine.Events;
public class Button : CustomInteractable
{
    public float distanseToPress; //button press reach distance
    [Range(.1f,1f)]
    public float DistanceMultiply=.1f; //button sensetivity slowdown
    public Transform MoveObject; //movable button object
    public UnityEvent ButtonDown, ButtonUp, ButtonUpdate; // events

    private float _startButtonPosition; //tech variable, assigned at start of pressed button
    private bool press; //button check, to ButtonDown call 1 time

    private void Awake()
    {
        _startButtonPosition = MoveObject.localPosition.z;
    }


    private void GrabStart(CustomHand hand)
    {
        SetInteractableVariable(hand);
        hand.SkeletonUpdate();
        hand.grabType = CustomHand.GrabType.Select;
		Grab.Invoke ();
    }

    private void GrabUpdate(CustomHand hand)
    {
        if ((rightHand || leftHand) && GetMyGrabPoserTransform(hand))
        {
            hand.SkeletonUpdate();
            GetComponentInChildren<MeshRenderer>().material.color = Color.grey;
            float tempDistance = Mathf.Clamp(_startButtonPosition-(_startButtonPosition-transform.InverseTransformPoint(hand.PivotPoser.position).z)*DistanceMultiply, _startButtonPosition, distanseToPress);
            if (tempDistance >= distanseToPress)
            {
                GetComponentInChildren<MeshRenderer>().material.color = Color.blue;
                if (!press)
                {
                    ButtonDown.Invoke();
                }
                press = true;
                ButtonUpdate.Invoke();
            }
            else
            {
                if (press)
                {
                    ButtonUp.Invoke();
                }
                press = false;
            }
            MoveObject.localPosition = new Vector3(0, 0, tempDistance);
            MoveObject.rotation = Quaternion.LookRotation(GetMyGrabPoserTransform(hand).forward, hand.PivotPoser.up);
            hand.GrabUpdateCustom();
        }
    }

    private void GrabEnd(CustomHand hand)
    {
        //if ((rightHand || leftHand) && GetMyGrabPoserTransform(hand))
        //{
            MoveObject.localPosition = new Vector3(0, 0, _startButtonPosition);
            DetachHand(hand);

            GetComponentInChildren<MeshRenderer>().material.color = Color.green;
        //}
		ReleaseHand.Invoke ();
    }
}
