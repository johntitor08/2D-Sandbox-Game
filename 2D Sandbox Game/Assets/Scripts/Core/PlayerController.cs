using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Inventory))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    
    [Header("Mining")]
    public float miningRange = 3f;
    public float miningCooldown = 0.3f;
    
    [Header("References")]
    public Camera mainCamera;
    
    private Rigidbody2D rb;
    private bool isGrounded;
    private float lastMineTime;
    private WorldGenerator worldGen;
    private Inventory inventory;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        worldGen = Object.FindFirstObjectByType<WorldGenerator>();
        inventory = GetComponent<Inventory>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        // Set spawn position
        if (worldGen != null)
            transform.position = new Vector3(worldGen.worldWidth / 2f, worldGen.surfaceHeight + 10, 0);
        else
            Debug.LogError("PlayerController: No WorldGenerator found in scene; cannot place player or mine/place blocks.");
    }
    
    void Update()
    {
        HandleMovement();
        HandleMining();
        HandlePlacing();
    }
    
    void HandleMovement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
        
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        
        // Camera follow
        if (mainCamera != null)
        {
            Vector3 camPos = mainCamera.transform.position;
            camPos.x = transform.position.x;
            camPos.y = transform.position.y;
            mainCamera.transform.position = camPos;
        }
    }
    
    void HandleMining()
    {
        if (worldGen == null || mainCamera == null) return;

        if (Input.GetMouseButton(0) && Time.time > lastMineTime + miningCooldown)
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int blockPos = new Vector2Int(
                Mathf.RoundToInt(mousePos.x),
                Mathf.RoundToInt(mousePos.y)
            );

            float distance = Vector2.Distance(transform.position, new Vector2(blockPos.x, blockPos.y));

            if (distance <= miningRange)
            {
                GameObject blockObj = GetBlockAt(blockPos);
                if (blockObj != null)
                {
                    Block block = blockObj.GetComponent<Block>();
                    if (block != null)
                    {
                        // Throttle every hit, not just the killing blow, so the
                        // cooldown actually limits damage to multi-hit blocks.
                        lastMineTime = Time.time;
                        if (block.TakeDamage())
                        {
                            if (inventory != null)
                                inventory.AddBlock(block.blockType);
                            worldGen.DestroyBlock(blockPos);
                        }
                    }
                }
            }
        }
    }

    void HandlePlacing()
    {
        if (worldGen == null || mainCamera == null) return;

        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int blockPos = new Vector2Int(
                Mathf.RoundToInt(mousePos.x),
                Mathf.RoundToInt(mousePos.y)
            );
            
            float distance = Vector2.Distance(transform.position, new Vector2(blockPos.x, blockPos.y));
            
            if (distance <= miningRange && !worldGen.IsBlockAt(blockPos))
            {
                if (inventory != null)
                {
                    GameObject blockPrefab = inventory.GetSelectedBlockPrefab();
                    if (blockPrefab != null && inventory.RemoveBlock(inventory.selectedSlot))
                    {
                        worldGen.PlaceBlock(blockPos, blockPrefab);
                    }
                }
            }
        }
    }
    
    GameObject GetBlockAt(Vector2Int pos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(new Vector2(pos.x, pos.y), 0.1f);
        foreach (var hit in hits)
        {
            Block block = hit.GetComponent<Block>();
            if (block != null && block.gridPosition == pos)
                return hit.gameObject;
        }
        return null;
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        // Only count contacts coming from below as "ground" so that brushing a
        // wall does not enable mid-air jumps.
        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        isGrounded = false;
    }
}