using System.Collections.Generic;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

// Enhanced MeshGenerator with collision, player control, and camera following
public class EnhancedMeshGenerator : MonoBehaviour
{
    public Material material;
    public int instanceCount = 100;
    private Mesh cubeMesh;
    private List<Matrix4x4> matrices = new List<Matrix4x4>();
    private List<int> colliderIds = new List<int>();

    public int enemyCount = 5;
    private List<Matrix4x4> enemyMatrices = new List<Matrix4x4>();
    private List<int> enemyColliderIds = new List<int>();

    public float width = 1f;
    public float height = 1f;
    public float depth = 1f;
    
    public float movementSpeed = 5f;
    public float gravity = 9.8f;
    public float jumpForce = 12f;
    
    private int playerID = -1;
    private Vector3 playerVelocity = Vector3.zero;
    private bool isGrounded = false;


    
    // Camera reference
    public PlayerCameraFollow cameraFollow;
    
    // Z-position constant for all boxes
    public float constantZPosition = 0f;
    
    // Range for random generation
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -50f;
    public float maxY = 50f;
    
    // Ground plane settings
    public float groundY = -20f;
    public float groundWidth = 200f;
    public float groundDepth = 200f;

    void Start()
    {
        // Find or create camera if not assigned
        SetupCamera();
        
        // Create the cube mesh
        CreateCubeMesh();
        
        // Create player box
        CreatePlayer();
        
        // Create ground
        CreateGround();
        
        // Set up random boxes
        GenerateRandomBoxes();

        SpawnEnemies();
    }
    
