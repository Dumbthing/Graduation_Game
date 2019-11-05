﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]

public class Movement : MonoBehaviour
{
    private Rigidbody playerRB;
    private bool touchStart = false, canMove = false, canJump = true, canPush, isJumping = false;

    private float swipeTimeThreshold = 0.3f, swipeTimeTimer;
    private Vector3 initTouchPos;
    private Vector3 currTouchPos;

    private Vector3 joystickPos;
    private Vector3 stickLimitPos;

    private Vector2 swipeStartPos, swipeEndPos, swipeDirection;

    [Tooltip("Put the joystick here")]
    public Transform stick;
    [Tooltip("Put the joystick border here")]
    public Transform stickLimit;

    public FloatReference sneakSpeed;
    public FloatReference walkSpeed;
    public FloatReference runSpeed;
    public FloatReference rotationSpeed;
    public FloatReference jumpHeight;
    public IntReference radius;

    public FloatVariable currentSpeed;

    public FloatReference fallMultiplier;

    public IntReference attackDegree;

    public FloatReference attackRange;

    public IntReference swipePixelDistance;

    public IntVariable moveState;
    private RaycastHit[] hit;

    public GameObject leftFootPos, rightFootPos;

    private SphereCollider playerTrigger;

    public LayerMask interactableLayers;

    private GameObject[] enemyReferences;

    private GameObject[] interactableReferences;

    //public FloatReference floatingWeight;

    private int leftTouch = 99;
    private int rightTouch = 98;
    private Vector3 _previousPosition = Vector3.zero;
    [Header("Idle until this has been reached must be smaller than sneak threshold!")]
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float idleThreshold = 0.1f;

    [Header("Sneak threshold must be smaller than run threshold!")]
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float sneakThreshold;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float runThreshold;
    //public List<Touchlocation> touches = new List<Touchlocation>();
    void Start()
    {
        playerRB = GetComponent<Rigidbody>();
        enemyReferences = GameObject.FindGameObjectsWithTag("Enemy");
        interactableReferences = GameObject.FindGameObjectsWithTag("Interactable");
        Debug.Log("Length of interactables in scene: " + interactableReferences.Length + "Enemies in scene: " + enemyReferences.Length);
    }

    void Awake()
    {
        playerTrigger = GetComponent<SphereCollider>();
        playerTrigger.radius = attackRange.value;
        playerTrigger.isTrigger = true;
    }

