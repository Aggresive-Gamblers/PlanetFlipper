using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class PlayerController : MonoBehaviour
{
    public float rayCastLength;
    public float rotationSpeed;
    public float speed;
    public float airmult;
    public float gravity;
    public float gravitymul;
    public float gravitymulneg;
    public float jumpForce;
    public float maxmoveSpeed;
    private Rigidbody rb;
    public Transform currentPlanet;
    public Transform playerVisual;

    public Animator animator;
    
    RaycastHit[] hits;
    Vector3 planetDir;
    Vector3 normalVector;
    Vector3 input;
    Vector2 _move;

    public bool isTouchingPlanetSurface = false;
    private Transform MainCameraTransform;
    public Transform CameraArmTransform;
    public Transform DrillTransform;

    Animator anim;

    bool CanJump = true;
    public float jumpcooldown;
    bool IstryingJetpack = false;
    bool slowDown = false;

    bool isDrilling = false;

    [Header("ground check")]
    private Collider[] groundCollision;
    public bool grounded;
    public float spherecastradius;
    public LayerMask whatIsGround;

    public float airdrag;
    public float grounddrag;

    public float maxdist;

    public float jetpackforce;
    public bool CanJetpack;
    public bool didjump;

    public bool IsLockingMouse;

    [Header("Jetpack")]
    public float recoveryJetpackRate;
    public float useJetpackRate;
    public float JetpackReservoir;
    public float JetpackFuel;
    public Image JetpackFillBar;
    private bool IsStunned;

    [Header("Drill Settings")]
    public float drillBaseSpeed = 100f;      
    public float drillMaxSpeed = 1000f;       
    public float drillAcceleration = 2f;     
    private float currentDrillSpeed = 0f;

    [Header("Sound Settings")]
    public AudioClip drillSound;
    public AudioClip jetpackSound;

    public AudioClip chillmusic;
    public AudioClip whitenoise;

    public void OnMove(InputAction.CallbackContext ctx)
    {
        _move = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            if(CanJump && grounded && !IsStunned)
            {
                Jump();
            }
        }
    }

    public void OnJetpack(InputAction.CallbackContext ctx)
    {
        if(GameManager.Instance != null && GameManager.Instance.jetpackUnlocked)
        {
            if (ctx.started)
            {
                IstryingJetpack = true;
            }
            if (ctx.canceled)
            {
                IstryingJetpack = false;
                SoundManager.Instance.StopLoopSound(true,0);
            }
        }
    }

    public void OnDrill(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            isDrilling = true;
            IsStunned = true;
            SoundManager.Instance.PlayLoopSound(drillSound, 0.3f,1);

        }
        if (ctx.canceled)
        {
            isDrilling = false;
            IsStunned = false;
            SoundManager.Instance.StopLoopSound(true,1);
        }
    }

    public void TakeDamage(float damage,Transform kbpoint)
    {
        //healt - blablabal
        Debug.Log("BOOOM");
        rb.AddExplosionForce(damage*1000,kbpoint.position,10);
    }

    void HandleDrillingRotation()
    {
        if (isDrilling)
        {
            currentDrillSpeed = Mathf.Lerp(currentDrillSpeed, drillMaxSpeed, drillAcceleration);

            DrillTransform.Rotate(Vector3.up, currentDrillSpeed * Time.deltaTime, Space.Self);
        }
        else
        {
            currentDrillSpeed = Mathf.Lerp(currentDrillSpeed, 0f, drillAcceleration);

            DrillTransform.Rotate(Vector3.up, currentDrillSpeed * Time.deltaTime, Space.Self);
        }
    }


    public void Stunt()
    {
        Debug.Log("Badaboum");
        IsStunned = true;
        Invoke("UnStunt",5f);
    }

    public void UnStunt()
    {
        if (IsStunned)
        {
            IsStunned = false;  
        }
    }


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        CanJetpack = true;
        JetpackReservoir = 20;
        
        
    }

    private void Start()
    {
        FindCamera();
        if (GameManager.Instance != null && GameManager.Instance.reservoirUpgradeUnlocked)
        {
            JetpackReservoir = 40; 
            JetpackFuel = JetpackReservoir;
        }
        
        if(SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMusic(chillmusic,0);
            SoundManager.Instance.PlayMusic(whitenoise,1);
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVel;

        if (grounded)
        {
            if (rb.linearVelocity.magnitude > maxmoveSpeed)
            {
                Vector3 limitedVel = rb.linearVelocity.normalized * maxmoveSpeed;
                rb.linearVelocity = limitedVel;
            }
        }
    }

    private void Update()
    {
        if (MainCameraTransform == null)
        {
            FindCamera();
        }

        if (!didjump)
        {
            groundCollision = Physics.OverlapSphere(playerVisual.transform.position + playerVisual.transform.up * 0.3f, spherecastradius, whatIsGround);
            grounded = groundCollision.Length > 0;
        }

        HandleAnimation();
        HandleDrillingRotation();
        SpeedControl();

        JetpackFillBar.fillAmount = JetpackFuel / JetpackReservoir;
    }

    private void HandleAnimation()
    {
        animator.SetBool("Grounded" , grounded);
        animator.SetBool("Fall" , false); // wip
        animator.SetBool("Drilling", isDrilling);
        bool canUseJetpack = GameManager.Instance != null && GameManager.Instance.jetpackUnlocked;
        animator.SetBool("Jetpack" , IstryingJetpack && CanJetpack && !grounded && canUseJetpack); 
        animator.SetBool("Walking",_move != Vector2.zero);
        animator.SetFloat("DownSpeed", Vector3.Dot(rb.linearVelocity,-playerVisual.transform.up));
    }

    private void FindCamera()
    {
        if (Camera.main != null)
        {
            MainCameraTransform = Camera.main.transform;
        }
    }

    private void FixedUpdate()
    {
        Movement();
        ApplyGravity();
        ApplyPlanetRotation();

    }


    void Jump()
    {
        animator.SetTrigger("Jump");

        rb.AddForce(normalVector.normalized * jumpForce, ForceMode.Impulse);
        CanJump = false;
        grounded = false;
        didjump = true;
        Invoke(nameof(RestoreGravity), jumpcooldown);
    }
    void Jetpack()
    {
        rb.AddForce(-normalVector.normalized * (gravity * jetpackforce), ForceMode.Force);
        JetpackFuel -= useJetpackRate * Time.deltaTime;
        SoundManager.Instance.PlayLoopSound(jetpackSound, 0.3f,0);

        if (JetpackFuel < 0)
        {
            JetpackBurnOut();
        }
    }

    void JetpackBurnOut()
    {
        JetpackFuel = 0;
        CanJetpack = false;
    }


    void RestoreGravity()
    {
        CanJump = true;
        slowDown = false;
        didjump = false;
    }

    public void EnterNewGravityField(Transform newplanet,float mult,float multneg)
    {
        currentPlanet = newplanet;
        
        gravitymul = mult;
        gravitymulneg = multneg;
        //gravity = tmpGravity / 4f;
        //rb.linearVelocity *= .5f;
        //rotationSpeed = tmpRotationSpeed / 10f;
        //slowDown = true;
        //CanJump = false;
        //Invoke(nameof(RestoreGravity), .5f);
    }

    void Movement()
    {
        if (CanJetpack && grounded)
        {
            if (JetpackFuel >= JetpackReservoir)
            {
                JetpackFuel = JetpackReservoir;
            }
            else
            {
                JetpackFuel += recoveryJetpackRate * Time.deltaTime;
            }
        }

        input = new Vector3(_move.x, 0, _move.y);
        Vector3 cameraRotation = new Vector3(0, MainCameraTransform.localEulerAngles.y + CameraArmTransform.localEulerAngles.y, 0);
        Vector3 Dir = Quaternion.Euler(cameraRotation) * input;
        Vector3 movement_dir = (transform.forward * Dir.z + transform.right * Dir.x);

        if (movement_dir != Vector3.zero)
        {
            playerVisual.localRotation = Quaternion.Slerp(playerVisual.localRotation, Quaternion.LookRotation(Dir), 0.4f);
        }

        if (MainCameraTransform == null || CameraArmTransform == null || IsStunned)
            return;

        

        Vector3 currentNormalVelocity = Vector3.Project(rb.linearVelocity, normalVector.normalized);
        //rb.linearVelocity = currentNormalVelocity + (movement_dir * speed);

        if (grounded)
        {
            rb.AddForce(movement_dir * (speed * 10f), ForceMode.Force);
            rb.linearDamping = grounddrag;
            if (!CanJetpack)
            {
                CanJetpack = true;
                JetpackFuel = 0.1f;
            }
        }
        else
        {
            rb.AddForce(movement_dir * (speed * 10f * airmult), ForceMode.Force);
            rb.linearDamping = airdrag;
        }

        if (IstryingJetpack && CanJetpack && GameManager.Instance != null && GameManager.Instance.jetpackUnlocked)
        {
            Jetpack();
        }

        

        
        if (slowDown)
            rb.linearVelocity *= .5f;

    }

    void ApplyGravity()
    {
        if (currentPlanet == null) return;

        GetPlanetNormal();

        float distancepercent = (maxdist - Mathf.Clamp(normalVector.sqrMagnitude, 0, maxdist)) / maxdist;

        float gravityintensity = Mathf.Lerp(gravitymulneg, gravitymul, distancepercent);

        if (gravityintensity > -1 && gravityintensity < 1)
        {
            gravityintensity = 1;
        }
        if (gravityintensity < 0)
        {
            gravityintensity = -1 / gravityintensity;
        }

        rb.linearVelocity +=  normalVector.normalized * (gravity * gravityintensity);

        hits = new RaycastHit[0];
    }

    void ApplyPlanetRotation()
    {
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, normalVector) * transform.rotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        if (isTouchingPlanetSurface && CanJump)
            rotationSpeed = rotationSpeed;
    }

    void GetPlanetNormal()
    {
        if (currentPlanet == null) return;
        
        normalVector = (transform.position - currentPlanet.position);

    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerVisual.transform.position + playerVisual.transform.up * 0.3f, spherecastradius);
    }
}