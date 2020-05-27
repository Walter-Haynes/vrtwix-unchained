using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

using Valve.VR;

public class CustomHand : MonoBehaviour
{
    #region Variables

    public float grabRadius, indexRadius, pinchRadius;//different grabs radiuses
    public Vector3 grabPoint = new Vector3(0, 0, -.1f), indexPoint = new Vector3(-0.04f, -0.055f, -0.005f), pinchPoint = new Vector3(0, 0, -.05f);//локальная позиция точки захвата левой руки

    public LayerMask layerColliderChecker;//Layer to interact & grab with
    public SteamVR_Action_Boolean grabButton, pinchButton;//grab inputs
    public SteamVR_Action_Single SqueezeButton;//squeeze input he he
    public SteamVR_Input_Sources handType;//hand type, is it right or left
    public GrabType grabType;// current grab type
    public enum GrabType
    {
        None,
        Select,
        Grip,
        Pinch,
    }
    public SteamVR_RenderModel RenderModel;// controller model
    [Range(0.001f, 1f)]
    public float blend = .1f, blendPosition = .1f;// hand blend transition speed
    public bool smoothBlendPhysicsObject;// smooth pickup of physical object
    public Collider[] selectedGrapColliders, selectedIndexColliders, selectedPinchColliders;//colliders in a grab radius
    public CustomInteractable selectedIndexInteractable, selectedPinchInteractable, selectedGripInteractable, grabInteractable;// nearest interaction objects and object is currently interacting with
    public SteamVR_Behaviour_Skeleton skeleton;// current hand's skeleton
    public SteamVR_Skeleton_Poser grabPoser;// poser of object currently interacting with
    public Vector3 posSavePoser, rotSavePoser, inverceLocalPosition;//magic variables, which are need to calculate something ( need to know )
    public Transform PivotPoser, ToolTransform;//Pivot from hands poser, hidden instrument to simplify some calculations
    public bool HideController, alwaysHideController;//hide controller
    public float Squeeze;//squeeze strength 
    public SteamVR_Action_Vibration hapticSignal = SteamVR_Input.GetAction<SteamVR_Action_Vibration>("Haptic");//Output of haptic ramble
    private bool setHandTransform;//Assing position, to pass of the 1st frame, used to be a bug ( maybe remove, need to check if this bug still here )
    private float blendToAnimation = 1, blendToPose = 1, blendToPoseMoveObject = 1;//smooth transition for animation and pose

    private Vector3 endFramePos, oldInterpolatePos;
    private Quaternion endFrameRot, oldInterpolateRot;
    
    #endregion

    #region Methods
    
    //protected SteamVR_Events.Action renderModelLoadedAction;

    //protected void Awake()
    //{
    //    renderModelLoadedAction = SteamVR_Events.RenderModelLoadedAction(OnRenderModelLoaded);
    //}

    //private void OnRenderModelLoaded(SteamVR_RenderModel loadedRenderModel, bool success)
    //{
    //    print(1);
    //}

    private void Start()
    {
        if (!PivotPoser)
            PivotPoser = new GameObject().transform;
        PivotPoser.hideFlags = HideFlags.HideInHierarchy;
        if (!ToolTransform)
            ToolTransform = new GameObject().transform;
        ToolTransform.hideFlags = HideFlags.HideInHierarchy;

        if (GetComponent<SteamVR_Behaviour_Pose>())
        {
            handType = GetComponent<SteamVR_Behaviour_Pose>().inputSource;
        }
        else
        {
            Debug.LogError("no SteamVR_Behaviour_Pose on this object");
        }
        if (GetComponentInChildren<SteamVR_Behaviour_Skeleton>())
        {
            skeleton = GetComponentInChildren<SteamVR_Behaviour_Skeleton>();
        }
        if (GetComponentInChildren<SteamVR_RenderModel>())
        {
            RenderModel = GetComponentInChildren<SteamVR_RenderModel>();
            StartCoroutine(HideControllerCoroutine());
        }
        skeleton.BlendToSkeleton();
        
    }

    private void FixedUpdate()
    {
        SelectIndexObject();
        Squeeze = SqueezeButton.GetAxis(handType);
        PivotUpdate();
        GrabCheck();

        if (grabPoser && grabInteractable)
        {
            GrabUpdate();
            return;
        }

        SelectPinchObject();
        SelectGripObject();

    }

    private IEnumerator HideControllerCoroutine() {
        while (true)
        {
            if (RenderModel.transform.childCount > 0)
            {
                RenderModelVisible(HideController);
                break;
            }
            yield return 0;
        }
    }