    void FixedUpdate()
    {
        if (playerRB.velocity.y <= 0 && isJumping)
        {
            //playerRB.mass * fallMultiplier.value;

            playerRB.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier.value - 1) * Time.deltaTime;

            if (Physics.Raycast(leftFootPos.transform.position, Vector3.down, 0.10f) || Physics.Raycast(rightFootPos.transform.position, Vector3.down, 0.10f))
            {
                isJumping = false;
            }
        }
        _previousPosition = transform.position;
        // Making sure touches only run on Android
#if UNITY_ANDROID
        int i = 0;
        while (i < Input.touchCount)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began)
            {
                if (t.position.x < Screen.width / 2)
                {
                    stick.gameObject.SetActive(true);
                    stickLimit.gameObject.SetActive(true);
                    leftTouch = t.fingerId;
                    stickLimit.transform.position = t.position;
                    canMove = true;

                }


                else if (t.position.x > Screen.width / 2)
                {
                    rightTouch = t.fingerId;
                    swipeStartPos = t.position;

                    swipeTimeTimer = Time.time;
                    //Debug.Log("Began phase: " + swipeTimeTimer);

                    /*                     if(canJump && rightTouch == t.fingerId) {
                                            playerJump(Vector3.up, jumpHeight.value);
                                        } */

                    // Start timer on finger down.

                }

            }
            else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && leftTouch == t.fingerId && canMove)
            {

                Vector3 offset = new Vector3(t.position.x - stickLimit.transform.position.x, 0, t.position.y - stickLimit.transform.position.y);
                Vector3 direction = Vector3.ClampMagnitude(offset, 1.0f);
                float dragDist = Vector2.Distance(stick.transform.position, stickLimit.transform.position);
                Vector2 joyDiff = t.position - new Vector2(stickLimit.transform.position.x, stickLimit.transform.position.y);
                // Need new clamping.
                joyDiff = Vector2.ClampMagnitude(joyDiff, radius.value);

                if (dragDist <= radius.value * idleThreshold)
                {
                    //movePlayer(direction,0);
                    //moveState.value = 0;
                }
                else if (dragDist > radius.value * idleThreshold && dragDist <= radius.value * sneakThreshold)
                {
                    movePlayer(direction, sneakSpeed.value);
                    //moveState.value = 1;
                }
                else if (dragDist > radius.value * sneakThreshold && dragDist < radius.value * runThreshold)
                {
                    movePlayer(direction, walkSpeed.value);
                    //moveState.value = 2;
                }
                else if (dragDist >= radius.value * runThreshold)
                {
                    movePlayer(direction, runSpeed.value);
                    //moveState.value = 3;
                }
                stick.transform.position = joyDiff + new Vector2(stickLimit.transform.position.x, stickLimit.transform.position.y);
                // t.deltaPosition; is a Vector2 of the difference between the last frame to its position this frame. 
                if (stickLimit.transform.position.x < Screen.width / 2)
                {
                    stick.gameObject.SetActive(true);
                    stickLimit.gameObject.SetActive(true);

                }
                else if (t.position.x > Screen.width / 2)
                {

                    // Swipe movement? Maybe use t.deltaPosition to check change for swiping. 
                    // Could use TouchPhase.Stationary for just jumping?
                }

            }
            else if (t.phase == TouchPhase.Ended && leftTouch == t.fingerId)
            {
                //if(leftTouch == t.fingerId) {
                leftTouch = 99;
                stick.gameObject.SetActive(false);
                stickLimit.gameObject.SetActive(false);
                if (canMove)
                    canMove = false;
                //}

            }
            else if (t.phase == TouchPhase.Ended && rightTouch == t.fingerId)
            {
                rightTouch = 98;
                swipeEndPos = t.position;
                Vector2 swipeOffSet = new Vector2(swipeEndPos.x - swipeStartPos.x, swipeEndPos.y - swipeStartPos.y);
                swipeDirection = swipeOffSet.normalized;
                Vector3 worldDirection = new Vector3(swipeDirection.x, 0, swipeDirection.y);
                //Debug.Log("End phase: " + Time.time);
                if (swipeOffSet.magnitude > swipePixelDistance.value)
                {
                    //Debug.Log("Swipe");
                    Debug.DrawLine(swipeStartPos, swipeStartPos + swipeDirection * 300, Color.red, 5);
                    Debug.DrawLine(playerRB.transform.position, playerRB.transform.position + worldDirection * 5, Color.green, 5);
                    playerAttack(worldDirection);
                }
                else if (swipeTimeTimer + swipeTimeThreshold >= Time.time)
                {
                    playerJump(Vector3.up, jumpHeight.value);
                    //Debug.Log("Jump");
                }

            }


            ++i;
        }
#endif

        // Making sure Mouse only runs on PC.
        // If this says && !UNITY_ANDROID, delete it. This is used to test on Unity Remote
