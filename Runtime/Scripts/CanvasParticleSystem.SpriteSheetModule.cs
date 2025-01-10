using UnityEngine;

namespace Waker
{
    public partial class CanvasParticleSystem
    {
        public enum ParticleSpriteSheetAnimationTimeMode
        {
            Cycle,
            FPS,
        }

        [System.Serializable]
        public class SpriteSheetAnimationModule
        {
            [SerializeField] 
            private bool _enabled;
            
            [SerializeField] 
            private Vector2Int _tileSize = new Vector2Int(1, 1);
            
            [SerializeField] 
            private ParticleSpriteSheetAnimationTimeMode _animationMode = ParticleSpriteSheetAnimationTimeMode.Cycle;
            
            [SerializeField] 
            private int _animationCycle = 1;
            
            [SerializeField] 
            private float _animationFPS = 1f;

            public bool Enabled 
            { 
                get => _enabled;
                set => _enabled = value; 
            }

            public Vector2Int TileSize 
            { 
                get => _tileSize; 
                set => _tileSize = value; 
            }
            
            public ParticleSpriteSheetAnimationTimeMode AnimationMode 
            {
                get => _animationMode; 
                set => _animationMode = value; 
            }

            public int AnimationCycle 
            { 
                get => _animationCycle; 
                set => _animationCycle = value; 
            }
            
            public float AnimationFPS 
            { 
                get => _animationFPS; 
                set => _animationFPS = value; 
            }
            
            public Rect GetSpriteSheetRect(Rect fullRect, float lifeTime, float remainLifeTime)
            {
                if (!Enabled)
                {
                    return fullRect;
                }

                int index = GetIndex(fullRect, lifeTime, remainLifeTime);
                return CalculateSpriteSheetRect(fullRect, TileSize, index);
            }

            private int GetIndex(Rect fullRect, float lifeTime, float remainLifeTime)
            {
                switch (AnimationMode)
                {
                    case ParticleSpriteSheetAnimationTimeMode.Cycle:
                        return GetCycleIndex(fullRect, TileSize, AnimationCycle, 1f - remainLifeTime / lifeTime);
                    case ParticleSpriteSheetAnimationTimeMode.FPS:
                        return GetFPSIndex(fullRect, TileSize, AnimationFPS, lifeTime - remainLifeTime);
                    default:
                        return 0;
                }
            }

            private static int GetFPSIndex(Rect fullRect, Vector2Int sheetSize, float fps, float age)
            {
                int totalFrame = sheetSize.x * sheetSize.y;

                if (totalFrame <= 0)
                {
                    return 0;
                }

                return (int)(age * fps) % totalFrame;
            }

            private static int GetCycleIndex(Rect fullRect, Vector2Int sheetSize, int cycle, float lifeTimeRatio)
            {
                int totalFrame = sheetSize.x * sheetSize.y;

                if (totalFrame <= 0)
                {
                    return 0;
                }

                int cycleFrame = totalFrame * cycle;
                return (int)(lifeTimeRatio * cycleFrame) % totalFrame;
            }

            private static Rect CalculateSpriteSheetRect(Rect fullRect, Vector2Int sheetSize, int index)
            {
                int horizontalFrame = sheetSize.x;
                int verticalFrame = sheetSize.y;

                if (horizontalFrame <= 0 || verticalFrame <= 0)
                {
                    return fullRect;
                }

                int totalFrame = horizontalFrame * verticalFrame;
                int i = index % totalFrame;

                float x = fullRect.x;
                float y = fullRect.y;
                float width = fullRect.width;
                float height = fullRect.height;

                float sheetWidth = width / horizontalFrame;
                float sheetHeight = height / verticalFrame;

                int row = sheetSize.y - 1 - i / horizontalFrame;
                int col = i % horizontalFrame;

                x += sheetWidth * col;
                y += sheetHeight * row;

                return new Rect(x, y, sheetWidth, sheetHeight);
            }
        }
    }
}