    private void GrabCheck()
    {
        if (grabType != GrabType.None && grabInteractable)
        {
            if (grabType == GrabType.Pinch && pinchButton.GetStateUp(handType))
            {
                grabInteractable.SendMessage("GrabEnd", this, SendMessageOptions.DontRequireReceiver);
                GrabEnd();
            }
            if (grabType == GrabType.Grip && grabButton.GetStateUp(handType))
            {
                grabInteractable.SendMessage("GrabEnd", this, SendMessageOptions.DontRequireReceiver);
                GrabEnd();
            }
        }

        if (!grabPoser)
        {
            if (blend > 0)
            {
                blendToAnimation += 1f / blend * Time.deltaTime;
                blendToAnimation = Mathf.Clamp01(blendToAnimation);
                blendToPose += 1f / blendPosition * Time.deltaTime;
                blendToPose = Mathf.Clamp01(blendToPose);
                blendToPoseMoveObject += 1f / blendPosition * Time.deltaTime;
                blendToPoseMoveObject = Mathf.Clamp01(blendToPoseMoveObject);
            }
            else
            {
                blendToAnimation = 1;
            }

            CustomInteractable __oldGrabInteractable = grabInteractable;
            if (selectedIndexInteractable)
            {
                grabInteractable = selectedIndexInteractable;
                if (grabInteractable != __oldGrabInteractable)
                {
                    if (__oldGrabInteractable)
                        __oldGrabInteractable.SendMessage("GrabEnd", this, SendMessageOptions.DontRequireReceiver);
                    if (grabInteractable)
                    {
                        grabInteractable.SendMessage("GrabStart", this, SendMessageOptions.DontRequireReceiver);
                        setHandTransform = false;
                        grabType = GrabType.Select;
                        RenderModelVisible(!grabInteractable.hideController);
                        SkeletonUpdate();
                        blendToPose = 1;
                        blendToPoseMoveObject = 1;
                        endFramePos = transform.parent.InverseTransformPoint(skeleton.transform.position);
                        endFrameRot = skeleton.transform.rotation;
                    }
                }
            }
            else
            {
                if (selectedPinchInteractable && pinchButton.GetStateDown(handType))
                {
                    grabInteractable = selectedPinchInteractable;
                    if (grabInteractable != __oldGrabInteractable)
                    {
                        if (__oldGrabInteractable)
                            __oldGrabInteractable.SendMessage("GrabEnd", this, SendMessageOptions.DontRequireReceiver);
                        if (grabInteractable)
                        {
                            grabInteractable.SendMessage("GrabStart", this, SendMessageOptions.DontRequireReceiver);
                            setHandTransform = false;
                            grabType = GrabType.Pinch;
                            RenderModelVisible(!grabInteractable.hideController);
                            SkeletonUpdate();
                            blendToPose = 1;
                            blendToPoseMoveObject = 1;
                            endFramePos = transform.parent.InverseTransformPoint(skeleton.transform.position);
                            endFrameRot = skeleton.transform.rotation;
                        }
                    }
                }
                else
                {
                    if (selectedGripInteractable && grabButton.GetStateDown(handType))
                    {
                        grabInteractable = selectedGripInteractable;
                        if (grabInteractable != __oldGrabInteractable)
                        {
                            if (__oldGrabInteractable)
                                __oldGrabInteractable.SendMessage("GrabEnd", this, SendMessageOptions.DontRequireReceiver);
                            if (grabInteractable)
                            {
                                grabInteractable.SendMessage("GrabStart", this, SendMessageOptions.DontRequireReceiver);
                                setHandTransform = false;
                                grabType = GrabType.Grip;
                                RenderModelVisible(!grabInteractable.hideController);
                                SkeletonUpdate();
                                blendToPose = 1;
                                blendToPoseMoveObject = 1;
                                endFramePos = transform.parent.InverseTransformPoint(skeleton.transform.position);
                                endFrameRot = skeleton.transform.rotation;
                            }
                        }
                    }
                }
            }
        }
    }
    
    public void GrabUpdateCustom()
    {
        if (grabPoser)
        {
            skeleton.BlendToPoser(grabPoser, 0);

            posSavePoser = grabPoser.transform.localPosition;
            rotSavePoser = grabPoser.transform.localEulerAngles;

            grabPoser.transform.rotation = transform.rotation * grabPoser.GetBlendedPose(skeleton).rotation;
            grabPoser.transform.position = transform.TransformPoint(grabPoser.GetBlendedPose(skeleton).position);

            PivotUpdate();

            inverceLocalPosition = grabPoser.transform.InverseTransformPoint(transform.position);

            grabPoser.transform.localPosition = posSavePoser;
            grabPoser.transform.localEulerAngles = rotSavePoser;

            skeleton.transform.position = grabPoser.transform.TransformPoint(inverceLocalPosition);
            skeleton.transform.rotation = grabPoser.transform.rotation * Quaternion.Inverse(grabPoser.GetBlendedPose(skeleton).rotation);
            if (blend > 0)
            {
                blendToAnimation -= 1f / blend * Time.deltaTime;
                blendToAnimation = Mathf.Clamp01(blendToAnimation);
                blendToPose -= 1f / blendPosition * Time.deltaTime;
                blendToPose = Mathf.Clamp01(blendToPose);
                blendToPoseMoveObject -= 1f / blendPosition * Time.deltaTime;
                blendToPoseMoveObject = Mathf.Clamp01(blendToPoseMoveObject);
            }
            else
            {
                blendToAnimation = 0;
            }
            skeleton.skeletonBlend = blendToAnimation;
        }
    }

