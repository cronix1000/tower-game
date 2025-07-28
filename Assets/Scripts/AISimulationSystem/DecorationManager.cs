using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace AISimulationSystem
{
    [System.Serializable]
    public class MultiTileFeature
    {
        public string featureName;
        public TileBase centerTile;
        public List<TileOffset> surroundingTiles = new List<TileOffset>();
        public bool canRotate = true;
        public int priority = 1; // Higher priority features placed first
        
        [System.Serializable]
        public class TileOffset
        {
            public Vector2Int offset;
            public TileBase tile;
            public bool isOptional = false; // Can be skipped if space not available
        }
    }

    [System.Serializable]
    public class RoomTheme
    {
        [Header("Theme Identity")]
        public string themeName;
        public ChallengeType challengeType;
        public string themeDescription;
        
        [Header("Single Tile Decorations")]
        public List<TileBase> floorDecorations = new List<TileBase>();
        public List<TileBase> wallDecorations = new List<TileBase>();
        public List<TileBase> overlayEffects = new List<TileBase>();
        
        [Header("Multi-Tile Features")]
        public List<MultiTileFeature> largeFeatures = new List<MultiTileFeature>();
        public List<MultiTileFeature> mediumFeatures = new List<MultiTileFeature>();
        public List<MultiTileFeature> smallFeatures = new List<MultiTileFeature>();
        
        [Header("Placement Rules")]
        [Range(0f, 1f)] public float decorationDensity = 0.3f;
        [Range(0, 3)] public int maxLargeFeatures = 1;
        [Range(0, 5)] public int maxMediumFeatures = 2;
        [Range(0, 8)] public int maxSmallFeatures = 4;
        
        [Header("Theme Colors")]
        public Color ambientTint = Color.white;
        public List<Color> accentColors = new List<Color>();
    }

    public class DecorationManager : MonoBehaviour
    {
        [Header("Tilemap Layers")]
        public Tilemap floorDecorationTilemap;
        public Tilemap wallDecorationTilemap;
        public Tilemap overlayTilemap;
        public Tilemap featureTilemap;
        
        [Header("Available Themes")]
        public List<RoomTheme> availableThemes = new List<RoomTheme>();
        
        [Header("UI References")]
        public UnityEngine.UI.Button randomizeButton;
        public UnityEngine.UI.Dropdown themeDropdown;
        
        private Dictionary<ChallengeType, List<RoomTheme>> themesByChallenge;
        private Dictionary<Room, RoomDecorationState> roomStates = new Dictionary<Room, RoomDecorationState>();
        private Room currentlySelectedRoom;
        private RoomTheme currentTheme;

        [System.Serializable]
        private class RoomDecorationState
        {
            public RoomTheme appliedTheme;
            public List<Vector3Int> decoratedPositions = new List<Vector3Int>();
            public List<MultiTileFeature> placedFeatures = new List<MultiTileFeature>();
            public int randomSeed;
        }

        private void Start()
        {
            InitializeThemes();
            SetupUI();
        }

        private void InitializeThemes()
        {
            themesByChallenge = new Dictionary<ChallengeType, List<RoomTheme>>();
            
            foreach (var theme in availableThemes)
            {
                if (!themesByChallenge.ContainsKey(theme.challengeType))
                {
                    themesByChallenge[theme.challengeType] = new List<RoomTheme>();
                }
                themesByChallenge[theme.challengeType].Add(theme);
            }
        }

        private void SetupUI()
        {
            if (randomizeButton != null)
            {
                randomizeButton.onClick.AddListener(RandomizeCurrentRoom);
            }
            
            if (themeDropdown != null)
            {
                themeDropdown.onValueChanged.AddListener(OnThemeDropdownChanged);
            }
        }

        // Main decoration method called when player assigns challenge to room
        public void OnRoomChallengeAssigned(Room room, ChallengeCard challenge)
        {
            currentlySelectedRoom = room;
            
            // Get available themes for this challenge type
            List<RoomTheme> availableThemesForChallenge = GetThemesForChallenge(challenge.type);
            
            if (availableThemesForChallenge.Count > 0)
            {
                // Start with first theme or previously selected theme
                currentTheme = availableThemesForChallenge[0];
                UpdateThemeDropdown(availableThemesForChallenge);
                ApplyThemeToRoom(room, currentTheme);
            }
        }

        // Player can cycle through themes and randomize
        public void ApplyThemeToRoom(Room room, RoomTheme theme)
        {
            if (room == null || theme == null) return;
            
            // Generate new random seed for this decoration
            int randomSeed = Random.Range(0, 10000);
            Random.InitState(randomSeed);
            
            // Clear existing decorations
            ClearRoomDecorations(room);
            
            // Create new decoration state
            RoomDecorationState state = new RoomDecorationState
            {
                appliedTheme = theme,
                randomSeed = randomSeed
            };
            
            // Apply decorations in order of priority
            ApplyLargeFeatures(room, theme, state);
            ApplyMediumFeatures(room, theme, state);
            ApplySmallFeatures(room, theme, state);
            ApplySingleTileDecorations(room, theme, state);
            ApplyOverlayEffects(room, theme, state);
            
            // Store state
            roomStates[room] = state;
            
            Debug.Log($"Applied theme '{theme.themeName}' to room at {room.center} (Seed: {randomSeed})");
        }

        private void ApplyLargeFeatures(Room room, RoomTheme theme, RoomDecorationState state)
        {
            if (theme.largeFeatures.Count == 0) return;
            
            List<MultiTileFeature> availableFeatures = new List<MultiTileFeature>(theme.largeFeatures);
            availableFeatures = availableFeatures.OrderByDescending(f => f.priority).ToList();
            
            int featuresPlaced = 0;
            while (featuresPlaced < theme.maxLargeFeatures && availableFeatures.Count > 0)
            {
                MultiTileFeature feature = availableFeatures[Random.Range(0, availableFeatures.Count)];
                availableFeatures.Remove(feature);
                
                if (TryPlaceFeature(room, feature, state, FeatureSize.Large))
                {
                    featuresPlaced++;
                }
            }
        }

        private void ApplyMediumFeatures(Room room, RoomTheme theme, RoomDecorationState state)
        {
            if (theme.mediumFeatures.Count == 0) return;
            
            List<MultiTileFeature> availableFeatures = new List<MultiTileFeature>(theme.mediumFeatures);
            
            int featuresPlaced = 0;
            while (featuresPlaced < theme.maxMediumFeatures && availableFeatures.Count > 0)
            {
                MultiTileFeature feature = availableFeatures[Random.Range(0, availableFeatures.Count)];
                availableFeatures.Remove(feature);
                
                if (TryPlaceFeature(room, feature, state, FeatureSize.Medium))
                {
                    featuresPlaced++;
                }
            }
        }

        private void ApplySmallFeatures(Room room, RoomTheme theme, RoomDecorationState state)
        {
            if (theme.smallFeatures.Count == 0) return;
            
            List<MultiTileFeature> availableFeatures = new List<MultiTileFeature>(theme.smallFeatures);
            
            int featuresPlaced = 0;
            while (featuresPlaced < theme.maxSmallFeatures && availableFeatures.Count > 0)
            {
                MultiTileFeature feature = availableFeatures[Random.Range(0, availableFeatures.Count)];
                availableFeatures.Remove(feature);
                
                if (TryPlaceFeature(room, feature, state, FeatureSize.Small))
                {
                    featuresPlaced++;
                }
            }
        }

        private bool TryPlaceFeature(Room room, MultiTileFeature feature, RoomDecorationState state, FeatureSize size)
        {
            // Get potential placement positions based on feature size
            List<Vector2Int> candidatePositions = GetFeaturePlacementPositions(room, size);
            
            // Shuffle positions for randomness
            for (int i = 0; i < candidatePositions.Count; i++)
            {
                Vector2Int temp = candidatePositions[i];
                int randomIndex = Random.Range(i, candidatePositions.Count);
                candidatePositions[i] = candidatePositions[randomIndex];
                candidatePositions[randomIndex] = temp;
            }
            
            // Try to place feature at each candidate position
            foreach (var centerPos in candidatePositions)
            {
                if (CanPlaceFeatureAt(room, feature, centerPos, state))
                {
                    PlaceFeatureAt(centerPos, feature, state);
                    return true;
                }
            }
            
            return false;
        }

        private List<Vector2Int> GetFeaturePlacementPositions(Room room, FeatureSize size)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            switch (size)
            {
                case FeatureSize.Large:
                    // Large features prefer center area
                    positions.Add(room.center);
                    // Add some offset positions near center
                    positions.Add(room.center + Vector2Int.up);
                    positions.Add(room.center + Vector2Int.down);
                    positions.Add(room.center + Vector2Int.left);
                    positions.Add(room.center + Vector2Int.right);
                    break;
                    
                case FeatureSize.Medium:
                    // Medium features can be anywhere but not too close to walls
                    foreach (var pos in room.floorPositions)
                    {
                        if (GetDistanceToNearestWall(pos, room) >= 2)
                        {
                            positions.Add(pos);
                        }
                    }
                    break;
                    
                case FeatureSize.Small:
                    // Small features can be placed anywhere
                    positions.AddRange(room.floorPositions);
                    break;
            }
            
            return positions.Where(pos => room.floorPositions.Contains(pos)).ToList();
        }

        private bool CanPlaceFeatureAt(Room room, MultiTileFeature feature, Vector2Int centerPos, RoomDecorationState state)
        {
            // Check if center position is available
            Vector3Int centerTilePos = new Vector3Int(centerPos.x, centerPos.y, 0);
            if (state.decoratedPositions.Contains(centerTilePos))
                return false;
            
            // Check if all surrounding tiles can be placed
            foreach (var tileOffset in feature.surroundingTiles)
            {
                Vector2Int targetPos = centerPos + tileOffset.offset;
                Vector3Int targetTilePos = new Vector3Int(targetPos.x, targetPos.y, 0);
                
                // Must be within room bounds
                if (!room.floorPositions.Contains(targetPos))
                {
                    if (!tileOffset.isOptional)
                        return false;
                }
                
                // Must not conflict with existing decorations
                if (state.decoratedPositions.Contains(targetTilePos))
                {
                    if (!tileOffset.isOptional)
                        return false;
                }
            }
            
            return true;
        }

        private void PlaceFeatureAt(Vector2Int centerPos, MultiTileFeature feature, RoomDecorationState state)
        {
            // Place center tile
            Vector3Int centerTilePos = new Vector3Int(centerPos.x, centerPos.y, 0);
            featureTilemap.SetTile(centerTilePos, feature.centerTile);
            state.decoratedPositions.Add(centerTilePos);
            
            // Place surrounding tiles
            foreach (var tileOffset in feature.surroundingTiles)
            {
                Vector2Int targetPos = centerPos + tileOffset.offset;
                Vector3Int targetTilePos = new Vector3Int(targetPos.x, targetPos.y, 0);
                
                // Only place if position is valid and not already occupied
                if (currentlySelectedRoom.floorPositions.Contains(targetPos) && 
                    !state.decoratedPositions.Contains(targetTilePos))
                {
                    featureTilemap.SetTile(targetTilePos, tileOffset.tile);
                    state.decoratedPositions.Add(targetTilePos);
                }
            }
            
            state.placedFeatures.Add(feature);
        }

        private void ApplySingleTileDecorations(Room room, RoomTheme theme, RoomDecorationState state)
        {
            // Apply floor decorations
            ApplySingleTileLayer(room, theme.floorDecorations, floorDecorationTilemap, theme.decorationDensity, state);
            
            // Apply wall decorations
            List<Vector2Int> wallPositions = GetAdjacentWallPositions(room);
            ApplySingleTileLayer(wallPositions, theme.wallDecorations, wallDecorationTilemap, theme.decorationDensity * 0.8f, state);
        }

        private void ApplySingleTileLayer(Room room, List<TileBase> tiles, Tilemap tilemap, float density, RoomDecorationState state)
        {
            ApplySingleTileLayer(room.floorPositions.ToList(), tiles, tilemap, density, state);
        }

        private void ApplySingleTileLayer(List<Vector2Int> positions, List<TileBase> tiles, Tilemap tilemap, float density, RoomDecorationState state)
        {
            if (tiles.Count == 0) return;
            
            foreach (var pos in positions)
            {
                Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
                
                // Skip if already decorated
                if (state.decoratedPositions.Contains(tilePos))
                    continue;
                
                if (Random.value < density)
                {
                    TileBase tile = tiles[Random.Range(0, tiles.Count)];
                    tilemap.SetTile(tilePos, tile);
                    state.decoratedPositions.Add(tilePos);
                }
            }
        }

        private void ApplyOverlayEffects(Room room, RoomTheme theme, RoomDecorationState state)
        {
            ApplySingleTileLayer(room, theme.overlayEffects, overlayTilemap, theme.decorationDensity * 0.5f, state);
        }

        // UI Methods
        public void RandomizeCurrentRoom()
        {
            if (currentlySelectedRoom != null && currentTheme != null)
            {
                ApplyThemeToRoom(currentlySelectedRoom, currentTheme);
            }
        }

        public void OnThemeDropdownChanged(int themeIndex)
        {
            if (currentlySelectedRoom == null) return;
            
            List<RoomTheme> availableThemes = GetThemesForCurrentRoom();
            if (themeIndex < availableThemes.Count)
            {
                currentTheme = availableThemes[themeIndex];
                ApplyThemeToRoom(currentlySelectedRoom, currentTheme);
            }
        }

        // Helper Methods
        private List<RoomTheme> GetThemesForChallenge(ChallengeType challengeType)
        {
            if (themesByChallenge.ContainsKey(challengeType))
            {
                return themesByChallenge[challengeType];
            }
            return new List<RoomTheme>();
        }

        private List<RoomTheme> GetThemesForCurrentRoom()
        {
            if (currentlySelectedRoom == null) return new List<RoomTheme>();
            
            ChallengeCard challenge = currentlySelectedRoom.assignedChallenge;
            if (challenge != null)
            {
                return GetThemesForChallenge(challenge.type);
            }
            return new List<RoomTheme>();
        }

        private void UpdateThemeDropdown(List<RoomTheme> themes)
        {
            if (themeDropdown == null) return;
            
            themeDropdown.ClearOptions();
            List<string> themeNames = themes.Select(t => t.themeName).ToList();
            themeDropdown.AddOptions(themeNames);
        }

        private float GetDistanceToNearestWall(Vector2Int position, Room room)
        {
            float minDistance = float.MaxValue;
            
            foreach (var floorPos in room.floorPositions)
            {
                // Check if this floor position is adjacent to a non-floor position (wall)
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                
                foreach (var direction in directions)
                {
                    Vector2Int adjacentPos = floorPos + direction;
                    if (!room.floorPositions.Contains(adjacentPos))
                    {
                        float distance = Vector2Int.Distance(position, floorPos);
                        minDistance = Mathf.Min(minDistance, distance);
                    }
                }
            }
            
            return minDistance;
        }

        private List<Vector2Int> GetAdjacentWallPositions(Room room)
        {
            List<Vector2Int> wallPositions = new List<Vector2Int>();
            
            foreach (var floorPosition in room.floorPositions)
            {
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                
                foreach (var direction in directions)
                {
                    Vector2Int adjacentPos = floorPosition + direction;
                    if (!room.floorPositions.Contains(adjacentPos))
                    {
                        wallPositions.Add(adjacentPos);
                    }
                }
            }
            
            return wallPositions.Distinct().ToList();
        }

        public void ClearRoomDecorations(Room room)
        {
            if (!roomStates.ContainsKey(room)) return;
            
            RoomDecorationState state = roomStates[room];
            
            foreach (var position in state.decoratedPositions)
            {
                floorDecorationTilemap?.SetTile(position, null);
                wallDecorationTilemap?.SetTile(position, null);
                overlayTilemap?.SetTile(position, null);
                featureTilemap?.SetTile(position, null);
            }
            
            roomStates.Remove(room);
        }

        public void ClearAllDecorations()
        {
            foreach (var room in roomStates.Keys.ToList())
            {
                ClearRoomDecorations(room);
            }
        }

        private enum FeatureSize
        {
            Small,
            Medium,
            Large
        }
    }

    // Example themed room configurations
    public static class ThemePresets
    {
        public static RoomTheme CreateFantasyCombatTheme()
        {
            var theme = new RoomTheme
            {
                themeName = "Fantasy Combat Arena",
                challengeType = ChallengeType.Combat,
                themeDescription = "Medieval fantasy combat with weapons and armor",
                decorationDensity = 0.4f,
                maxLargeFeatures = 1,
                maxMediumFeatures = 2,
                maxSmallFeatures = 4
            };
            
            // You would populate the tile lists and features here
            return theme;
        }

        public static RoomTheme CreateSciFiCombatTheme()
        {
            var theme = new RoomTheme
            {
                themeName = "Sci-Fi Combat Lab",
                challengeType = ChallengeType.Combat,
                themeDescription = "Futuristic combat with energy weapons and tech",
                decorationDensity = 0.3f,
                maxLargeFeatures = 1,
                maxMediumFeatures = 3,
                maxSmallFeatures = 3
            };
            
            return theme;
        }
    }
}