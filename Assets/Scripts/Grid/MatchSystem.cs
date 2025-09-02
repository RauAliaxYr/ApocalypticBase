using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class MatchSystem : MonoBehaviour
    {
        [Header("Match Settings")]
        public int minMatchCount = 3;
        public float matchCheckDelay = 0.1f;
        [Tooltip("Duration of tiles moving toward the spawn position on match")] 
        public float matchMoveDuration = 0.2f;
        public AnimationCurve matchMoveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        private Dictionary<string, TileDefinition> tileIdToDef = new Dictionary<string, TileDefinition>();
        // towerIdToDef removed - using evolution-based system
        
        [Header("Rewards")]
        // Stub: additional swaps granted for matches longer than 3
        public int swapPlus = 0;
        
        private GridController gridController;
        private BoardState boardState;
        private bool isCheckingMatches = false;
        // Collect positions removed during current match-processing pass
        private List<Vector2Int> removedPositionsBuffer = new List<Vector2Int>();
        
        public void Initialize(GridController controller)
        {
            if (controller == null)
            {
                Debug.LogError("MatchSystem: GridController is null!");
                return;
            }
            
            gridController = controller;
            boardState = controller.boardState;
            
            if (boardState == null)
            {
                Debug.LogError("MatchSystem: BoardState is null!");
                return;
            }
            
            // Build definition caches
            BuildDefinitionCaches();
            
            // Subscribe to fill completion to trigger cascade checks
            if (gridController.gridFiller != null)
            {
                gridController.gridFiller.OnFillCompleted += OnFillCompleted;
            }
        }

        private void BuildDefinitionCaches()
        {
            tileIdToDef.Clear();
            
            // From GridController resource prefabs
            if (gridController != null && gridController.allowedResourcePrefabs != null)
            {
                foreach (var prefab in gridController.allowedResourcePrefabs)
                {
                    if (prefab == null) continue;
                    var res = prefab.GetComponent<Resource>();
                    if (res == null || res.definition == null) continue;
                    var tileDef = res.definition;
                    // Index by resourceId and towerIds
                    if (!string.IsNullOrEmpty(tileDef.resourceId)) tileIdToDef[tileDef.resourceId] = tileDef;
                    if (tileDef.towerIds != null)
                    {
                        foreach (var tid in tileDef.towerIds)
                        {
                            if (!string.IsNullOrEmpty(tid)) tileIdToDef[tid] = tileDef;
                        }
                    }
                    // capture produced tower and its upgrade chain
                    // (legacy noop)
                }
            }

            // Fallback: load from Resources to ensure lookups work even if not linked via prefabs
            if (tileIdToDef.Count == 0)
            {
                var loadedTiles = Resources.LoadAll<TileDefinition>("");
                foreach (var def in loadedTiles)
                {
                    if (def == null) continue;
                    if (!string.IsNullOrEmpty(def.resourceId)) tileIdToDef[def.resourceId] = def;
                    if (def.towerIds != null)
                    {
                        foreach (var tid in def.towerIds)
                        {
                            if (!string.IsNullOrEmpty(tid)) tileIdToDef[tid] = def;
                        }
                    }
                }
            }
            // Evolution-based system uses TileDefinition for both resources and towers
        }

        // RegisterTowerChain removed - using evolution-based system

        private void OnFillCompleted(GridFiller.FillReason reason)
        {
            // Always check matches after any fill (supports cascades and initial matches)
            CheckMatches();
        }
        
        public void CheckMatches()
        {
            if (isCheckingMatches) return;
            
            StartCoroutine(CheckMatchesCoroutine());
        }
        
        private IEnumerator CheckMatchesCoroutine()
        {
            isCheckingMatches = true;
            
            yield return new WaitForSeconds(matchCheckDelay);
            
            // Check horizontal matches
            List<MatchData> horizontalMatches = FindHorizontalMatches();
            
            // Check vertical matches
            List<MatchData> verticalMatches = FindVerticalMatches();
            
            // Combine all matches
            List<MatchData> allMatches = new List<MatchData>();
            allMatches.AddRange(horizontalMatches);
            allMatches.AddRange(verticalMatches);
            
            // Process matches sequentially to avoid conflicts
            if (allMatches.Count > 0)
            {
                yield return ProcessMatchesSequentially(allMatches);
            }
            
            isCheckingMatches = false;
        }
        
        private List<MatchData> FindHorizontalMatches()
        {
            List<MatchData> matches = new List<MatchData>();
            
            for (int y = 0; y <boardState.height; y++)
            {
                int matchStart = 0;
                string currentType = "";
                int currentCount = 0;
                
                for (int x = 0; x < boardState.width; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    string tileType = GetTileTypeAt(position);
                    
                    if (tileType == currentType && !string.IsNullOrEmpty(tileType))
                    {
                        currentCount++;
                    }
                    else
                    {
                        // Check if we have a match (non-empty)
                        if (currentCount >= minMatchCount && !string.IsNullOrEmpty(currentType))
                        {
                            matches.Add(new MatchData
                            {
                                positions = GetPositionsInRange(matchStart, x - 1, y, true),
                                tileType = currentType,
                                matchCount = currentCount,
                                isHorizontal = true
                            });
                        }
                        
                        // Start new potential match
                        matchStart = x;
                        currentType = tileType;
                        currentCount = 1;
                    }
                }
                
                // Check last potential match in row (non-empty)
                if (currentCount >= minMatchCount && !string.IsNullOrEmpty(currentType))
                {
                    matches.Add(new MatchData
                    {
                        positions = GetPositionsInRange(matchStart, boardState.width - 1, y, true),
                        tileType = currentType,
                        matchCount = currentCount,
                        isHorizontal = true
                    });
                }
            }
            
            return matches;
        }
        
        private List<MatchData> FindVerticalMatches()
        {
            List<MatchData> matches = new List<MatchData>();
            
            for (int x = 0; x < boardState.width; x++)
            {
                int matchStart = 0;
                string currentType = "";
                int currentCount = 0;
                
                for (int y = 0; y < boardState.height; y++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    string tileType = GetTileTypeAt(position);
                    
                    if (tileType == currentType && !string.IsNullOrEmpty(tileType))
                    {
                        currentCount++;
                    }
                    else
                    {
                        // Check if we have a match (non-empty)
                        if (currentCount >= minMatchCount && !string.IsNullOrEmpty(currentType))
                        {
                            matches.Add(new MatchData
                            {
                                positions = GetPositionsInRange(matchStart, y - 1, x, false),
                                tileType = currentType,
                                matchCount = currentCount,
                                isHorizontal = false
                            });
                        }
                        
                        // Start new potential match
                        matchStart = y;
                        currentType = tileType;
                        currentCount = 1;
                    }
                }
                
                // Check last potential match in column (non-empty)
                if (currentCount >= minMatchCount && !string.IsNullOrEmpty(currentType))
                {
                    matches.Add(new MatchData
                    {
                        positions = GetPositionsInRange(matchStart, boardState.height - 1, x, false),
                        tileType = currentType,
                        matchCount = currentCount,
                        isHorizontal = false
                    });
                }
            }
            
            return matches;
        }
        
        private List<Vector2Int> GetPositionsInRange(int start, int end, int fixedCoord, bool isHorizontal)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            for (int i = start; i <= end; i++)
            {
                if (isHorizontal)
                {
                    positions.Add(new Vector2Int(i, fixedCoord));
                }
                else
                {
                    positions.Add(new Vector2Int(fixedCoord, i));
                }
            }
            
            return positions;
        }
        
        private string GetTileTypeAt(Vector2Int position)
        {
            var cell = boardState.GetTile(position);
            return cell != null ? cell.tileId : "";
        }
        
        private IEnumerator ProcessMatchesSequentially(List<MatchData> matches)
        {
            // Prepare buffer for all removed positions across matches
            removedPositionsBuffer.Clear();

            // Process each match one by one
            foreach (MatchData match in matches)
            {
                yield return ProcessMatchCoroutine(match);
            }
            
            // After all matches are processed, trigger collapse and then new fill for removed positions
            if (gridController != null && gridController.gridFiller != null)
            {
                if (removedPositionsBuffer.Count > 0)
                {
                    // Collapse existing tiles into gaps, then spawn new tiles for remaining empties
                    gridController.FillPositionsAfterRemoval(removedPositionsBuffer);
                }
            }
        }

        private void ProcessMatch(MatchData match)
        {
            // Publish match event
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Publish(new MatchFoundEvent
                {
                    MatchedPositions = match.positions,
                    TileId = match.tileType,
                    MatchCount = match.matchCount
                });
            }
            
            // Reward: for every tile beyond 3 in a match, add +1 to swapPlus
            if (match.matchCount > 3)
            {
                swapPlus += (match.matchCount - 3);
            }
            
            // Unified flow: choose one result position, then evolve tile at that position
            Vector2Int resultPos = ChooseResultPosition(match);
            ProcessUnifiedMatch(match, resultPos);
            
            List<Vector2Int> toRemove = new List<Vector2Int>(match.positions);
            toRemove.Remove(resultPos);
            
            RemoveMatchedTiles(toRemove);
            // Track removed positions for a single collapse-and-fill after all matches
            if (toRemove != null && toRemove.Count > 0)
            {
                removedPositionsBuffer.AddRange(toRemove);
            }
            
            // Clear last swap markers after processing
            gridController.ClearLastSwap();
        }

        private IEnumerator ProcessMatchCoroutine(MatchData match)
        {
            // Determine unified spawn/result position
            Vector2Int spawnPos = ChooseResultPosition(match);

            // Animate all matched tiles moving toward the spawn position
            if (gridController != null)
            {
                yield return AnimateTilesToTarget(match.positions, gridController.GridToWorldPosition(spawnPos));
            }

            // After animation completes, run the existing processing (transform/upgrade and removals)
            ProcessMatch(match);
        }

        private IEnumerator AnimateTilesToTarget(List<Vector2Int> positions, Vector3 targetWorld)
        {
            // Collect transforms and start positions
            List<Transform> transforms = new List<Transform>(positions.Count);
            List<Vector3> startPositions = new List<Vector3>(positions.Count);

            foreach (var pos in positions)
            {
                var tile = gridController != null ? gridController.GetTileAt(pos) : null;
                if (tile != null)
                {
                    transforms.Add(tile.transform);
                    startPositions.Add(tile.transform.position);
                }
            }

            if (transforms.Count == 0)
            {
                yield break;
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, matchMoveDuration);

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = matchMoveCurve != null ? matchMoveCurve.Evaluate(t) : t;

                for (int i = 0; i < transforms.Count; i++)
                {
                    var tr = transforms[i];
                    if (tr == null) continue; // Was destroyed mid-animation
                    tr.position = Vector3.Lerp(startPositions[i], targetWorld, eased);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Snap to target (in case of tiny drift)
            for (int i = 0; i < transforms.Count; i++)
            {
                var tr = transforms[i];
                if (tr == null) continue;
                tr.position = targetWorld;
            }
        }
        
        private void ProcessResourceMatch(MatchData match, Vector2Int spawnPos)
        {
            // 3 одинаковых ресурса = превращаем ресурсный префаб в башню 1 уровня
            if (!boardState.IsValidPosition(spawnPos)) return;

            TileDefinition resDef = GetTileDefinition(match.tileType);
            if (resDef == null) return;

            // Get existing tile object at spawnPos
            TileBase tile = gridController.GetTileAt(spawnPos);
            if (tile == null) return;
            
            // Disable resource, enable tower on same prefab
            Resource res = tile.GetComponent<Resource>();
            Tower tower = tile.GetComponent<Tower>();
            if (res != null) res.enabled = false;
            if (tower != null)
            {
                tower.enabled = true;
                tower.ApplyEvolution(resDef, 1);
                // Update board state id to tower level id if provided
                string towerId = (resDef.towerIds != null && resDef.towerIds.Length > 0 && !string.IsNullOrEmpty(resDef.towerIds[0])) ? resDef.towerIds[0] : match.tileType + "_tower1";
                boardState.RemoveResource(spawnPos);
                boardState.AddTower(spawnPos, towerId, 1);
                // Ensure grid mapping now points to the active Tower component
                gridController.AddTileToGrid(spawnPos, tower);
            }
            else
            {
            }
        }
        
        private void ProcessTowerMatch(MatchData match, Vector2Int upgradePos)
        {
            // 3 одинаковых башни = повышаем уровень существующей башни
            // Prefer requested upgrade position, but fallback to any tile in match that is a tower
            Vector2Int centerPosition = upgradePos;
            var cell = boardState.GetTile(centerPosition);
            if (cell == null || cell.category != TileCategory.Tower)
            {
                foreach (var p in match.positions)
                {
                    var c = boardState.GetTile(p);
                    if (c != null && c.category == TileCategory.Tower)
                    {
                        centerPosition = p;
                        cell = c;
                        break;
                    }
                }
                if (cell == null || cell.category != TileCategory.Tower) return;
            }

            TileBase tileObj = gridController.GetTileAt(centerPosition);
            if (tileObj == null) return;
            Tower tower = tileObj.GetComponent<Tower>();
            if (tower == null || tower.evolution == null) return;

            int nextLevel = Mathf.Min(cell.level + 1, tower.evolution.maxTowerLevel);
            if (nextLevel == cell.level) return;
            
            tower.ApplyEvolution(tower.evolution, nextLevel);
            // Update board state id to next level id
            string nextId = (tower.evolution.towerIds != null && tower.evolution.towerIds.Length >= nextLevel) ? tower.evolution.towerIds[nextLevel - 1] : (cell.tileId + "_lvl" + nextLevel);
            boardState.RemoveTower(centerPosition);
            boardState.AddTower(centerPosition, nextId, nextLevel);
            // Keep grid mapping pointing at the same tile (tower still active)
            gridController.AddTileToGrid(centerPosition, tower);
            
            // Publish upgrade event (optional)
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Publish(new TowerUpgradedEvent
                {
                    Position = centerPosition,
                    OldTowerId = cell.tileId,
                    NewTowerId = nextId,
                    NewLevel = nextLevel
                });
            }
        }
        
        private void ProcessBonusMatch(MatchData match)
        {
            // Bonus effect - could be extra gold, special power, etc.
            if (GameManager.Instance != null && GameManager.Instance.EconomyManager != null)
            {
                GameManager.Instance.EconomyManager.AddGold(match.matchCount * 10);
            }
        }
        
        private void ProcessResourceGain(MatchData match)
        {
            // Resource gain - could be extra resources, etc.
            if (GameManager.Instance != null && GameManager.Instance.EconomyManager != null)
            {
                GameManager.Instance.EconomyManager.AddGold(match.matchCount * 5);
            }
        }
        
        private void RemoveMatchedTiles(List<Vector2Int> positions)
        {
            // Remove only these positions (no filling here - will be done after all matches)
            foreach (Vector2Int position in positions)
            {
                boardState.RemoveTower(position);
                boardState.RemoveResource(position);
                gridController.RemoveTile(position);
            }
            
            // Note: Filling is now handled in ProcessMatchesSequentially after all matches are processed
        }

        private Vector2Int ChooseTowerSpawnPosition(MatchData match)
        {
            // If the last swap produced this match, prefer the swapped tile that is in the match
            if (gridController.lastSwapA.HasValue || gridController.lastSwapB.HasValue)
            {
                if (gridController.lastSwapA.HasValue && match.positions.Contains(gridController.lastSwapA.Value))
                {
                    return gridController.lastSwapA.Value;
                }
                if (gridController.lastSwapB.HasValue && match.positions.Contains(gridController.lastSwapB.Value))
                {
                    return gridController.lastSwapB.Value;
                }
            }
            
            // Otherwise pick center; if even-length, choose randomly between the two middle tiles along the match axis
            return GetCenterPositionRandomIfEven(match.positions, match.isHorizontal);
        }
        
        private Vector2Int GetCenterPositionRandomIfEven(List<Vector2Int> positions, bool isHorizontal)
        {
            if (positions == null || positions.Count == 0) return Vector2Int.zero;
            
            // Sort positions along axis
            List<Vector2Int> sorted = new List<Vector2Int>(positions);
            if (isHorizontal)
                sorted.Sort((a, b) => a.x.CompareTo(b.x));
            else
                sorted.Sort((a, b) => a.y.CompareTo(b.y));
            
            int count = sorted.Count;
            if (count % 2 == 1)
            {
                return sorted[count / 2];
            }
            else
            {
                // Even: pick randomly between the two middle ones
                Vector2Int a = sorted[(count / 2) - 1];
                Vector2Int b = sorted[count / 2];
                return (Random.value < 0.5f) ? a : b;
            }
        }
        
        private Vector2Int GetCenterPosition(List<Vector2Int> positions)
        {
            if (positions.Count == 0) return Vector2Int.zero;
            
            Vector2Int sum = Vector2Int.zero;
            foreach (Vector2Int pos in positions)
            {
                sum += pos;
            }
            
            return new Vector2Int(sum.x / positions.Count, sum.y / positions.Count);
        }
        
        private TileDefinition GetTileDefinition(string tileId)
        {
            if (string.IsNullOrEmpty(tileId)) return null;
            if (tileIdToDef.TryGetValue(tileId, out var def)) return def;
            // As a last resort, try reloading once
            BuildDefinitionCaches();
            tileIdToDef.TryGetValue(tileId, out def);
            if (def == null)
            {
                Debug.LogWarning($"MatchSystem: TileDefinition not found for id '{tileId}'.");
            }
            return def;
        }
        
        // GetTowerDefinition and GetNextLevelTower removed - using evolution-based system
        
        private void ProcessUnifiedMatch(MatchData match, Vector2Int resultPos)
        {
            var cell = boardState.GetTile(resultPos);
            if (cell == null) return;
            if (!TryGetNextEvolution(cell.tileId, cell.level, out var def, out string nextId, out TileCategory nextCategory, out int nextLevel))
            {
                return;
            }

            TileBase tileObj = gridController.GetTileAt(resultPos);
            if (tileObj == null) return;
            Resource res = tileObj.GetComponent<Resource>();
            Tower tower = tileObj.GetComponent<Tower>();

            if (nextCategory == TileCategory.Tower)
            {
                if (res != null) res.enabled = false;
                if (tower != null)
                {
                    tower.enabled = true;
                    tower.ApplyEvolution(def, nextLevel);
                    boardState.RemoveResource(resultPos);
                    boardState.RemoveTower(resultPos);
                    boardState.AddTower(resultPos, nextId, nextLevel);
                    gridController.AddTileToGrid(resultPos, tower);
                }
            }
        }

        private bool TryGetNextEvolution(string currentId, int currentLevel, out TileDefinition def, out string nextId, out TileCategory category, out int nextLevel)
        {
            def = GetTileDefinition(currentId);
            nextId = null;
            category = TileCategory.Resource;
            nextLevel = currentLevel;
            if (def == null) return false;

            // Resource -> Tower level 1
            if (!string.IsNullOrEmpty(def.resourceId) && currentId == def.resourceId)
            {
                if (def.towerIds != null && def.towerIds.Length > 0 && !string.IsNullOrEmpty(def.towerIds[0]))
                {
                    nextId = def.towerIds[0];
                    category = TileCategory.Tower;
                    nextLevel = 1;
                    return true;
                }
                return false;
            }

            // Tower level N -> N+1 (by matching id in the chain)
            if (def.towerIds != null)
            {
                for (int i = 0; i < def.towerIds.Length; i++)
                {
                    string tid = def.towerIds[i];
                    if (string.IsNullOrEmpty(tid)) continue;
                    if (tid == currentId)
                    {
                        int currentTowerLevel = i + 1;
                        int desired = currentTowerLevel + 1;
                        if (desired <= def.maxTowerLevel && desired - 1 < def.towerIds.Length && !string.IsNullOrEmpty(def.towerIds[desired - 1]))
                        {
                            nextId = def.towerIds[desired - 1];
                            category = TileCategory.Tower;
                            nextLevel = desired;
                            return true;
                        }
                        return false; // Already at max
                    }
                }
            }
            return false;
        }

        private Vector2Int ChooseResultPosition(MatchData match)
        {
            if (gridController.lastSwapA.HasValue && match.positions.Contains(gridController.lastSwapA.Value))
            {
                return gridController.lastSwapA.Value;
            }
            if (gridController.lastSwapB.HasValue && match.positions.Contains(gridController.lastSwapB.Value))
            {
                return gridController.lastSwapB.Value;
            }
            return GetCenterPositionRandomIfEven(match.positions, match.isHorizontal);
        }
    }
    
    [System.Serializable]
    public class MatchData
    {
        public List<Vector2Int> positions;
        public string tileType;
        public int matchCount;
        public bool isHorizontal;
    }