using UnityEngine;

// Makes the rigged hand model ("Free Pack - VR Hands") follow the XR controllers.
//
// Both hands are one skinned mesh, so instead of moving GameObjects this script
// moves the two hand bones (J_Left / J_Right) every frame:
// 1. Rotation: the hand's real shape is measured from its finger bones
//    (wrist -> middle fingertip = "forward", pinky -> index = "across"), and the
//    bone is turned so the fingers point along the controller's forward and the
//    back of the hand faces the controller's up (palm down). rotationOffset adds
//    an extra tilt on top, in the controller's space.
// 2. Position: the bone is moved so its palm contact point sits at the
//    controller's positionOffset. The CPR scripts use those same palm contact
//    points, so what the player sees touching the casualty is what they measure.
//
// The model's Animator is switched off: its idle clip would overwrite the bones.
// Tune the offsets in Play mode, then use the component's "Copy Component" /
// "Paste Component Values" menu to keep them after leaving Play mode.
[DefaultExecutionOrder(10000)]
public class XRHandFollower : MonoBehaviour
{
    [System.Serializable]
    public class Hand
    {
        [Tooltip("XR controller transform (e.g. LeftHand Controller).")]
        public Transform controller;

        [Tooltip("Hand bone moved by this script (J_Left or J_Right).")]
        public Transform bone;

        [Tooltip("Palm point beneath the bone (LeftPalmContact / RightPalmContact).")]
        public Transform palmContact;

        [Tooltip("Where the palm sits, in the controller's local space (metres).")]
        public Vector3 positionOffset = new Vector3(0f, 0f, 0.05f);

        [Tooltip("Extra tilt of the hand relative to the controller (degrees). " +
                 "0,0,0 = fingers forward, palm down.")]
        public Vector3 rotationOffset;

        // Finger bones used to measure the hand's shape (found automatically)
        [System.NonSerialized] public Transform wrist, middleTip, index, pinky;
        [System.NonSerialized] public bool isRight;
    }

    public Hand left = new Hand();
    public Hand right = new Hand();

    [Tooltip("Switch off the hand model's Animator (its idle clip overwrites the bones).")]
    public bool disableAnimator = true;

    private void Awake()
    {
        if (disableAnimator)
        {
            foreach (Animator animator in GetComponentsInChildren<Animator>(true))
                animator.enabled = false;
        }

        FindFingerBones(left, false);
        FindFingerBones(right, true);
    }

    private void OnEnable()
    {
        Application.onBeforeRender += Follow;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= Follow;
    }

    // LateUpdate runs after the Animator; onBeforeRender uses the newest
    // controller pose, which the XR controllers also update just before rendering.
    private void LateUpdate()
    {
        Follow();
    }

    private void Follow()
    {
        Apply(left);
        Apply(right);
    }

    private static void FindFingerBones(Hand hand, bool isRight)
    {
        hand.isRight = isRight;

        if (hand.bone == null)
            return;

        string wristName = hand.bone.name + "_Hand"; // J_Left -> J_Left_Hand

        foreach (Transform t in hand.bone.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == wristName) hand.wrist = t;
            else if (t.name == wristName + "Middle4") hand.middleTip = t;
            else if (t.name == wristName + "Index1") hand.index = t;
            else if (t.name == wristName + "Pinky1") hand.pinky = t;
        }
    }

    // The hand's own frame (fingers = forward, back of hand = up),
    // expressed in the bone's local space.
    private static Quaternion HandFrame(Hand hand)
    {
        if (hand.wrist == null || hand.middleTip == null ||
            hand.index == null || hand.pinky == null)
            return Quaternion.identity;

        Vector3 fingers = hand.bone.InverseTransformDirection(
            hand.middleTip.position - hand.wrist.position);
        Vector3 across = hand.bone.InverseTransformDirection(
            hand.index.position - hand.pinky.position); // towards the thumb

        Vector3 backOfHand = hand.isRight
            ? Vector3.Cross(across, fingers)
            : Vector3.Cross(fingers, across);

        return Quaternion.LookRotation(fingers, backOfHand);
    }

    private static void Apply(Hand hand)
    {
        if (hand.controller == null || hand.bone == null)
            return;

        hand.bone.rotation =
            hand.controller.rotation *
            Quaternion.Euler(hand.rotationOffset) *
            Quaternion.Inverse(HandFrame(hand));

        Vector3 target = hand.controller.TransformPoint(hand.positionOffset);

        if (hand.palmContact != null)
            hand.bone.position += target - hand.palmContact.position;
        else
            hand.bone.position = target;
    }
}
