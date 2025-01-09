using UnityEngine;
using UnityEditor;

namespace Waker.Editors
{
    [CustomEditor(typeof(CanvasParticleSystem))]
    public class CanvasParticleSystemControlPanel : Editor
    {
        // 버튼의 크기와 위치를 설정
        private Rect playButtonRect;
        private Rect pauseButtonRect;
        private Rect stopButtonRect;

        // 패널의 배경 색상
        private Color panelBackgroundColor = new Color(0, 0, 0, 0.5f);

        // 패널의 크기
        private Vector2 panelSize = new Vector2(150, 100);

        // 오프셋 (오브젝트 위에 패널을 표시하기 위한 위치 조정)
        private Vector3 panelOffset = new Vector3(0, 2, 0);

        // OnSceneGUI는 씬 뷰에 그려지는 내용을 정의
        void OnSceneGUI()
        {
            // 타겟 오브젝트의 위치
            CanvasParticleSystem ps = (CanvasParticleSystem)target;

            // 씬뷰 스크린 사이즈
            Rect screenRect = SceneView.currentDrawingSceneView.position;
            Rect panelRect = new Rect(screenRect.width - panelSize.x - 20, screenRect.height - panelSize.y - 40, panelSize.x, panelSize.y);

            // GUI 창 그리기
            Handles.BeginGUI();
            GUI.Box(panelRect, GUIContent.none, EditorStyles.helpBox);

            // 배경 색상 적용
            EditorGUI.DrawRect(panelRect, panelBackgroundColor);

            // 버튼 위치 설정 (패널 내부)
            float buttonWidth = 50;
            float buttonHeight = 30;
            float spacing = 10;
            float startX = panelRect.x + (panelRect.width - (buttonWidth * 3 + spacing * 2)) / 2;
            float startY = panelRect.y + 20;

            playButtonRect = new Rect(startX, startY, buttonWidth, buttonHeight);
            pauseButtonRect = new Rect(startX + buttonWidth + spacing, startY, buttonWidth, buttonHeight);
            stopButtonRect = new Rect(startX + (buttonWidth + spacing) * 2, startY, buttonWidth, buttonHeight);

            // 버튼 그리기 및 클릭 이벤트 처리
            if (GUI.Button(playButtonRect, "Play"))
            {
                ps.Play();
            }

            if (GUI.Button(pauseButtonRect, "Pause"))
            {
                ps.Pause();
            }

            if (GUI.Button(stopButtonRect, "Stop"))
            {
                ps.Stop();
            }

            // 패널 제목
            GUI.Label(new Rect(panelRect.x, panelRect.y + 5, panelRect.width, 20), "Particle Control", EditorStyles.boldLabel);

            Handles.EndGUI();
        }
    }
}