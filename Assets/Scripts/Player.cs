using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    private bool useCustomGravity = true;
    private Vector3 gravityDirection = Vector3.down;
    //cube must jump perpendicullarly to platform, not in opposite to gravity
    private Vector3 jumpDirection = Vector3.up;
    [SerializeField]
    private float jumpPower = 20;
    [SerializeField]
    private float minBallisticPower = 5f;
    [SerializeField]
    private float maxBallisticPower = 20f;
    private float currentBallisticPower;

    private Rigidbody rb;
    private Collider coll;


    private enum CUBE_STATE
    {
        INACTIVE,
        CAN_JUMP,
        CAN_AIM,
        AIMING
    }

    CUBE_STATE cubeState = CUBE_STATE.CAN_JUMP;

    private RigidbodyConstraints defaultConstraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

    private Material material;
    [SerializeField]
    Color32 aimingColor = new Color32(18, 214, 255, 255);
    [SerializeField]
    Color32 canAimColor = new Color32(18, 214, 255, 255);
    [SerializeField]
    Color32 canJumpColor = new Color32(255, 92, 194, 255);
    [SerializeField]
    Color32 defaultColor = new Color32(145, 145, 145, 255);

    [SerializeField]
    private GameObject vectorCircleSample;
    private GameObject currentVectorCircle;
    [SerializeField]
    private GameObject arrowSample;
    private GameObject currentArrow;
    private float arrowOffsetAxeZ = -1;

    private void Awake()
    {
        Application.targetFrameRate = 60;

        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
        material = GetComponent<MeshRenderer>().material;
    }


    private void Update()
    {
        switch (cubeState)
        {
            case CUBE_STATE.CAN_JUMP: material.color = canJumpColor;
                break;
            case CUBE_STATE.CAN_AIM:
                material.color = canAimColor;
                break;
            case CUBE_STATE.AIMING:
                material.color = aimingColor;
                break;
            case CUBE_STATE.INACTIVE:
                material.color = defaultColor;
                break;
        }
    }

    private void FixedUpdate()
    {
       if(useCustomGravity) rb.AddForce(gravityDirection * 9.8f, ForceMode.Acceleration);
    }

    public void Jump()
    {
        rb.AddForce(jumpDirection * jumpPower, ForceMode.Force);
    }

    private Vector3 startClickPos = Vector3.zero;
    private Vector3 currentClickPos = Vector3.zero;
    private float startClickTime = 0;
    private float currentClickTime = 0;
    private Vector3 clampedDirection = Vector3.zero;

    private void OnMouseDown()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            startClickPos = Input.touches[0].position;
        }
        else if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            startClickPos = Input.mousePosition;
        }
        startClickTime = Time.time;
    }

    
    private float mouseDragThreshold = 5;
    private float minTimeToDrag = .7f;
    private float maxCircleMagnitude = 1.5f;

    private void OnMouseDrag()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            currentClickPos = Input.touches[0].position;
        }
        else if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            currentClickPos = Input.mousePosition;
        }
        if ((startClickPos - currentClickPos).magnitude > mouseDragThreshold && startClickTime - currentClickTime >= minTimeToDrag)
        {
            if (cubeState == CUBE_STATE.AIMING)
            {
                Vector3 worldStartPos = Camera.main.ScreenToWorldPoint(new Vector3(startClickPos.x, startClickPos.y, -Camera.main.transform.position.z - arrowOffsetAxeZ));
                Vector3 worldCurPos = Camera.main.ScreenToWorldPoint(new Vector3(currentClickPos.x, currentClickPos.y, -Camera.main.transform.position.z - arrowOffsetAxeZ));

                Vector3 localStartPos = transform.InverseTransformVector(worldStartPos);
                Vector3 localCurPos = transform.InverseTransformVector(worldCurPos);

                clampedDirection = Vector3.ClampMagnitude(localCurPos - localStartPos, maxCircleMagnitude);

                float cubeDeadzone = .5f;

                float angle = Vector3.Angle(Vector3.up, clampedDirection);
                if (clampedDirection.x > 0) angle = 360 - angle;
                currentArrow.transform.localRotation = Quaternion.Euler(0, 0, angle - 180);
                float scale = Mathf.Lerp(.25f, .6f, (clampedDirection.magnitude- cubeDeadzone) / (maxCircleMagnitude- cubeDeadzone));
                currentBallisticPower = Mathf.Lerp(minBallisticPower, maxBallisticPower, (clampedDirection.magnitude - cubeDeadzone) / (maxCircleMagnitude - cubeDeadzone));
                currentArrow.transform.localScale = new Vector3(scale, scale, scale);
            }
            else if (cubeState == CUBE_STATE.CAN_AIM || cubeState == CUBE_STATE.CAN_JUMP)
            {
                cubeState = CUBE_STATE.AIMING;

                rb.velocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.FreezeAll;
                useCustomGravity = false;

                drawingArrowCoroutine = StartCoroutine(DrawArrowCoroutine());
            }
            else
            {

            }
        }
    }

    private void OnMouseUp()
    {
        if ((startClickPos - currentClickPos).magnitude > mouseDragThreshold)
        {
            if (cubeState == CUBE_STATE.AIMING)
            {
                cubeState = CUBE_STATE.INACTIVE;
                rb.constraints = defaultConstraints;
                useCustomGravity = true;
                currentCollisionTime = 0;
                rb.AddForce((startClickPos - currentClickPos).normalized * currentBallisticPower, ForceMode.Force);
                if (drawingArrowCoroutine != null) StopCoroutine(drawingArrowCoroutine);
               // Destroy(currentVectorCircle);
                Destroy(currentArrow);
            }
        }
        else
        {
            if (cubeState == CUBE_STATE.CAN_JUMP)
            {
                currentCollisionTime = 0;
                Jump();
                cubeState = CUBE_STATE.CAN_AIM;
            }
        }
    }
    
    //player cannot jump and aim again until he touches platform on which cube can stay
    //maxInclineDotProduct is used for defining, can cube stay on the platform or not
    //maxInclineDotProduct represents maximum angle between gravity direction and platform's normal vector
    //factically its a maximum dot product between gravity direction and normal vector of contact point cube_platform collision
    //value scales from 0 to 1, where 1 - vectors must point in the same direction (0 degress diff), 0 - vectors may be perpendicular (90 degrees diff)
    //default value is 0.7 (~35 degress diff)

    [SerializeField]
    [Range(0, 1)]
    private float maxInclineDotProduct = .7f;

    private float collisionTimeThreshold = .25f;
    private float currentCollisionTime = 0;

    private void OnCollisionStay(Collision collision)
    {
        currentCollisionTime += Time.deltaTime;
        if ((cubeState == CUBE_STATE.INACTIVE || cubeState == CUBE_STATE.CAN_AIM) && (currentCollisionTime >= collisionTimeThreshold))
        {
            foreach (ContactPoint contactPoint in collision.contacts)
            {
                if (Vector3.Dot(contactPoint.normal, -gravityDirection) >= maxInclineDotProduct)
                {
                    jumpDirection = contactPoint.normal;
                    cubeState = CUBE_STATE.CAN_JUMP;
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (cubeState == CUBE_STATE.CAN_JUMP) cubeState = CUBE_STATE.CAN_AIM;
        currentCollisionTime = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        Orb orb;
        if (other.TryGetComponent<Orb>(out orb)){
            switch (orb.Type)
            {
                case Orb.ORB_TYPE.ADD_AIM:
                    cubeState = CUBE_STATE.CAN_AIM;
                    break;
                case Orb.ORB_TYPE.GRAVITY_UP:
                    gravityDirection = Vector3.up;
                    break;
                case Orb.ORB_TYPE.GRAVITY_DOWN:
                    gravityDirection = Vector3.down;
                    break;
                case Orb.ORB_TYPE.GRAVITY_LEFT:
                    gravityDirection = Vector3.left;
                    break;
                case Orb.ORB_TYPE.GRAVITY_RIGHT:
                    gravityDirection = Vector3.right;
                    break;
                case Orb.ORB_TYPE.COLLECTIBLE:
                    Destroy(other.gameObject);
                    break;
            }
        }
    }


    private Coroutine drawingArrowCoroutine;

    IEnumerator DrawArrowCoroutine()
    {
        currentArrow = Instantiate(arrowSample, transform);
        SpriteRenderer arrowSprite = currentArrow.GetComponent<SpriteRenderer>();

        float appearingTimer = .75f;
        float currentTimer = 0;
        Color startColor = arrowSprite.color;
        Color targetColor = startColor;
        targetColor.a = 1;

        while (currentTimer < appearingTimer)
        {
            currentTimer += Time.deltaTime;
            arrowSprite.color = Color.Lerp(startColor, targetColor, currentTimer / appearingTimer);
            yield return new WaitForEndOfFrame();
        }
        drawingArrowCoroutine = null;
    }
}
