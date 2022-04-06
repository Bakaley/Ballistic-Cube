using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{

    new private Rigidbody rigidbody;
    public float jumpAmount = 10;
    new private Collider collider;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();
    }

    public void Jump()
    {
        rigidbody.AddForce(Vector3.up * jumpAmount, ForceMode.Force);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }
    }

    private void OnMouseDown()
    {
        
    }

    private void OnMouseDrag()
    {
        
    }

    private void OnMouseUp()
    {
        
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
      
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("drag");
        rigidbody.velocity = Vector3.zero;
        rigidbody.useGravity = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    
    }
}
