using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class MatchSystem : MonoBehaviour
    {
        [Header("Match Settings")]
        public int minMatchCount = 3;
        public float matchCheckDelay = 0.1f;
        
        [Header("Rewards")]
        // Stub: additional swaps granted for matches longer than 3
        public int swapPlus = 0;
        
        private GridController gridController;
        private BoardState boardState;
        private bool isCheckingMatches = false;
        
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
            
            // Subscribe to fill completion to trigger cascade checks
            if (gridController.gridFiller != null)
            {
                gridController.gridFiller.OnFillCompleted += OnFillCompleted;
            }
        }

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
            
            // Process all matches
            List<MatchData> allMatches = new List<MatchData>();
            allMatches.AddRange(horizontalMatches);
            allMatches.AddRange(verticalMatches);
            
            foreach (MatchData match in allMatches)
            {
                ProcessMatch(match);
            }
            
            // Check for cascading matches
            if (allMatches.Count > 0)
            {
                yield return new WaitForSeconds(matchCheckDelay);
                CheckMatches(); // Recursive check for cascading
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
            
            // New rules:
            // - 3 ресурсов одного типа -> создаём башню producedTower в центре матча
            // - 3 башни одного уровня и типа -> создаём башню nextLevel в центре
            var anyPos = match.positions[0];
            var cell = boardState.GetTile(anyPos);
            if (cell == null) { RemoveMatchedTiles(match.positions); return; }
            if (cell.category == TileCategory.Resource)
            {
                ProcessResourceMatch(match);
            }
            else if (cell.category == TileCategory.Tower)
            {
                ProcessTowerMatch(match);
            }
            
            // Replace center with a tower if resource match; otherwise just remove matched
            // Remove only the matched tiles; tiles above will fall via filler
            RemoveMatchedTiles(match.positions);
        }
        
        private void ProcessResourceMatch(MatchData match)
        {
            // 3 одинаковых ресурса = постройка башни
            Vector2Int centerPosition = GetCenterPosition(match.positions);
            
            if (!boardState.IsValidPosition(centerPosition) || boardState.GetTile(centerPosition) != null)
                return;
                
            // Create tower at center position from resource tile definition mapping
            TileDefinition resDef = GetTileDefinition(match.tileType);
            if (resDef != null && resDef.producedTower != null)
            {
                gridController.CreateTower(centerPosition, resDef.producedTower);
            }
        }
        
        private void ProcessTowerMatch(MatchData match)
        {
            // 3 одинаковых башни = улучшенная башня
            Vector2Int centerPosition = GetCenterPosition(match.positions);
            
            var cell = boardState.GetTile(centerPosition);
            if (cell == null || cell.category != TileCategory.Tower)
                return;
            
            TowerDefinition nextLevelTower = GetNextLevelTower(cell.tileId);
            
            if (nextLevelTower != null)
            {
                // Upgrade tower
                boardState.RemoveTower(centerPosition);
                gridController.CreateTower(centerPosition, nextLevelTower);
                
                // Publish upgrade event
                if (EventBus.Instance != null)
                {
                    EventBus.Instance.Publish(new TowerUpgradedEvent
                    {
                        Position = centerPosition,
                        OldTower = GetTowerDefinition(cell.tileId),
                        NewTower = nextLevelTower
                    });
                }
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
            // Remove only these positions
            foreach (Vector2Int position in positions)
            {
                boardState.RemoveTower(position);
                boardState.RemoveResource(position);
                gridController.RemoveTile(position);
            }
            
            // Let gravity collapse and fill
            gridController.FillPositionsAfterRemoval(positions);
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
            // This would load from ScriptableObjects
            // For now, return null
            return null;
        }
        
        private TowerDefinition GetTowerDefinition(string towerId)
        {
            // This would load from ScriptableObjects
            // For now, return null
            return null;
        }
        
        private TowerDefinition GetNextLevelTower(string currentTowerId)
        {
            // This would get the next level tower
            // For now, return null
            return null;
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