    private void GrabUpdate()
    {

        if (grabPoser)
        {
            skeleton.BlendToPoser(grabPoser, 0);

            posSavePoser = grabPoser.transform.localPosition;
            rotSavePoser = grabPoser.transform.localEulerAngles;

            grabPoser.transform.rotation = transform.rotation * grabPoser.GetBlendedPose(skeleton).rotation;
            grabPoser.transform.position = transform.TransformPoint(grabPoser.GetBlendedPose(skeleton).position);

            PivotUpdate();

            inverceLocalPosition = grabPoser.transform.InverseTransformPoint(transform.position);

            grabPoser.transform.localPosition = posSavePoser;
            grabPoser.transform.localEulerAngles = rotSavePoser;

            grabInteractable.SendMessage("GrabUpdate", this, SendMessageOptions.DontRequireReceiver);
            if (blend > 0)
            {
                blendToAnimation -= 1f / blend * Time.deltaTime;
                blendToAnimation = Mathf.Clamp01(blendToAnimation);
                blendToPose -= 1f / blendPosition * Time.deltaTime;
                blendToPose = Mathf.Clamp01(blendToPose);
                blendToPoseMoveObject -= 1f / blendPosition * Time.deltaTime;
                blendToPoseMoveObject = Mathf.Clamp01(blendToPoseMoveObject);
            }
            else
            {
                blendToAnimation = 0;
            }
            skeleton.skeletonBlend = blendToAnimation;
        }
    }

    public void HapticResponse(float hlength, float hfreq, float hpower)
    {
        hapticSignal.Execute(0, hlength, hfreq, hpower, handType);

    }

    private void LateUpdate()
    {
        if (grabPoser)
        {

            if (setHandTransform)
            {

                skeleton.transform.position = grabPoser.transform.TransformPoint(inverceLocalPosition);
                skeleton.transform.rotation = grabPoser.transform.rotation * Quaternion.Inverse(grabPoser.GetBlendedPose(skeleton).rotation);

                skeleton.transform.position = Vector3.Lerp(skeleton.transform.position, transform.parent.TransformPoint(endFramePos), blendToPose);
                skeleton.transform.rotation = Quaternion.Lerp(skeleton.transform.rotation, endFrameRot, blendToPose);

                oldInterpolatePos = skeleton.transform.position;
                oldInterpolateRot = skeleton.transform.rotation;
            }
            else
            {
                setHandTransform = true;
            }
        }
        else
        {
            skeleton.transform.position = Vector3.Lerp(transform.parent.TransformPoint(endFramePos), skeleton.transform.parent.position, blendToPose);
            skeleton.transform.rotation = Quaternion.Lerp(endFrameRot, skeleton.transform.parent.rotation, blendToPose);
        }


    }

    private void RenderModelVisible(bool visible)
    {
        if (RenderModel)
        {
            if (alwaysHideController)
                RenderModel.SetMeshRendererState(false);
            else
                RenderModel.SetMeshRendererState(visible);
        }
    }

    private void GrabEnd()
    {
        endFramePos = transform.parent.InverseTransformPoint(oldInterpolatePos);
        endFrameRot = oldInterpolateRot;

        skeleton.transform.localPosition = Vector3.zero;
        skeleton.transform.localEulerAngles = Vector3.zero; ///save coord
		skeleton.BlendToSkeleton(blend);

        RenderModelVisible(!HideController);
        blendToPose = 0;
        blendToPoseMoveObject = 0;
        grabPoser = null;
        grabInteractable = null;
        grabType = GrabType.None;
    }

    public void DetachHand()
    {
        GrabEnd();
    }

