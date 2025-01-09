using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace Waker
{
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public partial class CanvasParticleSystem : MonoBehaviour
    {
        [Header("Main")]
        [SerializeField] private float _duration = 1f;
        [SerializeField] private bool _loop = true;

        [Header("Particle")]
        [SerializeField] private Color _color;
        [SerializeField] private ParticleSystem.MinMaxCurve _startLifetime = new ParticleSystem.MinMaxCurve(1f);
        [SerializeField] private ParticleSystem.MinMaxCurve _startSize = new ParticleSystem.MinMaxCurve(100f);
        [SerializeField] private ParticleSystem.MinMaxCurve _startSpeed = new ParticleSystem.MinMaxCurve(100f);
        [SerializeField] private ParticleSystem.MinMaxCurve _startRotation = new ParticleSystem.MinMaxCurve(100f);

        [Header("Emit")]
        [SerializeField] private int _emitCountPerSecond = 10;

        [Header("Sharp")]
        [SerializeField] private SharpModule _sharpModule;


        [Header("Texture")]
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Material _material;
        [SerializeField] private SpriteSheetModule _spriteSheet;
        [SerializeField] private AttractorModule _attractor;
        [SerializeField] private SizeOverLifeTimeModule _sizeOverLifeTime;


        private Canvas _canvas;
        private CanvasRenderer _canvasRenderer;
        private RectTransform _rectTransform;
    
        private Particle[] _particles = new Particle[256];

        // Rendering
        private UIVertex[] _vertex = new UIVertex[4];

        private VertexHelper _vertexHelper;
        private Mesh _renderMesh;

        private float _playTime;
        private float _prevPlayTime;

        private bool _isPlaying;
        private bool _isPause;

        private void OnValidate()
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                _particles[i].color = _color;
            }

            if (_canvasRenderer != null)
            {
                if (_sprite != null)
                {
                    Texture2D mainTexture = _sprite.texture;

                    _canvasRenderer.SetTexture(mainTexture);
                    _canvasRenderer.SetMaterial(_material, mainTexture);
                }
                else
                {
                    _canvasRenderer.SetTexture(null);
                    _canvasRenderer.SetMaterial(null, null);
                }
            }
        }

        private void Awake()
        {
            _canvasRenderer = GetComponent<CanvasRenderer>();
            _rectTransform = GetComponent<RectTransform>();

            _vertexHelper = new VertexHelper();
            _renderMesh = new Mesh();

            // Find Canvas
            Transform parent = transform.parent;
            while (parent != null)
            {
                _canvas = parent.GetComponent<Canvas>();
                if (_canvas != null)
                {
                    break;
                }

                parent = parent.parent;
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += EditorUpdate;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorUpdate;
#endif
        }

        private void Start()
        {
            if (_material == null)
            {
                _material = new Material(Shader.Find("UI/Default"));
            }

            if (_canvasRenderer != null)
            {
                if (_sprite != null)
                {
                    Texture2D mainTexture = _sprite.texture;

                    _canvasRenderer.SetTexture(mainTexture);
                    _canvasRenderer.SetMaterial(_material, mainTexture);
                }
                else
                {
                    _canvasRenderer.SetTexture(null);
                    _canvasRenderer.SetMaterial(null, null);
                }
            }
        }

        private void Update()
        {   
            if (!Application.isPlaying)
            {
                return;
            }

            UpdateParticles();
        }

#if UNITY_EDITOR
        private void EditorUpdate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (UnityEditor.Selection.activeGameObject != gameObject)
            {
                return;
            }

            UpdateParticles();

            CreateMesh();
            UpdateRenderer();
            UnityEditor.SceneView.RepaintAll();
        }
