# Unity FPS Arena 프로젝트 세션 요약

## 프로젝트 환경
- Unity 6000.5.4f1, URP 파이프라인
- 프로젝트 경로: `C:\Users\user\Documents\PHANTOM_EDGE`
- com.unity.ai.navigation 제거 완료
- CS0618 수정 완료 (FindAnyObjectByType 사용)

## 게임 설정
- 1인칭 근접 전투 전용 (3인칭/권총 삭제)
- 카타나 + 그래플링 훅
- 아레나 FPS, 웨이브 기반 적 스폰

---

## 완료된 작업 (누적)

### 버그 수정
1. **러셔 차징 방향 수정**: `player.position + (transform.position - player.position)` → `transform.position + chargeDir * chargeDistance`
2. **UIManager.Instance 누락**: Awake에 싱글톤 패턴 추가
3. **CameraShake 덮어쓰기**: isShaking 중이면 originalPos 유지, intensity/duration은 max로 확장
4. **HitPause 스태킹**: 항상 리셋하도록 수정 (previousTimeScale = 1f로 고정)
5. **EnemyController originalColor**: Start()에서 미리 캐시 (Color.clear 체크 제거)
6. **벽탐색 후방 추가**: hitB 레이캐스트 + wallNormal 방향 수정 (벽에서 밀려나오는 방향)
7. **PostProcessing FilmGrain 에러 수정**: Unity 6 URP 호환 위해 `size`, `type` 프로퍼티 제거
8. **AutoSetup 컴파일 에러 수정**: `CreateButton` 헬퍼 메서드 추가, KatanaTrail→TrailRenderer 타입 불일치 수정

### 게임플레이 개선
1. **Coyote Time**: 0.1f - 공중에서도 잠시 점프 가능
2. **Jump Buffer**: 0.12f - 착지 전 점프 입력 유지
3. **벽점프 Normal 수정**: 벽을 바라보는 방향 → 벽에서 밀려나오는 방향
4. **메뉴 시스템 추가**: 메인 메뉴 / 일시정지 / 게임오버 메뉴 전체 구현
5. **게임 시작 플로우**: 메뉴 → Start 버튼 → 게임 시작 (커서 잠금, 이동 가능)

### UI/HUD 개선
1. **데미지 비네팅**: 피격 시 화면 붉은 테두리 + 저체력 시 지속 효과
2. **데미지 방향 표시**: 피격 시 방향 화살표 표시
3. **대시 아이콘**: 잔여 대시 수량 시각화
4. **그래플 쿨타운**: 원형 게이지 + 키 텍스트 연동
5. **크로스헤이어 다이내믹**: 대시/슬라이드/속도에 따라 크기 변화
6. **체력바 스무딩**: Lerp 기반 부드러운 체력바
7. **속도 텍스트**: km/h 단위 표시
8. **웨이브 알림**: 페이드 아웃 애니메이션

### 코드 최적화
1. **SpawnHitEffect 정적 재질**: 매번 new Material 대신 static 재질 재사용
2. **오브젝트 풀**: ObjectPool 시스템 추가 (Spark, Blood, Gib)
3. **스파크 수 감소**: 12개 → 6개 (성능)

### 비주얼/이펙트 수정
1. **카타나 회전 수정**: `Quaternion.Euler(10f, 180f, 0f)` → `Quaternion.Euler(90f, 180f, 0f)` - 모델이 눕혀져 있던 문제 해결
2. **트레일 회전 보정**: Trail 오브젝트 로컬 회전 90도 추가 (블레이드 방향과 정렬)
3. **아이들 트레일 색상 변경**: 파란색 → 금색/주황 계열 (카타나 모델과 매칭)

### 이전 세션 작업 포함
- AutoSetup.cs (에디터 자동 셋업 1,600+줄)
- PlayerController.cs (카타나 5단계 스윙, 패링, 대시, 슬라이드, 벽점프)
- EnemyController.cs (Rusher/Shooter AI)
- EnemySpawner.cs (웨이브 스폰 + FBX 로딩)
- GrapplingHook.cs (훅 + 스윙)
- 전체 Effects 시스템 (CameraShake, HitPause, KatanaTrail, KatanaAura 등)
- ProceduralAudio + AudioManager
- ComboSystem
- PostProcessingSetup