    void SetupCamera()
    {
        if (cameraFollow == null)
        {
            // Try to find existing camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Check if it already has our script
                cameraFollow = mainCamera.GetComponent<PlayerCameraFollow>();
                if (cameraFollow == null)
                {
                    // Add our script to existing camera
                    cameraFollow = mainCamera.gameObject.AddComponent<PlayerCameraFollow>();
                }
            }
            else
            {
                // No main camera found, create a new one
                GameObject cameraObj = new GameObject("PlayerCamera");
                Camera cam = cameraObj.AddComponent<Camera>();
                cameraFollow = cameraObj.AddComponent<PlayerCameraFollow>();
                
                // Set this as the main camera
                cam.tag = "MainCamera";
            }
            
            // Configure default camera settings
            cameraFollow.offset = new Vector3(0, 0, -15);
            cameraFollow.smoothSpeed = 0.1f;
        }
    }

    void CreateCubeMesh()
    {
        cubeMesh = new Mesh();
        
        // Create 8 vertices for the cube (corners)
        Vector3[] vertices = new Vector3[8]
        {
            // Bottom face vertices
            new Vector3(0, 0, 0),       // Bottom front left - 0
            new Vector3(width, 0, 0),   // Bottom front right - 1
            new Vector3(width, 0, depth),// Bottom back right - 2
            new Vector3(0, 0, depth),   // Bottom back left - 3
            
            // Top face vertices
            new Vector3(0, height, 0),       // Top front left - 4
            new Vector3(width, height, 0),   // Top front right - 5
            new Vector3(width, height, depth),// Top back right - 6
            new Vector3(0, height, depth)    // Top back left - 7
        };
        
        // Triangles for the 6 faces (2 triangles per face)
        int[] triangles = new int[36]
        {
            // Front face triangles (facing -Z)
            0, 4, 1,
            1, 4, 5,
            
            // Back face triangles (facing +Z)
            2, 6, 3,
            3, 6, 7,
            
            // Left face triangles (facing -X)
            0, 3, 4,
            4, 3, 7,
            
            // Right face triangles (facing +X)
            1, 5, 2,
            2, 5, 6,
            
            // Bottom face triangles (facing -Y)
            0, 1, 3,
            3, 1, 2,
            
            // Top face triangles (facing +Y)
            4, 7, 5,
            5, 7, 6
        };
        
        Vector2[] uvs = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / width, vertices[i].z / depth);
        }

        cubeMesh.vertices = vertices;
        cubeMesh.triangles = triangles;
        cubeMesh.uv = uvs;
        cubeMesh.RecalculateNormals();
        cubeMesh.RecalculateBounds();
        Debug.Log("Cube mesh vertices: " + cubeMesh.vertexCount);
    }
    
    void CreatePlayer()
    {
        // Create player at a specific position
        Vector3 playerPosition = new Vector3(0, 10, constantZPosition);
        Vector3 playerScale = Vector3.one;
        Quaternion playerRotation = Quaternion.identity;
        
        // Register with collision system - properly handle width/height/depth
        playerID = CollisionManager.Instance.RegisterCollider(
            playerPosition, 
            new Vector3(width * playerScale.x, height * playerScale.y, depth * playerScale.z), 
            true);
        
        // Create transformation matrix
        Matrix4x4 playerMatrix = Matrix4x4.TRS(playerPosition, playerRotation, playerScale);
        matrices.Add(playerMatrix);
        colliderIds.Add(playerID);
        
        // Update the matrix in collision manager
        CollisionManager.Instance.UpdateMatrix(playerID, playerMatrix);
    }
    
    void CreateGround()
    {
        // Create a large ground plane
        Vector3 groundPosition = new Vector3(0, groundY, constantZPosition);
        Vector3 groundScale = new Vector3(groundWidth, 1f, groundDepth);
        Quaternion groundRotation = Quaternion.identity;
        
        // Register with collision system - use actual dimensions
        int groundID = CollisionManager.Instance.RegisterCollider(
            groundPosition, 
            new Vector3(groundWidth, 1f, groundDepth), 
            false);
        
        // Create transformation matrix
        Matrix4x4 groundMatrix = Matrix4x4.TRS(groundPosition, groundRotation, groundScale);
        matrices.Add(groundMatrix);
        colliderIds.Add(groundID);
        
        // Update the matrix in collision manager
        CollisionManager.Instance.UpdateMatrix(groundID, groundMatrix);
    }
    
    void GenerateRandomBoxes()
    {
        // Create random boxes (excluding player and ground)
        for (int i = 0; i < instanceCount - 2; i++)
        {
            // Random position (constant Z)
            Vector3 position = new Vector3(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY),
                constantZPosition
            );
            
            // Random rotation only around Z axis
            Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            
            // Random non-uniform scale - different for each dimension
            Vector3 scale = new Vector3(
                Random.Range(0.5f, 3f),
                Random.Range(0.5f, 3f),
                Random.Range(0.5f, 3f)
            );
            
            // Register with collision system - properly handle rectangular shapes
            int id = CollisionManager.Instance.RegisterCollider(
                position, 
                new Vector3(width * scale.x, height * scale.y, depth * scale.z), 
                false);
            
            // Create transformation matrix
            Matrix4x4 boxMatrix = Matrix4x4.TRS(position, rotation, scale);
            matrices.Add(boxMatrix);
            colliderIds.Add(id);
            
            // Update the matrix in collision manager
            CollisionManager.Instance.UpdateMatrix(id, boxMatrix);
        }
    }

    void SpawnEnemies()
    {
        int enemyCount = 10;

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 enemyPos = new Vector3(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY),
                constantZPosition
            );

            Vector3 enemyScale = new Vector3(1f, 1f, 1f);
            Quaternion enemyRot = Quaternion.identity;

            Matrix4x4 enemyMatrix = Matrix4x4.TRS(enemyPos, enemyRot, enemyScale);

            int id = CollisionManager.Instance.RegisterCollider(
                enemyPos,
                new Vector3(width * enemyScale.x, height * enemyScale.y, depth * enemyScale.z),
                false
            );

            matrices.Add(enemyMatrix);
            colliderIds.Add(id);
            enemyColliderIds.Add(id);
            enemyMatrices.Add(enemyMatrix);
            CollisionManager.Instance.UpdateMatrix(id, enemyMatrix);

            Debug.Log($"Spawned enemy {i} at {enemyPos}");
        }
    }

    void Update()
    {
        UpdatePlayer();
        CheckEnemyCollisions();
        RenderBoxes();
    }
    
    void UpdatePlayer()
    {
        if (playerID == -1) return;

        // Get current player matrix
        Matrix4x4 playerMatrix = matrices[colliderIds.IndexOf(playerID)];
        DecomposeMatrix(playerMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);

        // Get horizontal input
        float horizontal = 0;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1;
        if (Input.GetKey(KeyCode.D)) horizontal += 1;

        // Check jump input
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            // Fast upward jump
            playerVelocity.y = 12f; // high speed upward
        }

        // Gravity
        if (!isGrounded)
        {
            // Slow falling
            playerVelocity.y -= gravity * 0.5f * Time.deltaTime;
        }

        // Horizontal speed is reduced when in air
        float appliedSpeed = isGrounded ? movementSpeed : movementSpeed * 0.5f;

        // Apply horizontal movement
        Vector3 newPos = pos;
        newPos.x += horizontal * appliedSpeed * Time.deltaTime;

        if (!CheckCollisionAt(playerID, new Vector3(newPos.x, pos.y, pos.z)))
        {
            pos.x = newPos.x;
        }

        // Apply vertical movement
        newPos = pos;
        newPos.y += playerVelocity.y * Time.deltaTime;

        if (CheckCollisionAt(playerID, new Vector3(pos.x, newPos.y, pos.z)))
        {
            if (playerVelocity.y < 0)
            {
                isGrounded = true;
            }
            playerVelocity.y = 0;
        }
        else
        {
            pos.y = newPos.y;
            isGrounded = false;
        }

        // Update matrix
        Matrix4x4 newMatrix = Matrix4x4.TRS(pos, rot, scale);
        matrices[colliderIds.IndexOf(playerID)] = newMatrix;
        CollisionManager.Instance.UpdateCollider(playerID, pos, new Vector3(width * scale.x, height * scale.y, depth * scale.z));
        CollisionManager.Instance.UpdateMatrix(playerID, newMatrix);

        // Update camera
        if (cameraFollow != null)
        {
            cameraFollow.SetPlayerPosition(pos);
        }
    }

    bool CheckCollisionAt(int id, Vector3 position)
    {
        return CollisionManager.Instance.CheckCollision(id, position, out _);
    }

    void CheckEnemyCollisions()
    {
        if (playerID == -1) return;

        // Get player's current matrix
        Matrix4x4 playerMatrix = matrices[colliderIds.IndexOf(playerID)];
        DecomposeMatrix(playerMatrix, out Vector3 playerPos, out _, out _);

        if (CollisionManager.Instance.CheckCollision(playerID, playerPos, out List<int> collidingIds))
        {
            foreach (int id in collidingIds)
            {
                if (enemyColliderIds.Contains(id))
                {
                    Debug.Log("Player hit an enemy!");
                    // Handle enemy collision logic here
                }
            }
        }
    }

    void RenderBoxes()
    {
        // Convert list to array for Graphics.DrawMeshInstanced
        Matrix4x4[] enemyArray = enemyMatrices.ToArray();
        for (int i = 0; i < enemyArray.Length; i += 1023)
        {
            int batchSize = Mathf.Min(1023, enemyArray.Length - i);
            Matrix4x4[] batch = new Matrix4x4[batchSize];
            System.Array.Copy(enemyArray, i, batch, 0, batchSize);
            Graphics.DrawMeshInstanced(cubeMesh, 0, material, batch, batchSize);
        }
    }

    void DecomposeMatrix(Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        position = matrix.GetPosition();
        rotation = matrix.rotation;
        scale = matrix.lossyScale;
    }
    
    // Add a new random box at runtime (can be called from button or other trigger)
    public void AddRandomBox()
    {
        Vector3 position = new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            constantZPosition
        );
        
        Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        
        // Random non-uniform scale - different for each dimension
        Vector3 scale = new Vector3(
            Random.Range(0.5f, 3f),
            Random.Range(0.5f, 3f),
            Random.Range(0.5f, 3f)
        );
        
        // Register with collision system - properly handle rectangular shapes
        int id = CollisionManager.Instance.RegisterCollider(
            position, 
            new Vector3(width * scale.x, height * scale.y, depth * scale.z), 
            false);
        
        Matrix4x4 boxMatrix = Matrix4x4.TRS(position, rotation, scale);
        matrices.Add(boxMatrix);
        colliderIds.Add(id);
        
        CollisionManager.Instance.UpdateMatrix(id, boxMatrix);
    }
}