#if UNITY_EDITOR && !UNITY_ANDROID
        if (Input.GetMouseButtonDown(0))
        {
            initTouchPos = Input.mousePosition;

            // Joystick anchor where you initially press. Circle around is the sneak/Walk indicator, if you drag outside it starts running.
            // Movement function which changes between the 3 based on thresholds. Make script, then fit it with UI, so we can disable it. 
            // Movement keeps working, as long as initial position is on the left side of the screen. 

            stickLimit.transform.position = Input.mousePosition;
            stick.transform.position = Input.mousePosition;
            // If the anchor point for Joystick is on the left side of the screen, allow movement.
            if (stickLimit.transform.position.x < Screen.width / 2)
            {
                canMove = true;
                stick.gameObject.SetActive(true);
                stickLimit.gameObject.SetActive(true);
            }

            if (Input.mousePosition.x > Screen.width / 2 && canJump)
            {
                swipeStartPos = Input.mousePosition;
                swipeTimeTimer = Time.time;
                //playerJump(Vector3.up, jumpHeight.value);
            }

        }
        if (Input.GetMouseButton(0))
        {

            touchStart = true;
            currTouchPos = Input.mousePosition;
        }
        else
        {
            touchStart = false;
        }

        if (Input.GetMouseButtonUp(0))
        {
            swipeEndPos = Input.mousePosition;
            Vector2 swipeOffSet = new Vector2(swipeEndPos.x - swipeStartPos.x, swipeEndPos.y - swipeStartPos.y);
            swipeDirection = swipeOffSet.normalized;
            Vector3 worldDirection = new Vector3(swipeDirection.x, 0, swipeDirection.y);
            //Debug.Log("End phase: " + Time.time);
            if (swipeOffSet.magnitude > swipePixelDistance.value)
            {
                //Debug.Log("Swipe");
                Debug.DrawLine(swipeStartPos, swipeStartPos + swipeDirection * 300, Color.red, 5);
                Debug.DrawLine(playerRB.transform.position, playerRB.transform.position + worldDirection * 5, Color.green, 5);


            }
            else if (swipeTimeTimer + swipeTimeThreshold >= Time.time)
            {
                playerJump(Vector3.up, jumpHeight.value);
                //Debug.Log("Jump");
            }
        }

        if (touchStart && canMove)
        {

            Vector3 offset = new Vector3(currTouchPos.x - initTouchPos.x, 0, currTouchPos.y - initTouchPos.y);
            Vector3 direction = Vector3.ClampMagnitude(offset, 1.0f);
            float dragDist = Vector2.Distance(stick.transform.position, stickLimit.transform.position);
            Vector2 joyDiff = Input.mousePosition - stickLimit.transform.position;
            joyDiff = Vector2.ClampMagnitude(joyDiff, radius.value);

            if (dragDist <= radius.value * idleThreshold)
            {
                //movePlayer(direction, 0);
                //moveState.value = 0;
            }
            else if (dragDist <= radius.value * sneakThreshold)
            {
                movePlayer(direction, sneakSpeed.value);
                //moveState.value = 1;
            }
            else if (dragDist > radius.value * sneakThreshold && dragDist < radius.value * runThreshold)
            {
                movePlayer(direction, walkSpeed.value);
                //moveState.value = 2;
            }
            else if (dragDist >= radius.value * runThreshold)
            {
                movePlayer(direction, runSpeed.value);
                //moveState.value = 3;
            }



            stick.transform.position = joyDiff + new Vector2(stickLimit.transform.position.x, stickLimit.transform.position.y);
        }
        else
        {
            stick.gameObject.SetActive(false);
            stickLimit.gameObject.SetActive(false);
            canMove = false;
        }
#endif



        SetState();
    }
    

    private void movePlayer(Vector3 direction, float speedMove)
    {
        Quaternion rotation = direction != Vector3.zero
            ? Quaternion.LookRotation(direction) : Quaternion.identity; // Shorthand if : else
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotationSpeed.value);
        playerRB.MovePosition(transform.position + (direction * speedMove * Time.deltaTime));
    }

    private void playerJump(Vector3 direction, float jumpHeight)
    {
        if (!isJumping && (Physics.Raycast(leftFootPos.transform.position, Vector3.down, 0.10f) || Physics.Raycast(rightFootPos.transform.position, Vector3.down, 0.10f)))
        {
            playerRB.AddForce(direction * jumpHeight, ForceMode.Impulse);
            /*         if(playerRB.velocity.y <= 0) {
                        //playerRB.mass * fallMultiplier.value;
                        playerRB.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier.value -1) * Time.deltaTime;

                    } */
            // If the feet are atleast 10 cm away from the ground. 
            isJumping = true;

        }


        //canJump = false;
    }

    private void playerAttack(Vector3 direction) {
        Debug.Log("Attack direction: " + direction);
    }

    public float GetSpeed()
    {
        currentSpeed.value = Vector3.Distance(transform.position, _previousPosition) / Time.fixedDeltaTime;
        return currentSpeed.value;
    }

    public void SetState()
    {
        if (currentSpeed.value <= 0.01f)
        {
            moveState.value = 0;

        }
        else if (currentSpeed.value <= sneakSpeed.value + 0.05f)
        {
            moveState.value = 1;

        }
        else if (currentSpeed.value <= walkSpeed.value + 0.05f)
        {
            moveState.value = 2;

        }
        else
        {
            moveState.value = 3;
        }


    }
}
