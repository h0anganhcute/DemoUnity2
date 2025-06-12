using UnityEngine;
using System.Collections;

public class BatManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator batAnimator;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform checkPointBat; 

    [Header("Behavior Settings")]
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float retreatDuration = 0.5f; 

    private Vector3 startPosition;
    private bool isActionLocked = false; 

    private enum BatState { Idle, Chasing, Returning, Retreating }
    private BatState currentState;

    void Start()
    {
        startPosition = checkPointBat != null ? checkPointBat.position : transform.position;
        if (playerTransform == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }
        
        currentState = BatState.Idle;
        batAnimator.SetBool("IsMove", false);
        batAnimator.SetInteger("State", 0);
    }
    void Update()
    {
        if (isActionLocked) return; 

        HandleStateTransitions();
        ExecuteCurrentStateAction();
        FlipSprite();
    }

    void HandleStateTransitions()
    {
        float distanceToPlayer = playerTransform != null ? Vector2.Distance(transform.position, 
            playerTransform.position) : float.MaxValue;

        switch (currentState)
        {
            case BatState.Idle:
                if (distanceToPlayer < attackRange)
                {
                    ChangeState(BatState.Chasing);
                }
                break;

            case BatState.Chasing:
                if (distanceToPlayer >= attackRange)
                {
                    ChangeState(BatState.Returning);
                }
                break;

            case BatState.Returning:
                if (Vector2.Distance(transform.position, startPosition) < 0.1f)
                {
                    ChangeState(BatState.Idle);
                }
                break;
        }
    }

    void ExecuteCurrentStateAction()
    {
        switch (currentState)
        {
            case BatState.Chasing:
                transform.position = Vector2.MoveTowards(transform.position, 
                    playerTransform.position, moveSpeed * Time.deltaTime);
                break;
            case BatState.Returning:
                transform.position = Vector2.MoveTowards(transform.position, 
                    startPosition, moveSpeed * Time.deltaTime);
                break;
        }
    }

    void ChangeState(BatState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case BatState.Idle:
                batAnimator.SetBool("IsMove", false);
                batAnimator.SetInteger("State", 1); 
                StartCoroutine(ResetStateParameter());
                break;

            case BatState.Chasing:
                batAnimator.SetBool("IsMove", true);
                batAnimator.SetInteger("State", 0); 
                break;
            case BatState.Returning:
                break;
        }
    }

    private IEnumerator ResetStateParameter()
    {
        yield return null; 
        batAnimator.SetInteger("State", 0);
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState == BatState.Chasing && !isActionLocked && collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(RetreatAfterAttack(collision.transform));
        }
    }

    private IEnumerator RetreatAfterAttack(Transform player)
    {
        isActionLocked = true;
        Vector2 retreatDirection = (transform.position - player.position).normalized;
        float retreatDistance = Camera.main.orthographicSize * Camera.main.aspect * 0.5f; 
        Vector3 targetPosition = transform.position + (Vector3)retreatDirection * retreatDistance;
        float timer = 0;
        Vector3 startRetreatPos = transform.position;
        while (timer < retreatDuration)
        {
            transform.position = Vector3.Lerp(startRetreatPos, targetPosition, timer / retreatDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        isActionLocked = false; 
    }
    
    private void FlipSprite()
    {
        if (playerTransform == null) return;
        Vector3 targetPos = (currentState == BatState.Chasing) ? playerTransform.position : startPosition;
        
        if (transform.position.x > targetPos.x)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}