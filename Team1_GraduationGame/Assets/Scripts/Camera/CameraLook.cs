﻿// Code owner: Jannik Neerdal
using UnityEngine;
using Cinemachine;

public class CameraLook : MonoBehaviour
{
    // --- Inspector
    [Tooltip("If no player is defined, the GameObject with the tag \"Player\" will be used.")]
    public Transform player;

    [Tooltip("If no Input is defined, the object with the script \"CameraMovement\" attached will be used.")]
    public CameraMovement camMovement;

    [Tooltip("The default camLookOffset that will always be added")]
    public Vector3 defaultOffset = new Vector3(0.0f,0.0f,0.0f);

    [Tooltip("Control the amount that the players direction influences the camera.")]
    [Range(0.0f, 1.0f)] [SerializeField] private float lookDirFactor = 0.5f;
    public CameraOffset[] offsetTrack;

    // --- Hidden
    [HideInInspector] public Vector3 camTarget;
    private Movement playerMovement;
    private CinemachineSmoothPath cmPath;
    private Camera cam;
    private Vector3 lookDir;
    private Vector3 camLookOffset;
    [HideInInspector] public Vector3 camPosOffset;
    private float offsetTrackLerpValue, startingFOV;

    private void Awake()
    {
        if (GetComponent<CinemachineSmoothPath>() != null)
            cmPath = GetComponent<CinemachineSmoothPath>();
        else
            Debug.LogError("The DollyTrack is missing its track!", gameObject);
    }

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        if (player.GetComponent<Movement>() != null)
            playerMovement = player.GetComponent<Movement>();
        else
            Debug.LogError("The player does not have a movement script attached!", player);

        if (camMovement == null)
            camMovement = FindObjectOfType<CameraMovement>();
        cam = camMovement.GetComponent<Camera>();
        startingFOV = cam.fieldOfView;
    }

    public void OnArrayChanged() // Called in a custom inspector
    {
        if (cmPath == null)
        {
            if (GetComponent<CinemachineSmoothPath>() != null)
                cmPath = GetComponent<CinemachineSmoothPath>();
            else
                Debug.LogError("The DollyTrack is missing its track!", gameObject);
        }

        if (cmPath.m_Waypoints.Length != offsetTrack.Length)
        {
            CameraOffset[] temp = new CameraOffset[offsetTrack.Length];
            for (int i = 0; i < offsetTrack.Length; i++)
            {
                temp[i] = new CameraOffset(offsetTrack[i].GetLook(), offsetTrack[i].GetPos(), offsetTrack[i].GetFOV());
            }
            offsetTrack = new CameraOffset[cmPath.m_Waypoints.Length];
            for (int i = 0; i < cmPath.m_Waypoints.Length && i < temp.Length; i++)
            {
                offsetTrack[i] = temp[i];
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(camTarget, 1.0f);
    }

    void LateUpdate()
    {
        if (playerMovement != null)
        {
            lookDir = player.forward * playerMovement.GetSpeed() * lookDirFactor;
        }

        if (camMovement != null)
        {
            offsetTrackLerpValue = (camMovement.camRail.position.x - cmPath.m_Waypoints[camMovement.previousTrackIndex].position.x - camMovement.trackX) /
                             (cmPath.m_Waypoints[camMovement.nextTrackIndex].position.x - cmPath.m_Waypoints[camMovement.previousTrackIndex].position.x);
            camLookOffset = Vector3.Lerp(offsetTrack[camMovement.previousTrackIndex].GetLook(), offsetTrack[camMovement.nextTrackIndex].GetLook(), offsetTrackLerpValue);
            camPosOffset = Vector3.Lerp(offsetTrack[camMovement.previousTrackIndex].GetPos(), offsetTrack[camMovement.nextTrackIndex].GetPos(), offsetTrackLerpValue);
            cam.fieldOfView = Mathf.Lerp(startingFOV + offsetTrack[camMovement.previousTrackIndex].GetFOV(), startingFOV + offsetTrack[camMovement.nextTrackIndex].GetFOV(), offsetTrackLerpValue);
        }
        camTarget = player.position + camLookOffset + lookDir;
    }
}
