using Assets.Scripts.IAJ.Unity.Util;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.IAJ.Unity.Movement.DynamicMovement;

public class CharacterDynamicMovementController : MonoBehaviour {

    public const float X_WORLD_SIZE = 55;
    public const float Z_WORLD_SIZE = 32.5f;
    private const float MAX_ACCELERATION = 20.0f;
    private const float MAX_SPEED = 20.0f;
    private const float DRAG = 0.9f;

    private const float TIME_TO_TARGET = 2.0f;
    private const float RADIUS = 1.0f;
    private const float MAX_ROTATION = 8 * MathConstants.MATH_PI;

    public KeyCode stopKey = KeyCode.A;
    public KeyCode seekKey = KeyCode.S;
    public KeyCode fleeKey = KeyCode.D;
    public KeyCode arriveKey = KeyCode.F;
    public KeyCode wanderKey = KeyCode.G;

    public GameObject targetGameObject;
    public GameObject movementText;
    public DynamicCharacter character;
    public GameObject debugTarget;


    private DynamicCharacter targetCharacter;
    private Text movementTextText;

    //early initialization
    void Awake()
    {
        this.character = new DynamicCharacter(gameObject);
        this.movementTextText = this.movementText.GetComponent<Text>();
    }

    // Use this for initialization
    void Start ()
    {
        //gets the target DynamicCharacter's properties
        this.targetCharacter = this.targetGameObject.GetComponent<CharacterDynamicMovementController>().character;
    }
	
	// Update is called once per frame
	void Update () {

        if (Input.GetKeyDown(stopKey))
        {
            this.character.Movement = null;
            this.debugTarget.SetActive(false);
        }

        else if (Input.GetKeyDown(seekKey))
        {
            this.debugTarget.SetActive(false);
            this.character.Movement = new DynamicSeek
            {
                Character = this.character.KinematicData,
                Target = this.targetCharacter.KinematicData,
                MaxAcceleration = MAX_ACCELERATION
            };
        }
        else if (Input.GetKeyDown(fleeKey))
        {
            this.debugTarget.SetActive(false);
            this.character.Movement = new DynamicFlee
            {
                Character = this.character.KinematicData,
                Target = this.targetCharacter.KinematicData,
                MaxAcceleration = MAX_ACCELERATION
            };
        }
        else if (Input.GetKeyDown(arriveKey))
        {
            this.debugTarget.SetActive(false);
            this.character.Movement = new DynamicArrive
            {
                Character       = this.character.KinematicData,
                DesiredTarget   = this.targetCharacter.KinematicData,
                MaxAcceleration = MAX_ACCELERATION
            };
        }
        else if (Input.GetKeyDown(wanderKey))
        {
            this.debugTarget.SetActive(true);
            this.character.Movement = new DynamicWander

            {
                Character = this.character.KinematicData,
                WanderOffset = 15,
                WanderRate = MathConstants.MATH_PI_4,
                DebugTarget = this.debugTarget,
                MaxAcceleration = MAX_ACCELERATION
            };
        }

        UpdateMovingGameObject();
        UpdateMovementText();
	}

    void OnDrawGizmos()
    {
        if(this.character != null && this.character.Movement!=null)
        {
            var wander = this.character.Movement as DynamicWander;
            if(wander != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(wander.CircleCenter, wander.WanderRadius);
            }
        }
    }

    private void UpdateMovingGameObject()
    {
        if (this.character.Movement != null)
        {
            this.character.Update();
            this.character.KinematicData.ApplyWorldLimit(X_WORLD_SIZE, Z_WORLD_SIZE);
        }
    }

    private void UpdateMovementText()
    {
        if (this.character.Movement == null)
        {
            this.movementTextText.text = this.name + " Movement: Stationary";
        }
        else
        {
            this.movementTextText.text = this.name + " Movement: " + this.character.Movement.Name;
        }
    } 

}
