using System.Collections.Generic;
using UnityEngine;

public class MatchSystem : MonoBehaviour
    {
        [Header("Match Settings")]
        public int minMatchCount = 3;
        public float matchCheckDelay = 0.1f;
        
        private GridController gridController;
        private BoardState boardState;
        private bool isCheckingMatches = false;
        
        public void Initialize(GridController controller)
        {
            gridController = controller;
            boardState = controller.boardState;
        }
        
        public void CheckMatches()
        {
            if (isCheckingMatches) return;
            
            StartCoroutine(CheckMatchesCoroutine());
        }
        
        private System.Collections.IEnumerator CheckMatchesCoroutine()
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
                        // Check if we have a match
                        if (currentCount >= minMatchCount)
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
                
                // Check last potential match in row
                if (currentCount >= minMatchCount)
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
                        // Check if we have a match
                        if (currentCount >= minMatchCount)
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
                
                // Check last potential match in column
                if (currentCount >= minMatchCount)
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
            // Check for tower first
            if (boardState.towers.ContainsKey(position))
            {
                return boardState.towers[position].towerId;
            }
            
            // Check for resource
            if (boardState.resources.ContainsKey(position))
            {
                return boardState.resources[position].resourceId;
            }
            
            return "";
        }
        
        private void ProcessMatch(MatchData match)
        {
            // Publish match event
            EventBus.Instance.Publish(new MatchFoundEvent
            {
                MatchedPositions = match.positions,
                TileType = GetTileDefinition(match.tileType),
                MatchCount = match.matchCount
            });
            
            // Process match result based on tile type
            TileDefinition tileDef = GetTileDefinition(match.tileType);
            if (tileDef != null)
            {
                switch (tileDef.matchResult)
                {
                    case MatchResult.BuildTower:
                        ProcessResourceMatch(match);
                        break;
                        
                    case MatchResult.UpgradeTower:
                        ProcessTowerMatch(match);
                        break;
                        
                    case MatchResult.BonusEffect:
                        ProcessBonusMatch(match);
                        break;
                        
                    case MatchResult.ResourceGain:
                        ProcessResourceGain(match);
                        break;
                }
            }
            
            // Remove matched tiles
            RemoveMatchedTiles(match.positions);
        }
        
        private void ProcessResourceMatch(MatchData match)
        {
            // 3 одинаковых ресурса = постройка башни
            Vector2Int centerPosition = GetCenterPosition(match.positions);
            
            if (!boardState.IsValidPosition(centerPosition) || 
                boardState.GetTile(centerPosition).hasTower)
                return;
                
            // Create tower at center position
            TowerDefinition towerDef = GetTowerDefinitionForResource(match.tileType);
            if (towerDef != null)
            {
                gridController.CreateTower(centerPosition, towerDef);
            }
        }
        
        private void ProcessTowerMatch(MatchData match)
        {
            // 3 одинаковых башни = улучшенная башня
            Vector2Int centerPosition = GetCenterPosition(match.positions);
            
            if (!boardState.towers.ContainsKey(centerPosition))
                return;
                
            TowerData currentTower = boardState.towers[centerPosition];
            TowerDefinition nextLevelTower = GetNextLevelTower(currentTower.towerId);
            
            if (nextLevelTower != null)
            {
                // Upgrade tower
                boardState.RemoveTower(centerPosition);
                gridController.CreateTower(centerPosition, nextLevelTower);
                
                // Publish upgrade event
                EventBus.Instance.Publish(new TowerUpgradedEvent
                {
                    Position = centerPosition,
                    OldTower = GetTowerDefinition(currentTower.towerId),
                    NewTower = nextLevelTower
                });
            }
        }
        
        private void ProcessBonusMatch(MatchData match)
        {
            // Bonus effect - could be extra gold, special power, etc.
            GameManager.Instance.EconomyManager.AddGold(match.matchCount * 10);
        }
        
        private void ProcessResourceGain(MatchData match)
        {
            // Resource gain - could be extra resources, etc.
            GameManager.Instance.EconomyManager.AddGold(match.matchCount * 5);
        }
        
        private void RemoveMatchedTiles(List<Vector2Int> positions)
        {
            foreach (Vector2Int position in positions)
            {
                // Remove from board state
                boardState.RemoveTower(position);
                boardState.RemoveResource(position);
                
                // Remove visual objects
                GameObject tileObj = gridController.GetTileObjectAt(position);
                if (tileObj != null)
                {
                    Destroy(tileObj);
                }
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
        
        private TowerDefinition GetTowerDefinitionForResource(string resourceId)
        {
            // This would map resources to towers
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