#endif

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            CreateMesh();
            UpdateRenderer();
        }

        private void OnDrawGizmosSelected()
        {
            if (_sharpModule.Enabled)
            {
                if (_sharpModule.SharpType == SharpType.Circle)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(transform.position, _sharpModule.CircleRadius);
                }
            }    
        }

        private void UpdateParticles()
        {
            if (_particles == null)
            {
                return;
            }

            if (_isPause)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            _playTime += deltaTime;

            if (_playTime > _duration)
            {
                if (!_loop)
                {
                    _isPlaying = false;
                }
                else
                {
                    _playTime %= _duration;
                    _lastEmitTime = 0f;
                }
            }

            if (_isPlaying)
            {
                EmitParticles(deltaTime);
            }

            System.Array.Sort(_particles, (a, b) => a.remainLifeTime.CompareTo(b.remainLifeTime));

            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i].remainLifeTime > 0f)
                {
                    _particles[i].Update(deltaTime);

                    _attractor.UpdateParticle(ref _particles[i], _canvas, _rectTransform);
                    _sizeOverLifeTime.UpdateParticle(ref _particles[i]);
                }
            }
        }

        private float _lastEmitTime;

        private void EmitParticles(float deltaTime)
        {
            if (_lastEmitTime <= 0)
            {
                EmitParticle();
            }

            float emitInterval = 1f / _emitCountPerSecond;

            _lastEmitTime += deltaTime;

            int emitCount = (int)(_lastEmitTime / emitInterval);
            _lastEmitTime -= emitCount * emitInterval;

            for (int i = 0; i < emitCount; i++)
            {
                EmitParticle();
            }
        }

        private bool EmitParticle()
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i].remainLifeTime <= 0f)
                {
                    Vector4 sharp = _sharpModule.GetStartPositionAndRotation(_playTime);
                    float rotaiton = sharp.z;

                    Vector2 position = new Vector2(sharp.x, sharp.y);
                    Vector2 direction = new Vector2(Mathf.Cos(rotaiton * Mathf.Deg2Rad), Mathf.Sin(rotaiton * Mathf.Deg2Rad));

                    _particles[i].position = _rectTransform.rect.center + new Vector2(sharp.x, sharp.y);
                    _particles[i].size = _startSize.Evaluate(_playTime % _duration / _duration, Random.value);
                    _particles[i].rotation = _startRotation.Evaluate(_playTime % _duration / _duration, Random.value);
                    _particles[i].color = _color;

                    _particles[i].velocity = direction.normalized * _startSpeed.Evaluate(_playTime % _duration / _duration, Random.value);

                    _particles[i].lifeTime = _startLifetime.Evaluate(_playTime % _duration / _duration, Random.value);
                    _particles[i].remainLifeTime = _particles[i].lifeTime;

                    return true;
                }
            }

            return false;
        }

        void CreateMesh()
        {
            if (_renderMesh == null)
                _renderMesh = new Mesh();

            if (_vertexHelper == null)
                _vertexHelper = new VertexHelper();

            _vertexHelper.Clear();

            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i].remainLifeTime > 0f)
                {
                    Rect rect = GetSpriteRect(_particles[i].lifeTime, _particles[i].remainLifeTime);

                    _particles[i].WriteToUIVertext(_vertex, rect);
                    _vertexHelper.AddUIVertexQuad(_vertex);
                }
            }

            _vertexHelper.FillMesh(_renderMesh);
        }

        void UpdateRenderer()
        {
            if (_canvasRenderer != null)
            {
                _canvasRenderer.SetMesh(_renderMesh);
            }

#if UNITY_EDITOR
            // 에디터에서 변경 사항을 즉시 반영
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        private Rect MainSpriteRect()
        {
            if (_sprite == null)
            {
                return new Rect(0, 0, 1, 1);
            }

            Rect rect = _sprite.rect;
            rect.x /= _sprite.texture.width;
            rect.y /= _sprite.texture.height;
            rect.width /= _sprite.texture.width;
            rect.height /= _sprite.texture.height;

            return rect;
        }

        private Rect GetSpriteRect(float lifeTime, float remainLifeTime)
        {
            Rect fullRect = MainSpriteRect();
            return _spriteSheet.GetSpriteSheetRect(fullRect, lifeTime, remainLifeTime);
        }

        public void Play()
        {
            _isPlaying = true;
            _isPause = false;
            _lastEmitTime = 0f;

            if (_playTime >= _duration)
            {
                _playTime = 0f;
            }
        }

        public void Pause()
        {
            _isPause = !_isPause;
        }

        public void Stop()
        {
            _isPlaying = false;
            _isPause = false;
            _playTime = 0f;

            ClearParticle();
        }

        private void ClearParticle()
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                _particles[i].remainLifeTime = 0f;
            }

            _lastEmitTime = 0f;
            _playTime = 0f;

            _renderMesh.Clear();
            UpdateRenderer();
        }
    }

    public partial class CanvasParticleSystem
    {
        public struct Particle
        {
            public Vector2 position;
            public float size;
            public float rotation;
            public Color32 color;

            public Vector2 renderPosition;
            public float renderSize;

            public Vector2 velocity;

            public float lifeTime;
            public float remainLifeTime;

            public void WriteToUIVertext(UIVertex[] vertex, Rect rect)
            {
                if (vertex.Length != 4)
                {
                    Debug.LogError("Vertex array is too small.");
                    return;
                }

                Vector2 position = this.renderPosition;
                float halfSize = this.renderSize * 0.5f;

                vertex[0].position = position + (Vector2)(Quaternion.Euler(0f, 0f, rotation) * new Vector2(-halfSize, -halfSize));
                vertex[1].position = position + (Vector2)(Quaternion.Euler(0f, 0f, rotation) * new Vector2(-halfSize, halfSize));
                vertex[2].position = position + (Vector2)(Quaternion.Euler(0f, 0f, rotation) * new Vector2(halfSize, halfSize));
                vertex[3].position = position + (Vector2)(Quaternion.Euler(0f, 0f, rotation) * new Vector2(halfSize, -halfSize));

                vertex[0].color = color;
                vertex[1].color = color;
                vertex[2].color = color;
                vertex[3].color = color;

                Vector2 rectMin = rect.min;
                Vector2 rectMax = rect.max;

                vertex[0].uv0 = rectMin;
                vertex[1].uv0 = new Vector2(rectMin.x, rectMax.y);
                vertex[2].uv0 = rectMax;
                vertex[3].uv0 = new Vector2(rectMax.x, rectMin.y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(float deltaTime)
            {
                remainLifeTime -= deltaTime;
                position += velocity * deltaTime;

                renderPosition = position;
                renderSize = size;
            }
        }

    }

    #region SpriteSheetModule
    public partial class CanvasParticleSystem
    {
        public enum SpriteSheetSpeedType
        {
            Cycle,
            FPS,
        }

        [System.Serializable]
        public class SpriteSheetModule
        {
            [SerializeField] private bool _enabled;
            [SerializeField] private Vector2Int _sheetSize = new Vector2Int(1, 1);
            [SerializeField] private SpriteSheetSpeedType _sheetSpeedType = SpriteSheetSpeedType.Cycle;
            [SerializeField] private int _sheetCycle = 1;
            [SerializeField] private float _sheetFPS = 1f;

            public bool Enabled { get => _enabled; set => _enabled = value; }

            public Vector2Int SheetSize { get => _sheetSize; set => _sheetSize = value; }
            public SpriteSheetSpeedType SheetSpeedType { get => _sheetSpeedType; set => _sheetSpeedType = value; }
            public int SheetCycle { get => _sheetCycle; set => _sheetCycle = value; }
            public float SheetFPS { get => _sheetFPS; set => _sheetFPS = value; }
            
            public Rect GetSpriteSheetRect(Rect fullRect, float lifeTime, float remainLifeTime)
            {
                if (!Enabled)
                {
                    return fullRect;
                }

                int sheetCycle = SheetCycle;

                switch (SheetSpeedType)
                {
                    case SpriteSheetSpeedType.Cycle:
                        return CalculateLifeTimeSpriteSheetFrameRect(fullRect, SheetSize, sheetCycle, 1f - remainLifeTime / lifeTime);
                    case SpriteSheetSpeedType.FPS:
                        return CalculateFPSSpriteSheetFrameRect(fullRect, SheetSize, SheetFPS, lifeTime - remainLifeTime);
                    default:
                        return fullRect;
                }
            }

            private static Rect CalculateFPSSpriteSheetFrameRect(Rect fullRect, Vector2Int sheetSize, float fps, float age)
            {
                int totalFrame = sheetSize.x * sheetSize.y;

                if (totalFrame <= 0)
                {
                    return fullRect;
                }

                int index = (int)(age * fps) % totalFrame;
                
                return CalculateSpriteSheetRect(fullRect, sheetSize, index);
            }

            private static Rect CalculateLifeTimeSpriteSheetFrameRect(Rect fullRect, Vector2Int sheetSize, int cycle, float lifeTimeRatio)
            {
                int totalFrame = sheetSize.x * sheetSize.y;

                if (totalFrame <= 0)
                {
                    return fullRect;
                }

                int cycleFrame = totalFrame * cycle;
                int index = (int)(lifeTimeRatio * cycleFrame) % totalFrame;

                return CalculateSpriteSheetRect(fullRect, sheetSize, index);
            }

            private static Rect CalculateSpriteSheetRect(Rect fullRect, Vector2Int sheetSize, int index)
            {
                Rect rect = fullRect;

                int horizontalFrame = sheetSize.x;
                int verticalFrame = sheetSize.y;

                if (horizontalFrame <= 0 || verticalFrame <= 0)
                {
                    return rect;
                }

                int totalFrame = horizontalFrame * verticalFrame;
                int i = index % totalFrame;

                float x = rect.x;
                float y = rect.y;
                float width = rect.width;
                float height = rect.height;

                float sheetWidth = width / horizontalFrame;
                float sheetHeight = height / verticalFrame;

                int row = i / horizontalFrame;
                int col = i % horizontalFrame;

                row = sheetSize.y - 1 - row;

                x += sheetWidth * col;
                y += sheetHeight * row;

                return new Rect(x, y, sheetWidth, sheetHeight);
            }
        }
    }
    #endregion

    #region SharpModule
    public partial class CanvasParticleSystem
    {
        public enum SharpType
        {
            Point,
            Circle,
            Rectangle,
            Edge,
        }

        public enum SharpSpawnMode
        {
            Random,
            Sequence,
        }

        [System.Serializable]
        public class SharpModule
        {
            [SerializeField] private bool _enabled = true;
            [SerializeField] private SharpType _sharpType = SharpType.Point;
            [SerializeField] private SharpSpawnMode _sharpSpawnMode = SharpSpawnMode.Random;
            [SerializeField] private int _sharpSpawnSpeed = 1;

            // Circle
            [SerializeField] private float _circleRadius = 1f;

            // Others
            [SerializeField] private bool _randomDirection = false;

            public bool Enabled { get => _enabled; set => _enabled = value; }
            public SharpType SharpType { get => _sharpType; set => _sharpType = value; }
            public SharpSpawnMode SharpSpawnMode { get => _sharpSpawnMode; set => _sharpSpawnMode = value; }
            public int SharpSpawnSpeed { get => _sharpSpawnSpeed; set => _sharpSpawnSpeed = value; }
            public float CircleRadius { get => _circleRadius; set => _circleRadius = value; }
            public bool RandomDirection { get => _randomDirection; set => _randomDirection = value; }

            // x, y: 위치, z: 이동 방향(각도)
            public Vector4 GetStartPositionAndRotation(float time)
            {
                if (!Enabled)
                {
                    return Vector4.zero;
                }

                if (SharpType == SharpType.Point)
                {
                    float directionAngle = GetDirection(Vector2.zero);
                    return new Vector4(0, 0, directionAngle, 0);
                }

                if (SharpType == SharpType.Circle)
                {
                    return GetCirclePosition(time);
                }

                return Vector4.zero;
            }

            private Vector4 GetCirclePosition(float time)
            {
                if (SharpSpawnMode == SharpSpawnMode.Random)
                {
                    Vector2 position = Random.insideUnitCircle * CircleRadius;
                    float directionAngle = GetDirection(position);

                    return new Vector4(position.x, position.y, directionAngle, 0);
                }
                else
                {
                    float angle = time * SharpSpawnSpeed * Mathf.PI * 2f;
                    float direction = GetDirection(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));

                    Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * CircleRadius;
                    float directionAngle = GetDirection(position);

                    return new Vector4(position.x, position.y, directionAngle, direction);
                }
            }

            private float GetDirection(Vector2 position)
            {
                if (RandomDirection)
                {
                    return Random.Range(0f, 360f);
                }
                else if (position.sqrMagnitude < 0.0001f)
                {
                    return 0f;
                }
                else
                {
                    return Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg;
                }
            }
        }
    }

    #endregion

    #region Attractor

    public partial class CanvasParticleSystem
    {
        [System.Serializable]
        public class AttractorModule
        {
            [SerializeField] private bool _enabled;
            [SerializeField] private Transform _attractor;
            [SerializeField] private AnimationCurve _attractorCurve = AnimationCurve.Linear(0, 0, 1, 1);

            private bool Enabled
            {
                get => _enabled; 
                set => _enabled = value; 
            }
            
            private AnimationCurve AttractorCurve 
            {
                get => _attractorCurve; 
                set => _attractorCurve = value; 
            }
            
            private Transform Attractor 
            { 
                get => _attractor; 
                set => _attractor = value; 
            }

            private Vector3 _cachedPosition;
            private Vector2 _destination;

            public void UpdateParticle(ref Particle particle, Canvas canvas, RectTransform origin)
            {
                if (!Enabled)
                {
                    return;
                }

                if (CheckAttractor())
                {
                    if (Attractor is RectTransform rectTransform)
                    {
                        // rectTransform의 위치를 Canvas 기준으로 변환 후 파티클 기준으로 다시 변환
                        Vector2 position = rectTransform.position;

                        RectTransformUtility.ScreenPointToLocalPointInRectangle(origin, position, canvas.worldCamera, out Vector2 localPosition);

                        _destination = localPosition;
                    }
                    else
                    {
                        // TODO: Add more types
                        _destination = Attractor.position;
                    }
                }

                float t = AttractorCurve.Evaluate(1f - particle.remainLifeTime / particle.lifeTime);
                particle.renderPosition = ExtendedLerp(particle.position, _destination, t);
            }

            private float ExtendedLerp(float a, float b, float t)
            {
                return a + (b - a) * t;
            }

            private Vector2 ExtendedLerp(Vector2 a, Vector2 b, float t)
            {
                return a + (b - a) * t;
            }

            private bool CheckAttractor()
            {
                if (Attractor == null)
                {
                    return false;
                }

                if (_cachedPosition != Attractor.position)
                {
                    _cachedPosition = Attractor.position;
                    return true;
                }

                return false;
            }
        }
    }

    #endregion

    #region Size Over LiveTime

    public partial class CanvasParticleSystem
    {
        [System.Serializable]
        public class SizeOverLifeTimeModule
        {
            [SerializeField] private bool _enabled;
            [SerializeField] private AnimationCurve _curve = AnimationCurve.Linear(0, 1, 1, 1);

            public bool Enabled
            {
                get => _enabled;
                set => _enabled = value;
            }

            public AnimationCurve Curve
            {
                get => _curve;
                set => _curve = value;
            }

            public void UpdateParticle(ref Particle particle)
            {
                if (!Enabled)
                {
                    return;
                }

                particle.renderSize = particle.size * _curve.Evaluate(1f - particle.remainLifeTime / particle.lifeTime);
            }
        }
    }

    #endregion
}