    private void SelectIndexObject()
    {
        if (!grabPoser)
        {
            selectedIndexInteractable = GetClosestInRadius(checkPosition: IndexPoint, checkRadius: indexRadius, colliders: ref selectedIndexColliders, desiredGrabType: GrabType.Pinch);
        }
        else
        {
            if(!selectedIndexInteractable) return;
            
            int __indexColliderAmount = Physics.OverlapSphereNonAlloc(position: IndexPoint, radius: indexRadius * 2, results: selectedIndexColliders, layerMask: layerColliderChecker);
            
            if (selectedIndexColliders == null || __indexColliderAmount <= 0)
            {
                selectedIndexInteractable.SendMessage("GrabEnd", this, SendMessageOptions.DontRequireReceiver);
                GrabEnd();
                selectedIndexInteractable = null;
                
                return;
            }
                
            for (int i = 0; i < __indexColliderAmount; i++)
            {
                CustomInteractable __tempCustomInteractable = selectedIndexColliders[i].GetComponentInParent<CustomInteractable>();
                if (__tempCustomInteractable && __tempCustomInteractable == selectedIndexInteractable)
                {
                    return;
                }
            }
                
            selectedIndexInteractable.SendMessage("GrabEnd", this, SendMessageOptions.DontRequireReceiver);
            GrabEnd();
            selectedIndexInteractable = null;
        }
    }

    private void SelectPinchObject()
    {
        selectedPinchInteractable = GetClosestInRadius(checkPosition: PinchPoint, checkRadius: pinchRadius, colliders: ref selectedPinchColliders, desiredGrabType: GrabType.Pinch);
    }

    private void SelectGripObject()
    {
        selectedGripInteractable = GetClosestInRadius(checkPosition: GrabPoint, checkRadius: grabRadius, colliders: ref selectedGrapColliders, desiredGrabType: GrabType.Grip);
    }

    private CustomInteractable GetClosestInRadius(in Vector3 checkPosition, in float checkRadius, ref Collider[] colliders, in GrabType desiredGrabType)
    {
        if(grabPoser) return null;
        
        int __colliderAmount = Physics.OverlapSphereNonAlloc(position: checkPosition, radius: checkRadius, results: colliders, layerMask: layerColliderChecker);
        
        CustomInteractable __closestInteractable = null;
        float __closestDistance = float.MaxValue;
        
        for (int __index = 0; __index < __colliderAmount; __index++)
        {
            if(!colliders[__index].transform.parent.TryGetComponent(component: out CustomInteractable __interactable)) continue;

            if(!__interactable.isInteractable || __interactable.grabType != desiredGrabType) continue;

            float __distanceToCheckPosition = __interactable.Distance(to: checkPosition);
            
            if(!(__distanceToCheckPosition < __closestDistance)) continue;

            __closestDistance = __distanceToCheckPosition;
            __closestInteractable = __interactable;
        }

        return __closestInteractable;
    }
    
    public void SkeletonUpdate()
    {
        if (skeleton)
        {
            if (grabPoser)
            {
                skeleton.BlendToPoser(grabPoser);
                PivotUpdate();
            }
        }
    }

    public void PivotUpdate()
    {
        if (grabPoser)
        {
            PivotPoser.rotation = transform.rotation * grabPoser.GetBlendedPose(skeleton).rotation;
            PivotPoser.position = transform.TransformPoint(grabPoser.GetBlendedPose(skeleton).position);
        }
    }

    #region Points

    [PublicAPI]
    public  Vector3 GrabPoint
    {
        get
        {
            switch(handType)
            {
                case SteamVR_Input_Sources.RightHand:
                    return transform.TransformPoint(Vector3.Scale(new Vector3(-1, 1, 1), grabPoint));
                case SteamVR_Input_Sources.LeftHand:
                    return transform.TransformPoint(grabPoint);
                default:
                    return Vector3.zero;
            }   
        }
    }
    [PublicAPI]
    public Vector3 PinchPoint
    {
        get
        {
            switch(handType)
            {
                case SteamVR_Input_Sources.RightHand:
                    return transform.TransformPoint(Vector3.Scale(new Vector3(-1, 1, 1), pinchPoint));
                case SteamVR_Input_Sources.LeftHand:
                    return transform.TransformPoint(pinchPoint);
                default:
                    return Vector3.zero;
            }   
        }
    }
    [PublicAPI]
    public Vector3 IndexPoint
    {
        get
        {
            switch(handType)
            {
                case SteamVR_Input_Sources.RightHand:
                    return transform.TransformPoint(Vector3.Scale(new Vector3(-1, 1, 1), indexPoint));
                case SteamVR_Input_Sources.LeftHand:
                    return transform.TransformPoint(indexPoint);
                default:
                    return Vector3.zero;
            }   
        }
    }

    #endregion

    public void SetEndFramePos() 
    {
        endFramePos = transform.parent.InverseTransformPoint(skeleton.transform.position);
    }

    public void SetBlendPose(float setBlend) 
    {
        blendToPoseMoveObject = setBlend;
    }

    public float GetBlendPose()
    {
        if (smoothBlendPhysicsObject) return 1 - blendToPoseMoveObject;
        return 1;

    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(PinchPoint, grabRadius);
        Gizmos.DrawWireSphere(GrabPoint, pinchRadius);
        Gizmos.DrawWireSphere(IndexPoint, indexRadius);
    }

    #endregion
}
