using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scatterer : LevelGeneratorPart
{
    public SpriteRenderer debugPlane;
    public int pixelsPerUnit;
    public Gradient debugColors;
    public GameObject tempColliderPrefab;
    public GameObject persistentColliderPrefab;
    public List<GameObject> temporaryColliders;
    public List<GameObject> persistingColliders;

    public ScattererObjectModule[] decorationScattererModules;
    public List<ScattererObjectModule> SCATTERER_MODULES = new();
    readonly static RandomSet<Vector2Int> allTiles = new();

    public int debugModule;

    public override void Init()
    {
        for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
        {
            for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
            {
                allTiles.Add(new(x, y));
            }
        }
        SCATTERER_MODULES.AddRange(decorationScattererModules);
        StartCoroutine(Scatter());
    }

    IEnumerator Scatter()
    {
        while (debugModule >= 0 && debugModule < SCATTERER_MODULES.Count)
        {
            DisplayField(SCATTERER_MODULES[debugModule]);
            yield return null;
        }
        foreach (var m in SCATTERER_MODULES)
        {
            if (m.enabled)
            {
                DisplayField(m);
                yield return null;
                ScatterModels(m);
                yield return null;
            }
        }
        foreach (GameObject c in persistingColliders)
        {
            Destroy(c);
        }
        debugPlane.enabled = false;
        stopped = true;
    }

    void DisplayField(ScattererObjectModule m)
    {
        Vector2Int texSize = WorldUtils.WORLD_SIZE * pixelsPerUnit;
        Color32[] cols = new Color32[texSize.x * texSize.y];
        for (int x = 0; x < texSize.x; x++)
        {
            for (int y = 0; y < texSize.y; y++)
            {
                int i = x + y * texSize.x;
                Vector2 tilePos = new Vector2(x + 0.5f, y + 0.5f) / pixelsPerUnit - Vector2.one * 0.5f;
                float e = m.EvaluateAt(tilePos);
                Color32 c = debugColors.Evaluate(1 / (1 + Mathf.Exp(-e)));
                cols[i] = c;
            }
        }
        Texture2D tex = new(texSize.x, texSize.y)
        {
            filterMode = FilterMode.Point
        };
        tex.SetPixels32(cols);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(Vector2.zero, texSize), Vector2.one * 0.5f, pixelsPerUnit);
        debugPlane.sprite = sp;
    }

    void ScatterModels(ScattererObjectModule m)
    {
        RandomSet<Vector2Int> tilesLeft;
        if (m.validTiles == null)
        {
            tilesLeft = new(allTiles);
        }
        else
        {
            tilesLeft = new();
            for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
            {
                for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
                {
                    if (m.validTiles[x, y])
                        tilesLeft.Add(new(x, y));
                }
            }
        }
        while (tilesLeft.Count > 0)
        {
            Vector2Int pos = tilesLeft.PopRandom();
            for (int i = 0; i < m.triesPerTile; i++)
            {
                Vector2 p = pos + Vector2.up * Random.Range(-0.6f, 0.6f) + Vector2.right * Random.Range(-0.6f, 0.6f);
                if (p.x > -0.5f && p.y > -0.5f && p.x < WorldUtils.WORLD_SIZE.x - 0.5f && p.y < WorldUtils.WORLD_SIZE.y - 0.5f)
                {
                    float e = m.EvaluateAt(p);
                    if (e >= m.minValue && e != float.NegativeInfinity)
                    {
                        float r = m.GetScaled(m.placementRadius, m.radiusGain, e);
                        if (Physics2D.CircleCast(p, r, Vector2.zero, 0, 1 << 7).collider == null)
                        {
                            float s = m.GetScaled(1, m.sizeGain, e);
                            Vector3 rayOrigin = WorldUtils.TileToWorldPos(p) + (WorldUtils.MAX_HEIGHT + 1) * WorldUtils.HEIGHT_STEP * Vector3.up;
                            Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayOrigin.y + 1, LayerMask.GetMask("CoarseTerrain"));
                            GameObject g = Instantiate(m.prefab, hit.point, Quaternion.Euler(Vector3.up * Random.Range(0f, 360f) + Random.onUnitSphere * m.angleSpread), transform);
                            g.transform.localScale *= s;
                            if (r > 0)
                            {
                                GameObject tc = Instantiate(tempColliderPrefab, p, Quaternion.identity, transform);
                                temporaryColliders.Add(tc);
                                tc.GetComponent<CircleCollider2D>().radius = r;
                            }
                            if (m.persistingRadius > 0)
                            {
                                GameObject pc = Instantiate(persistentColliderPrefab, p, Quaternion.identity, transform);
                                persistingColliders.Add(pc);
                                pc.GetComponent<CircleCollider2D>().radius = m.persistingRadius * s;
                            }
                        }
                    }
                }
            }
        }
        foreach (GameObject c in temporaryColliders)
        {
            Destroy(c);
        }
        foreach (GameObject c in persistingColliders)
        {
            if (c.layer == 0)
                c.layer = 7;
        }
    }
}
