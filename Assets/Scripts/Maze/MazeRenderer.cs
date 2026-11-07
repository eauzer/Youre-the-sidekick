using System.Collections;
using UnityEngine;

public class MazeRenderer : MonoBehaviour
{
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField, Min(0.01f)] private float cellSize = 1f;
    [SerializeField] private MazeGenerator mazeGenerator;
    [SerializeField] private Transform generatedMazeParent;
    [SerializeField] private Color startColor = new Color(0.25f, 0.85f, 0.55f);
    [SerializeField] private Color exitColor = new Color(1f, 0.7f, 0.2f);
    [SerializeField] private int floorSortingOrder;
    [SerializeField] private int wallSortingOrder = 1;

    private Sprite fallbackSprite;

    private IEnumerator Start()
    {
        yield return null;

        if (!TryGetMazeGenerator())
        {
            yield break;
        }

        if (mazeGenerator.Width == 0 || mazeGenerator.Height == 0)
        {
            mazeGenerator.GenerateMaze();
        }

        RenderMaze();
    }

    private void OnDestroy()
    {
        if (fallbackSprite != null)
        {
            Destroy(fallbackSprite.texture);
            Destroy(fallbackSprite);
        }
    }

    public void RegenerateMaze()
    {
        if (TryGetMazeGenerator())
        {
            mazeGenerator.GenerateMaze();
            RenderMaze();
        }
    }

    public void RenderMaze()
    {
        if (!TryGetMazeGenerator())
        {
            return;
        }

        if (mazeGenerator.Width == 0 || mazeGenerator.Height == 0)
        {
            Debug.LogWarning("Maze data is empty. Generate the maze before rendering it.", this);
            return;
        }

        if (!mazeGenerator.IsWalkable(mazeGenerator.StartPosition)
            || !mazeGenerator.IsWalkable(mazeGenerator.ExitPosition))
        {
            Debug.LogError("The maze start and exit positions must both be walkable.", this);
            return;
        }

        EnsureGeneratedParent();
        ClearGeneratedObjects();

        float spacing = Mathf.Max(0.01f, mazeGenerator.CellSize);
        if (!Mathf.Approximately(cellSize, spacing))
        {
            Debug.LogWarning(
            "MazeRenderer Cell Size differs from MazeGenerator Cell Size. Rendering uses the generator's cell size so tiles align with GridToWorld.",
                this);
        }

        for (int x = 0; x < mazeGenerator.Width; x++)
        {
            for (int y = 0; y < mazeGenerator.Height; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                bool walkable = mazeGenerator.WalkableGrid[x, y];
                GameObject prefab = walkable ? floorPrefab : wallPrefab;
                Vector3 worldPosition = mazeGenerator.GridToWorld(gridPosition);

                GameObject tile = prefab != null
                    ? Instantiate(prefab, worldPosition, Quaternion.identity, generatedMazeParent)
                    : CreateFallbackTile(walkable, worldPosition);

                tile.name = (walkable ? "Floor_" : "Wall_") + x + "_" + y;
                FitTileToCell(tile, spacing, worldPosition);
                ConfigureSpriteRenderers(tile, walkable, worldPosition.z);

                if (!walkable)
                {
                    ConfigureWallCollider(tile);
                }

                if (gridPosition == mazeGenerator.StartPosition)
                {
                    TintTile(tile, startColor);
                }
                else if (gridPosition == mazeGenerator.ExitPosition)
                {
                    TintTile(tile, exitColor);
                }
            }
        }

        Physics2D.SyncTransforms();
        ValidateGeneratedColliders();
    }

    private bool TryGetMazeGenerator()
    {
        if (mazeGenerator == null)
        {
            mazeGenerator = GetComponent<MazeGenerator>();
        }

        if (mazeGenerator != null)
        {
            return true;
        }

        Debug.LogError("MazeRenderer requires a MazeGenerator reference or component on the same GameObject.", this);
        return false;
    }

    private void EnsureGeneratedParent()
    {
        if (generatedMazeParent != null)
        {
            return;
        }

        GameObject parentObject = new GameObject("Generated Maze");
        generatedMazeParent = parentObject.transform;
        generatedMazeParent.SetParent(transform, false);
        generatedMazeParent.localPosition = Vector3.zero;
        generatedMazeParent.localRotation = Quaternion.identity;
        generatedMazeParent.localScale = Vector3.one;
    }

    private void ClearGeneratedObjects()
    {
        for (int i = generatedMazeParent.childCount - 1; i >= 0; i--)
        {
            Transform child = generatedMazeParent.GetChild(i);

            if (Application.isPlaying)
            {
                foreach (Collider2D collider in child.GetComponentsInChildren<Collider2D>(true))
                {
                    collider.enabled = false;
                }

                child.gameObject.SetActive(false);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private GameObject CreateFallbackTile(bool walkable, Vector3 worldPosition)
    {
        GameObject tile = new GameObject(walkable ? "Floor" : "Wall");
        tile.transform.SetParent(generatedMazeParent, true);
        tile.transform.position = worldPosition;

        SpriteRenderer spriteRenderer = tile.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetFallbackSprite();
        spriteRenderer.color = walkable ? Color.white : Color.black;
        spriteRenderer.sortingOrder = walkable ? 0 : 1;
        return tile;
    }

    private Sprite GetFallbackSprite()
    {
        if (fallbackSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "Maze Fallback Tile Texture";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            fallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            fallbackSprite.name = "Maze Fallback Tile";
        }

        return fallbackSprite;
    }

    private static void FitTileToCell(GameObject tile, float size, Vector3 cellCenter)
    {
        SpriteRenderer[] spriteRenderers = tile.GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers.Length == 0)
        {
            return;
        }

        Bounds bounds = spriteRenderers[0].bounds;
        for (int i = 1; i < spriteRenderers.Length; i++)
        {
            bounds.Encapsulate(spriteRenderers[i].bounds);
        }

        if (bounds.size.x <= 0f || bounds.size.y <= 0f)
        {
            return;
        }

        tile.transform.localScale = new Vector3(
            tile.transform.localScale.x * size / bounds.size.x,
            tile.transform.localScale.y * size / bounds.size.y,
            tile.transform.localScale.z);

        Bounds fittedBounds = spriteRenderers[0].bounds;
        for (int i = 1; i < spriteRenderers.Length; i++)
        {
            fittedBounds.Encapsulate(spriteRenderers[i].bounds);
        }

        tile.transform.position += cellCenter - fittedBounds.center;
        tile.transform.position = new Vector3(
            tile.transform.position.x,
            tile.transform.position.y,
            cellCenter.z);
    }

    private void ConfigureSpriteRenderers(GameObject tile, bool walkable, float zPosition)
    {
        SpriteRenderer[] spriteRenderers = tile.GetComponentsInChildren<SpriteRenderer>(true);
        int sortingOrder = walkable ? floorSortingOrder : wallSortingOrder;

        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            spriteRenderer.sortingOrder = sortingOrder;
            Vector3 spritePosition = spriteRenderer.transform.position;
            spritePosition.z = zPosition;
            spriteRenderer.transform.position = spritePosition;
        }

        if (spriteRenderers.Length == 0)
        {
            Debug.LogWarning("Maze tile prefab has no SpriteRenderer and will not render as a 2D tile.", tile);
        }
    }

    private static void ConfigureWallCollider(GameObject wall)
    {
        SpriteRenderer[] spriteRenderers = wall.GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers.Length == 0)
        {
            Debug.LogError("Wall prefab has no SpriteRenderer to size its collider from.", wall);
            return;
        }

        Bounds spriteBounds = GetCombinedSpriteBounds(spriteRenderers);
        BoxCollider2D[] existingColliders = wall.GetComponentsInChildren<BoxCollider2D>(true);
        BoxCollider2D wallCollider = wall.GetComponent<BoxCollider2D>();

        if (wallCollider == null)
        {
            wallCollider = wall.AddComponent<BoxCollider2D>();
        }

        foreach (BoxCollider2D existingCollider in existingColliders)
        {
            if (existingCollider != wallCollider)
            {
                DestroyImmediate(existingCollider);
            }
        }

        Vector3 lossyScale = wall.transform.lossyScale;
        Vector3 localCenter = wall.transform.InverseTransformPoint(spriteBounds.center);
        wallCollider.offset = new Vector2(localCenter.x, localCenter.y);
        wallCollider.size = new Vector2(
            spriteBounds.size.x / Mathf.Max(Mathf.Abs(lossyScale.x), 0.0001f),
            spriteBounds.size.y / Mathf.Max(Mathf.Abs(lossyScale.y), 0.0001f));
        wallCollider.isTrigger = false;
        wallCollider.enabled = true;
    }

    public bool ValidateGeneratedColliders()
    {
        if (generatedMazeParent == null)
        {
            Debug.LogError("Generated maze parent is not assigned.", this);
            return false;
        }

        bool isValid = true;
        int wallCount = 0;
        int floorCount = 0;

        for (int i = 0; i < generatedMazeParent.childCount; i++)
        {
            GameObject tile = generatedMazeParent.GetChild(i).gameObject;
            BoxCollider2D[] colliders = tile.GetComponentsInChildren<BoxCollider2D>(true);

            if (tile.name.StartsWith("Wall_"))
            {
                wallCount++;
                if (colliders.Length != 1)
                {
                    Debug.LogError("Generated wall must have exactly one BoxCollider2D.", tile);
                    isValid = false;
                    continue;
                }

                if (colliders[0].isTrigger)
                {
                    Debug.LogError("Generated wall BoxCollider2D must not be a trigger.", tile);
                    isValid = false;
                }

                SpriteRenderer[] spriteRenderers = tile.GetComponentsInChildren<SpriteRenderer>(true);
                if (spriteRenderers.Length == 0)
                {
                    Debug.LogError("Generated wall has no SpriteRenderer to validate against.", tile);
                    isValid = false;
                    continue;
                }

                Bounds spriteBounds = GetCombinedSpriteBounds(spriteRenderers);
                Bounds colliderBounds = colliders[0].bounds;
                if (!BoundsMatch(spriteBounds, colliderBounds, 0.01f))
                {
                    Debug.LogError("Generated wall collider does not match its visible sprite bounds.", tile);
                    isValid = false;
                }
            }
            else if (tile.name.StartsWith("Floor_"))
            {
                floorCount++;
                if (colliders.Length > 0)
                {
                    Debug.LogError("Generated floor must not have a BoxCollider2D.", tile);
                    isValid = false;
                }
            }
        }

        if (wallCount == 0 || floorCount == 0)
        {
            Debug.LogError("Generated maze validation found no walls or no floors.", this);
            isValid = false;
        }

        if (isValid)
        {
            Debug.Log($"Maze collider validation passed: {wallCount} walls, {floorCount} floors.", this);
        }

        return isValid;
    }

    private static Bounds GetCombinedSpriteBounds(SpriteRenderer[] spriteRenderers)
    {
        Bounds bounds = spriteRenderers[0].bounds;
        for (int i = 1; i < spriteRenderers.Length; i++)
        {
            bounds.Encapsulate(spriteRenderers[i].bounds);
        }

        return bounds;
    }

    private static bool BoundsMatch(Bounds first, Bounds second, float tolerance)
    {
        return (first.center - second.center).sqrMagnitude <= tolerance * tolerance
            && (first.size - second.size).sqrMagnitude <= tolerance * tolerance;
    }

    private static void TintTile(GameObject tile, Color color)
    {
        SpriteRenderer[] spriteRenderers = tile.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            spriteRenderer.color = color;
        }

    }
}