---

## 적 모델 설정 (미완료 - 사용자 작업 필요)

### 다운로드 링크
- **Ultimate Monsters (50마리)**: https://drive.google.com/drive/folders/18m4KpzpEzhC9wl7jzr6dUc0N8Jozr79C?usp=sharing
- **Zombie Apocalypse Kit (60개)**: https://drive.google.com/drive/folders/1mWP6sCHun7OUMHQeDNZLrXTteXlzWg_t?usp=sharing

### 설정 방법
```
Assets/Models/
├── Rusher.fbx    ← 러셔용 (예: Orc Enemy.fbx를 이름 변경)
├── Shooter.fbx   ← 슈터용 (예: Alien.fbx를 이름 변경)
└── Enemies/      ← 추가 모델용
```

---

## 실행 방법
1. Unity 열기
2. `PHANTOM EDGE > Reset Setup Flag`
3. `PHANTOM EDGE > Re-Setup Arena` (씬 전체 재생성 - 메뉴 포함)
4. Play → 메인 메뉴에서 Start 클릭 → 게임 시작

## 파일 구조
```
Assets/
├── Editor/AutoSetup.cs              ← 아레나 자동 생성 (메뉴 포함)
├── Scripts/
│   ├── Player/
│   │   ├── PlayerController.cs      ← 근접 전투 + 이동 + 패링
│   │   ├── GrapplingHook.cs         ← 그래플링 훅
│   │   ├── WeaponSway.cs            ← 무기 흔들림
│   │   └── ArmAnimation.cs          ← 팔 애니메이션
│   ├── Enemy/
│   │   ├── EnemyController.cs       ← 적 AI (Rusher/Shooter)
│   │   └── EnemySpawner.cs          ← 웨이브 스폰
│   ├── Core/
│   │   ├── GameManager.cs           ← 게임 상태 관리 (StartGame/ResetGame 추가)
│   │   ├── AudioManager.cs          ← 오디오 관리
│   │   ├── ObjectPool.cs            ← 오브젝트 풀 시스템
│   │   └── ProceduralAudio.cs       ← 프로시저럴 사운드
│   ├── Combat/
│   │   └── ComboSystem.cs           ← 콤보 시스템
│   ├── Effects/
│   │   ├── CameraShake.cs           ← 카메라 흔들림
│   │   ├── HitPause.cs               ← 히트 포즈
│   │   ├── KatanaTrail.cs           ← 카타나 트레일 + 감지기
│   │   ├── KatanaAura.cs            ← 카타나 오라
│   │   ├── GrapplePolish.cs         ← 그래플 이펙트
│   │   ├── MovementEffects.cs       ← 이동 이펙트
│   │   ├── EnemyDeathEffect.cs      ← 적 사망 연출
│   │   └── PostProcessingSetup.cs   ← 포스트 프로세싱 (FilmGrain 수정됨)
│   └── UI/
│       ├── UIManager.cs             ← HUD/UI
│       └── MenuManager.cs           ← 메인/일시정지/게임오버 메뉴 (신규)
├── katana.FBX
├── handgrip_color.jpg
├── Materials/Military/
├── Models/                           ← 적 FBX 모델 위치
└── Scenes/PHANTOM EDGE_Arena.unity
```

---

## 주의사항
- AnimatorController는 런타임 스크립트에서 사용 불가 (에디터 전용)
- File.Exists로 Unity 에셋 체크 불가 → AssetDatabase.LoadAssetAtPath 사용
- ModelImporter.animateMaterialProperties 없음 (Unity 6)
- TransitionInterruptionSource.CurrentState → Source 사용
- Custom Shaders 필요: Custom/Dissolve, Custom/KatanaAura, Custom/KatanaTrail
- 메뉴는 `PHANTOM EDGE > Re-Setup Arena` 실행 시 자동 생성됨
