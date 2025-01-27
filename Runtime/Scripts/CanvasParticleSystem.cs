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
        
        [SerializeField] 
        private float _duration = 1f;
        
        [SerializeField] 
        private bool _loop = true;


        [Header("Particle")]
        
        [SerializeField] 
        private Color _color;
        
        [SerializeField] 
        private ParticleSystem.MinMaxCurve _startLifetime = new ParticleSystem.MinMaxCurve(1f);
        
        [SerializeField] 
        private ParticleSystem.MinMaxCurve _startSize = new ParticleSystem.MinMaxCurve(100f);
        
        [SerializeField] 
        private ParticleSystem.MinMaxCurve _startSpeed = new ParticleSystem.MinMaxCurve(100f);
        
        [SerializeField] 
        private ParticleSystem.MinMaxCurve _startRotation = new ParticleSystem.MinMaxCurve(100f);

        [Header("Emit")]
        
        [SerializeField] 
        private int _emitCountPerSecond = 10;


        [Header("Sharp")]
        
        [SerializeField] 
        private SharpModule _sharpModule;


        [Header("Texture")]
        
        [SerializeField] 
        private Sprite _sprite;
        
        [SerializeField] 
        private Material _material;


        [Header("Sprite Sheet Animation")]
        
        [SerializeField] private SpriteSheetAnimationModule _spriteSheet;


        [Header("Attractor")]
        
        [SerializeField] private AttractorModule _attractor;


        [Header("Size Over LifeTime")]

        [SerializeField] private SizeOverLifeTimeModule _sizeOverLifeTime;

        // Components
        private Canvas _canvas;
        private CanvasRenderer _canvasRenderer;
        private RectTransform _rectTransform;
    
        // Particles
        private Particle[] _particles = new Particle[256];
    
        // Rendering
        private UIVertex[] _vertex = new UIVertex[4];

        private VertexHelper _vertexHelper;
        private Mesh _renderMesh;

        // State
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
            _lastEditorTime = (float)UnityEditor.EditorApplication.timeSinceStartup;
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

            UpdateParticles(Time.deltaTime);

            CreateMesh();
            UpdateRenderer();
        }

#if UNITY_EDITOR
        private float _lastEditorTime; // 이전 업데이트 시간

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

            // deltaTime 계산
            float currentTime = (float)UnityEditor.EditorApplication.timeSinceStartup;
            float deltaTime = currentTime - _lastEditorTime;
            _lastEditorTime = currentTime;

            // deltaTime을 사용하여 파티클 업데이트
            UpdateParticles(deltaTime);

            CreateMesh();
            UpdateRenderer();

            // 에디터 씬 뷰 갱신
            UnityEditor.SceneView.RepaintAll();
        }
#endif

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            
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

        private void UpdateParticles(float deltaTime)
        {
            if (_particles == null)
            {
                return;
            }

            if (_isPause)
            {
                return;
            }

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

                    _attractor.UpdateParticle(ref _particles[i], _rectTransform);
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

                    _particles[i].WriteToUIVertexs(_vertex, rect);
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
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        private Rect MainSpriteRect()
        {
            // TODO: caching main sprite rect
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
            public float lifeTime;
            public Vector2 startPosition;
            public float startRotation;
            public Vector2 startSize;

            public float remainLifeTime;
            public Vector2 position;
            public float size;
            public float rotation;
            public Color32 color;

            public Vector2 finalPosition;
            public Vector3 finalRotation;
            public float finalSize;

            public Vector2 velocity;


            public readonly void WriteToUIVertexs(UIVertex[] vertex, Rect rect)
            {
                if (vertex.Length != 4)
                {
                    Debug.LogError("Vertex array is too small.");
                    return;
                }

                Vector2 position = this.finalPosition;
                float halfSize = this.finalSize * 0.5f;

                Quaternion r = Quaternion.Euler(0f, 0f, rotation);

                vertex[0].position = position + (Vector2)(r * new Vector2(-halfSize, -halfSize));
                vertex[1].position = position + (Vector2)(r * new Vector2(-halfSize, halfSize));
                vertex[2].position = position + (Vector2)(r * new Vector2(halfSize, halfSize));
                vertex[3].position = position + (Vector2)(r * new Vector2(halfSize, -halfSize));

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

                finalPosition = position;
                finalSize = size;
            }
        }

    }
}
