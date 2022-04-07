using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    private bool useCustomGravity = true;
    private Vector3 gravityDirection;
    private float jumpPower = 10;
    private Rigidbody rb;
    private Collider coll;
    private bool ableToJump = true;
    private bool ableToAim = true;
    private bool inAiming = false;

    private RigidbodyConstraints defaultConstraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;


    [SerializeField]
    private GameObject vectorCircleSample;
    private GameObject currentVectorCircle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
        gravityDirection = Vector3.up;
    }

    public void Jump()
    {
        if (ableToJump)
        {
            rb.AddForce(gravityDirection * jumpPower, ForceMode.Force);
            ableToJump = false;
        }
    }

    private void FixedUpdate()
    {
       if(useCustomGravity) rb.AddForce(-gravityDirection * 9.8f, ForceMode.Acceleration);
    }


    private Vector3 startClickPos = Vector3.zero;
    private Vector3 currentClickPos;
    private float startClickTime = 0;
    private float currentClickTime;

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
        if (startClickPos != currentClickPos)
        {
            if (inAiming)
            {

            }
            else if (ableToAim)
            {
                ableToAim = false;
                inAiming = true;
                rb.velocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.FreezeAll;
                useCustomGravity = false;
                drawingCircleCoroutine = StartCoroutine(DrawAimCircle());
            }
            else
            {

            }
        }
    }

    private void OnMouseUp()
    {
        if (startClickPos != currentClickPos)
        {
            if (inAiming)
            {
                inAiming = false;
                rb.constraints = defaultConstraints;
                useCustomGravity = true;
                rb.AddForce((startClickPos - currentClickPos).normalized * jumpPower, ForceMode.Force);
                if (drawingCircleCoroutine != null) StopCoroutine(drawingCircleCoroutine);
                Destroy(currentVectorCircle);
            }
        }
        else
        {
            if (ableToJump)
            {
                Jump();
                ableToJump = false;
            }
        }

    }

    //player cannot jump and aim again until he touches platform on which cube can stay
    //maxInclineAngle is used for defining, can cube stay on the platform or not
    //maxInclineAngle represents maximum angle between gravity direction and platform's normal vector
    //factically its a maximum dot product between gravity direction and normal vector of contact point cube_platform collision
    //value scales from 0 to 1, where 1 - vectors shoud point in the same direction (0 degress diff), 0 - vectors may be perpendicular (90 degrees diff)
    //default value is 0.9 (~15 degress diff)

    private float maxInclineAngle = .7f;
    private void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contactPoint in collision.contacts)
        {
            if (Vector3.Dot(contactPoint.normal, gravityDirection) >= maxInclineAngle)
            {
                ableToAim = true;
                ableToJump = true;
            }
        }
    }


    private Coroutine drawingCircleCoroutine;

    IEnumerator DrawAimCircle()
    {
        ableToAim = false;
        currentVectorCircle = Instantiate(vectorCircleSample, transform);
        ParticleSystem circle = currentVectorCircle.GetComponent<ParticleSystem>();

        float appearingTimer = .25f;
        float currentTimer = 0;
        Color startColor = circle.startColor;
        Color targetColor = startColor;
        targetColor.a = 1;

        while (currentTimer < appearingTimer)
        {
            currentTimer += Time.deltaTime;
            currentVectorCircle.GetComponent<ParticleSystem>().startColor = Color.Lerp(startColor, targetColor, currentTimer / appearingTimer);
            currentVectorCircle.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, currentTimer / appearingTimer);
            yield return new WaitForEndOfFrame();
        }
        drawingCircleCoroutine = null;
    }
}
