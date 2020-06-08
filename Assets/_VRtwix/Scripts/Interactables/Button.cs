using UnityEngine;
using UnityEngine.Events;
public class Button : CustomInteractable
{
    public float distanseToPress; //button press reach distance
    [Range(.1f,1f)]
    public float distanceMultiply=.1f; //button sensetivity slowdown
    public Transform moveObject; //movable button object
    public UnityEvent buttonDown, buttonUp, buttonUpdate; // events

    private float _startButtonPosition; //tech variable, assigned at start of pressed button
    private bool _press; //button check, to ButtonDown call 1 time

    private void Awake()
    {
        _startButtonPosition = moveObject.localPosition.z;
    }


    private void GrabStart(CustomHand hand)
    {
        SetInteractableVariable(hand);
        hand.SkeletonUpdate();
        hand.grabType = GrabType.Select;
		grab.Invoke ();
    }

    private void GrabUpdate(CustomHand hand)
    {
        if ((rightHand || leftHand) && GetMyGrabPoserTransform(hand))
        {
            hand.SkeletonUpdate();
            GetComponentInChildren<MeshRenderer>().material.color = Color.grey;
            float __tempDistance = Mathf.Clamp(_startButtonPosition-(_startButtonPosition-transform.InverseTransformPoint(hand.pivotPoser.position).z)*distanceMultiply, _startButtonPosition, distanseToPress);
            if (__tempDistance >= distanseToPress)
            {
                GetComponentInChildren<MeshRenderer>().material.color = Color.blue;
                if (!_press)
                {
                    buttonDown.Invoke();
                }
                _press = true;
                buttonUpdate.Invoke();
            }
            else
            {
                if (_press)
                {
                    buttonUp.Invoke();
                }
                _press = false;
            }
            moveObject.localPosition = new Vector3(0, 0, __tempDistance);
            moveObject.rotation = Quaternion.LookRotation(GetMyGrabPoserTransform(hand).forward, hand.pivotPoser.up);
            hand.GrabUpdateCustom();
        }
    }

    private void GrabEnd(CustomHand hand)
    {
        //if ((rightHand || leftHand) && GetMyGrabPoserTransform(hand))
        //{
            moveObject.localPosition = new Vector3(0, 0, _startButtonPosition);
            DetachHand(hand);

            GetComponentInChildren<MeshRenderer>().material.color = Color.green;
        //}
		releaseHand.Invoke ();
    }
}
